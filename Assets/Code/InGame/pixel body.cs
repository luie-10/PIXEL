using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 픽셀 몬스터(체력)와, 조립 시 배치되어 있는 픽셀 개수에 따라
/// 실시간으로 변화하는 각종 패시브 능력치를 관리합니다.
/// PlayerPixelArtSpawner가 스프라이트를 완성한 시점(=게임에 실제 진입한 시점)에서 초기화되며,
/// 조립 단계(에디터)에서는 작동하지 않습니다.
/// </summary>
[RequireComponent(typeof(PlayerPixelArtSpawner))]
public class PlayerPixelBody : MonoBehaviour
{
    /// <summary>
    /// 외부(콜라이더 생성 스크립트 등)에 안전하게 노출하기 위한 살아있는 타일 정보입니다.
    /// </summary>
    public struct PixelTileInfo
    {
        public int x;
        public int y;
        public bool isAttackTile;
    }

    /// <summary>
    /// 픽셀 한 칸이 갖는 런타임 상태입니다. (저장용 데이터가 아니라 게임 중에만 존재하는 상태입니다)
    /// </summary>
    private class RuntimeTile
    {
        public int x;
        public int y;
        public PixelColor color;
        public bool isAttackTile;
        public int maxDurability;
        public int currentDurability;
        public bool IsAlive => currentDurability > 0;
    }

    [Header("Config")]
    [SerializeField] private PixelColorConfig config;

    [Header("References")]
    [SerializeField] private PlayerPixelArtSpawner spawner;

    [Header("Base Stats")]
    [Tooltip("픽셀 패시브가 전혀 없을 때의 기본 공격력입니다.")]
    [SerializeField] private float baseAttack = 10f;

    private readonly List<RuntimeTile> tiles = new List<RuntimeTile>();
    private readonly Dictionary<PixelColor, int> aliveCountByColor = new Dictionary<PixelColor, int>();

    private Coroutine greenRegenCoroutine;

    /// <summary>
    /// 빨간 픽셀 개수 × redAttackPerPixel 로 계산되는 추가 공격력입니다.
    /// </summary>
    public float RedAttackBonus { get; private set; }

    /// <summary>
    /// 기본 공격력(baseAttack)에 RedAttackBonus를 더한 현재 최종 공격력입니다.
    /// 액티브 스킬 데미지, 공격 타일 접촉 데미지 계산 등에서 사용합니다.
    /// </summary>
    public float CurrentAttack => baseAttack + RedAttackBonus;

    /// <summary>
    /// 1.0을 기준으로 한 이동속도 배율입니다. (노란 픽셀 개수 반영)
    /// </summary>
    public float MoveSpeedMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 기본 수집 범위에 추가되는 보너스입니다(칸 단위). (초록 픽셀 개수 반영)
    /// </summary>
    public float GreenPickupRangeBonus { get; private set; }

    /// <summary>
    /// 패시브 능력치가 변경될 때마다 호출됩니다. (픽셀 파괴, 초록 회복 등)
    /// </summary>
    public event Action StatsChanged;

    /// <summary>
    /// 픽셀 하나의 내구도가 0이 되어 파괴되었을 때 (x, y) 좌표와 함께 호출됩니다.
    /// 콜라이더 생성 스크립트가 이 이벤트를 구독해 해당 칸의 콜라이더를 제거합니다.
    /// </summary>
    public event Action<int, int> PixelBroken;

    /// <summary>
    /// 내구도 계산과 라운드 시작 보너스까지 모두 끝나 몸이 완전히 준비되었을 때 호출됩니다.
    /// 콜라이더 생성 등, 몸의 최종 상태가 필요한 시스템은 이 이벤트를 사용해야 합니다.
    /// </summary>
    public event Action BodyInitialized;

    private void Awake()
    {
        if (spawner == null) spawner = GetComponent<PlayerPixelArtSpawner>();
    }

    private void OnEnable()
    {
        if (spawner != null) spawner.SpriteBuilt += HandleSpriteBuilt;
    }

    private void OnDisable()
    {
        if (spawner != null) spawner.SpriteBuilt -= HandleSpriteBuilt;

        if (greenRegenCoroutine != null)
        {
            StopCoroutine(greenRegenCoroutine);
            greenRegenCoroutine = null;
        }
    }

    /// <summary>
    /// 스프라이트가 완성된 시점(=게임에 실제 진입한 시점)에 호출되어 실제로 패시브 능력치를 초기화합니다.
    /// </summary>
    private void HandleSpriteBuilt(PlayerPixelArtSpawner builtSpawner)
    {
        PixelArtData data = builtSpawner.LoadedData;

        if (data == null || data.tiles == null || config == null)
        {
            Debug.LogWarning(
                "[PlayerPixelBody] PixelArtData 또는 PixelColorConfig가 없어 능력치 시스템을 초기화하지 못했습니다.",
                this
            );
            return;
        }

        BuildRuntimeTiles(data);
        RecalculatePassiveStats();
        ApplyBlueRoundStartBonus();

        if (greenRegenCoroutine != null) StopCoroutine(greenRegenCoroutine);
        greenRegenCoroutine = StartCoroutine(CoGreenRegenLoop());

        BodyInitialized?.Invoke();
    }

    private void BuildRuntimeTiles(PixelArtData data)
    {
        tiles.Clear();

        foreach (PixelTileData tileData in data.tiles)
        {
            if (tileData == null) continue;

            int maxDurability = config.GetBaseDurability(tileData.color);

            tiles.Add(new RuntimeTile
            {
                x = tileData.x,
                y = tileData.y,
                color = tileData.color,
                isAttackTile = tileData.isAttackTile,
                maxDurability = maxDurability,
                currentDurability = maxDurability
            });
        }
    }

    /// <summary>
    /// 현재 살아있는 픽셀 개수를 색상별로 다시 집계하고, 각종 패시브 능력치를 갱신합니다.
    /// 픽셀이 파괴되어 개수가 줄면 다른 능력치도 함께 감소합니다.
    /// </summary>
    private void RecalculatePassiveStats()
    {
        aliveCountByColor.Clear();

        foreach (RuntimeTile tile in tiles)
        {
            if (!tile.IsAlive) continue;

            if (!aliveCountByColor.ContainsKey(tile.color))
                aliveCountByColor[tile.color] = 0;

            aliveCountByColor[tile.color]++;
        }

        int redCount = GetAliveCount(PixelColor.Red);
        int yellowCount = GetAliveCount(PixelColor.Yellow);
        int greenCount = GetAliveCount(PixelColor.Green);

        RedAttackBonus = redCount * config.redAttackPerPixel;
        MoveSpeedMultiplier = 1f + (yellowCount * config.yellowMoveSpeedPercentPerPixel);
        GreenPickupRangeBonus = greenCount * config.greenPickupRangePerPixel;

        StatsChanged?.Invoke();
    }

    /// <summary>
    /// 지정한 색상의 현재 살아있는 픽셀 개수를 반환합니다.
    /// </summary>
    public int GetAliveCount(PixelColor color)
    {
        return aliveCountByColor.TryGetValue(color, out int count) ? count : 0;
    }

    /// <summary>
    /// 현재 살아있는 모든 픽셀의 좌표와 공격 타일 여부를 반환합니다.
    /// 콜라이더 생성 스크립트가 이 목록을 기준으로 피격/공격 콜라이더를 만듭니다.
    /// </summary>
    public List<PixelTileInfo> GetAliveTileInfos()
    {
        List<PixelTileInfo> result = new List<PixelTileInfo>();

        foreach (RuntimeTile tile in tiles)
        {
            if (!tile.IsAlive) continue;

            result.Add(new PixelTileInfo
            {
                x = tile.x,
                y = tile.y,
                isAttackTile = tile.isAttackTile
            });
        }

        return result;
    }

    /// <summary>
    /// 파란 픽셀의 8방향으로 인접한 픽셀들에게 라운드 시작 시 1회
    /// (살아있는 파란 픽셀 수 / blueAdjacentBonusDivider) 만큼의 추가 내구도를 부여합니다.
    /// 하나의 픽셀이 여러 파란 픽셀에 인접하더라도 중첩되지 않고 한 번만 적용됩니다.
    /// </summary>
    private void ApplyBlueRoundStartBonus()
    {
        int blueCount = GetAliveCount(PixelColor.Blue);
        if (blueCount <= 0) return;

        int bonus = Mathf.RoundToInt(blueCount / Mathf.Max(config.blueAdjacentBonusDivider, 0.0001f));
        if (bonus <= 0) return;

        List<RuntimeTile> blueTiles = new List<RuntimeTile>();

        foreach (RuntimeTile tile in tiles)
        {
            if (tile.IsAlive && tile.color == PixelColor.Blue)
                blueTiles.Add(tile);
        }

        HashSet<RuntimeTile> alreadyBoosted = new HashSet<RuntimeTile>();

        foreach (RuntimeTile blueTile in blueTiles)
        {
            foreach (RuntimeTile candidate in tiles)
            {
                if (!candidate.IsAlive || candidate == blueTile) continue;
                if (alreadyBoosted.Contains(candidate)) continue;
                if (!IsAdjacent(blueTile, candidate)) continue;

                candidate.maxDurability += bonus;
                candidate.currentDurability += bonus;
                alreadyBoosted.Add(candidate);
            }
        }
    }

    private static bool IsAdjacent(RuntimeTile a, RuntimeTile b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx <= 1 && dy <= 1 && (dx != 0 || dy != 0);
    }

    /// <summary>
    /// 초록 픽셀 중심 3x3(체비셰프 거리 1칸) 범위 안에 살아있는 픽셀을 대상으로
    /// 일정 주기마다 회복시킵니다. 여러 초록 픽셀의 범위가 겹치면 회복량이 중첩됩니다.
    /// </summary>
    private IEnumerator CoGreenRegenLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(config.greenRegenInterval);

            List<RuntimeTile> greenTiles = new List<RuntimeTile>();

            foreach (RuntimeTile tile in tiles)
            {
                if (tile.IsAlive && tile.color == PixelColor.Green)
                    greenTiles.Add(tile);
            }

            if (greenTiles.Count == 0) continue;

            bool anyHealed = false;

            foreach (RuntimeTile target in tiles)
            {
                if (!target.IsAlive) continue;

                int healAmount = 0;

                foreach (RuntimeTile green in greenTiles)
                {
                    if (IsWithinChebyshevRange(green, target, 1))
                        healAmount += config.greenRegenAmountPerTick;
                }

                if (healAmount <= 0) continue;

                int before = target.currentDurability;
                target.currentDurability = Mathf.Min(target.maxDurability, target.currentDurability + healAmount);

                if (target.currentDurability != before)
                    anyHealed = true;
            }

            if (anyHealed) StatsChanged?.Invoke();
        }
    }

    private static bool IsWithinChebyshevRange(RuntimeTile center, RuntimeTile target, int range)
    {
        int dx = Mathf.Abs(center.x - target.x);
        int dy = Mathf.Abs(center.y - target.y);
        return dx <= range && dy <= range;
    }

    /// <summary>
    /// 살아있는 픽셀 중 하나를 무작위로 골라 지정한 만큼 데미지를 적용합니다.
    /// 내구도가 0이 되면 해당 픽셀이 파괴되어 스프라이트에서도 사라지고,
    /// 이 색상의 살아있는 개수가 줄어들어 관련 패시브 능력치도 함께 감소합니다.
    /// </summary>
    public bool TryDamageRandomAliveTile(int damage)
    {
        List<RuntimeTile> aliveTiles = new List<RuntimeTile>();

        foreach (RuntimeTile tile in tiles)
        {
            if (tile.IsAlive) aliveTiles.Add(tile);
        }

        if (aliveTiles.Count == 0) return false;

        RuntimeTile target = aliveTiles[UnityEngine.Random.Range(0, aliveTiles.Count)];
        target.currentDurability -= damage;

        if (target.currentDurability <= 0)
        {
            target.currentDurability = 0;

            if (spawner != null) spawner.ClearPixelVisual(target.x, target.y);

            PixelBroken?.Invoke(target.x, target.y);
            RecalculatePassiveStats();
        }

        return true;
    }

    /// <summary>
    /// 몸의 가장 바깥쪽(외곽)에 노출된 살아있는 픽셀들에게 추가 내구도를 부여합니다.
    /// "외곽 픽셀"은 8방향 인접 칸 중 하나라도 살아있는 픽셀이 없는, 즉 적의 공격에
    /// 가장 먼저 노출되는 픽셀로 정의합니다. Unyielding(언이딩) 스킬 등에서 사용합니다.
    /// </summary>
    public void ApplyOuterTileBonus(int bonusHp)
    {
        if (bonusHp <= 0) return;

        List<RuntimeTile> outerTiles = GetOuterTilesInternal();
        if (outerTiles.Count == 0) return;

        foreach (RuntimeTile tile in outerTiles)
        {
            tile.maxDurability += bonusHp;
            tile.currentDurability += bonusHp;
        }

        StatsChanged?.Invoke();
    }

    private List<RuntimeTile> GetOuterTilesInternal()
    {
        List<RuntimeTile> result = new List<RuntimeTile>();

        foreach (RuntimeTile tile in tiles)
        {
            if (!tile.IsAlive) continue;
            if (IsOuterTile(tile)) result.Add(tile);
        }

        return result;
    }

    private bool IsOuterTile(RuntimeTile tile)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (!HasAliveTileAt(tile.x + dx, tile.y + dy)) return true;
            }
        }

        return false;
    }

    private bool HasAliveTileAt(int x, int y)
    {
        foreach (RuntimeTile tile in tiles)
        {
            if (tile.x == x && tile.y == y && tile.IsAlive) return true;
        }

        return false;
    }

    /// <summary>
    /// 픽셀이 하나라도 살아있는지 확인합니다. (게임 오버 판정 등에서 활용할 수 있습니다)
    /// </summary>
    public bool HasAnyAliveTile()
    {
        foreach (RuntimeTile tile in tiles)
        {
            if (tile.IsAlive) return true;
        }

        return false;
    }
}
