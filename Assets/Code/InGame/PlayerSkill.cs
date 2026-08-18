using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerPixelBody))]
public class PlayerSkillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPixelBody pixelBody;
    [SerializeField] private PlayerPixelMagnet magnet;

    [Header("Red Rush (Red)")]
    [SerializeField] private int redRushCost = 5;
    [SerializeField] private float redRushCooldown = 10f;
    [SerializeField] private float redRushDamageRatio = 0.15f;
    [SerializeField] private float redRushDashDistanceCells = 3f;
    [SerializeField] private float redRushDashDuration = 0.2f;
    [SerializeField] private float redRushHitRadius = 0.6f;
    [Tooltip("레드러쉬 연출이 플레이어 뒤쪽으로 얼마나 떨어져서 따라올지(칸 단위)")]
    [SerializeField] private float redRushTrailOffsetCells = 1f;

    [Header("Unyielding (Blue)")]
    [SerializeField] private int unyieldingCost = 5;
    [SerializeField] private float unyieldingCooldown = 30f;
    [Tooltip("Unyielding 연출이 화면에 유지되는 시간(초). 버프 자체는 영구지만 연출은 잠깐만 보여줍니다.")]
    [SerializeField] private float unyieldingEffectLifeTime = 1f;

    [Header("Push (Yellow)")]
    [SerializeField] private int pushCost = 10;
    [SerializeField] private float pushCooldown = 60f;
    [SerializeField] private float pushRangeCells = 3f;
    [SerializeField] private float pushDistanceCells = 3f;
    [SerializeField] private float pushStunDuration = 0.5f;
    [SerializeField] private LayerMask enemyLayerMask;
    [Tooltip("Push 연출이 화면에 유지되는 시간(초)")]
    [SerializeField] private float pushEffectLifeTime = 0.6f;

    [Header("Magnetic Boost (Green)")]
    [SerializeField] private int magnetBoostCost = 8;
    [SerializeField] private float magnetBoostCooldown = 60f;
    [SerializeField] private float magnetBoostMultiplier = 2.5f;
    [SerializeField] private float magnetBoostDuration = 8f;

    [Header("Skill VFX Prefabs")]
    [SerializeField] private GameObject redRushEffectPrefab;
    [SerializeField] private GameObject unyieldingEffectPrefab;
    [SerializeField] private GameObject pushEffectPrefab;
    [SerializeField] private GameObject magnetBoostEffectPrefab;

    [Header("Skill VFX Scale")]
    [Tooltip("연출 크기 = 플레이어 현재 크기 × 이 값. 프리팹 원본 크기가 이미 적당하다면 1로 둡니다.")]
    [SerializeField] private float effectScaleMultiplier = 1f;

    private readonly Dictionary<PlayerSkillType, float> cooldownTimers = new Dictionary<PlayerSkillType, float>();
    private bool isDashing;

    private void Awake()
    {
        if (pixelBody == null) pixelBody = GetComponent<PlayerPixelBody>();
        if (magnet == null) magnet = GetComponent<PlayerPixelMagnet>();

        foreach (PlayerSkillType type in System.Enum.GetValues(typeof(PlayerSkillType)))
        {
            cooldownTimers[type] = 0f;
        }
    }

    private void Update()
    {
        foreach (PlayerSkillType type in System.Enum.GetValues(typeof(PlayerSkillType)))
        {
            if (cooldownTimers[type] > 0f)
            {
                cooldownTimers[type] -= Time.deltaTime;
            }
        }

        if (SettingsManager.Instance == null) return;

        foreach (PlayerSkillType type in System.Enum.GetValues(typeof(PlayerSkillType)))
        {
            KeyCode key = SettingsManager.Instance.GetSkillKey(type);
            if (key != KeyCode.None && Input.GetKeyDown(key))
            {
                ActivateSkill(type);
            }
        }
    }

    public bool ActivateSkill(PlayerSkillType type)
    {
        switch (type)
        {
            case PlayerSkillType.RedRush: return TryActivateRedRush();
            case PlayerSkillType.Unyielding: return TryActivateUnyielding();
            case PlayerSkillType.Push: return TryActivatePush();
            case PlayerSkillType.MagnetBoost: return TryActivateMagnetBoost();
            default: return false;
        }
    }

    public float GetCooldownDuration(PlayerSkillType type)
    {
        switch (type)
        {
            case PlayerSkillType.RedRush: return redRushCooldown;
            case PlayerSkillType.Unyielding: return unyieldingCooldown;
            case PlayerSkillType.Push: return pushCooldown;
            case PlayerSkillType.MagnetBoost: return magnetBoostCooldown;
            default: return 0f;
        }
    }

    public float GetRemainingCooldown(PlayerSkillType type) => Mathf.Max(0f, cooldownTimers[type]);

    public bool IsOnCooldown(PlayerSkillType type) => cooldownTimers[type] > 0f;

    public int GetPixelCost(PlayerSkillType type)
    {
        switch (type)
        {
            case PlayerSkillType.RedRush: return redRushCost;
            case PlayerSkillType.Unyielding: return unyieldingCost;
            case PlayerSkillType.Push: return pushCost;
            case PlayerSkillType.MagnetBoost: return magnetBoostCost;
            default: return 0;
        }
    }

    public PixelColor GetPixelColor(PlayerSkillType type)
    {
        switch (type)
        {
            case PlayerSkillType.RedRush: return PixelColor.Red;
            case PlayerSkillType.Unyielding: return PixelColor.Blue;
            case PlayerSkillType.Push: return PixelColor.Yellow;
            case PlayerSkillType.MagnetBoost: return PixelColor.Green;
            default: return PixelColor.Black;
        }
    }

    // ==========================================
    // Red Rush : 전방 대시 + 경로상의 적에게 데미지
    // 연출은 플레이어 뒤쪽에서 방향을 맞춰 따라오다가, 대시가 끝나는 순간 함께 삭제됩니다.
    // ==========================================
    private bool TryActivateRedRush()
    {
        if (cooldownTimers[PlayerSkillType.RedRush] > 0f || isDashing) return false;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendPixels(PixelColor.Red, redRushCost)) return false;

        cooldownTimers[PlayerSkillType.RedRush] = redRushCooldown;

        float offsetWorld = redRushTrailOffsetCells * PixelGameConstants.CellToWorld;
        SpawnSkillEffect(redRushEffectPrefab, "RedRush", redRushDashDuration, offsetWorld);

        StartCoroutine(CoRedRushDash());
        return true;
    }

    private IEnumerator CoRedRushDash()
    {
        isDashing = true;

        Vector3 dashDir = transform.up;
        float distanceWorld = redRushDashDistanceCells * PixelGameConstants.CellToWorld;
        Vector3 start = transform.position;
        Vector3 end = start + dashDir * distanceWorld;

        HashSet<Enemy> alreadyHit = new HashSet<Enemy>();
        float elapsed = 0f;

        while (elapsed < redRushDashDuration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / redRushDashDuration);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, redRushHitRadius);
            foreach (Collider2D hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null && !alreadyHit.Contains(enemy))
                {
                    alreadyHit.Add(enemy);
                    enemy.TakeDamage(pixelBody.CurrentAttack * redRushDamageRatio);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        isDashing = false;
    }

    // ==========================================
    // Unyielding : 파란 픽셀 소모 후 외곽 타일에 내구도 보너스
    // ==========================================
    private bool TryActivateUnyielding()
    {
        if (cooldownTimers[PlayerSkillType.Unyielding] > 0f) return false;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendPixels(PixelColor.Blue, unyieldingCost)) return false;

        cooldownTimers[PlayerSkillType.Unyielding] = unyieldingCooldown;

        int ownedBlueAfterCost = GameManager.Instance.GetOwnedPixelCount(PixelColor.Blue);
        int bonusHp = ownedBlueAfterCost / 2;
        pixelBody.ApplyOuterTileBonus(bonusHp);

        SpawnSkillEffect(unyieldingEffectPrefab, "Unyielding", unyieldingEffectLifeTime, 0f);
        return true;
    }

    // ==========================================
    // Push : 범위 내 적 밀치기 + 스턴
    // ==========================================
    private bool TryActivatePush()
    {
        if (cooldownTimers[PlayerSkillType.Push] > 0f) return false;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendPixels(PixelColor.Yellow, pushCost)) return false;

        cooldownTimers[PlayerSkillType.Push] = pushCooldown;

        float rangeWorld = pushRangeCells * PixelGameConstants.CellToWorld;
        float distanceWorld = pushDistanceCells * PixelGameConstants.CellToWorld;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, rangeWorld, enemyLayerMask);
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) continue;

            Vector2 dir = (enemy.transform.position - transform.position);
            enemy.ApplyKnockback(dir, distanceWorld, 0.2f);
            enemy.Stun(pushStunDuration);
        }

        SpawnSkillEffect(pushEffectPrefab, "Push", pushEffectLifeTime, 0f);
        return true;
    }

    // ==========================================
    // Magnetic Boost : 일정 시간 픽셀 수집 범위 배율 증가
    // 연출은 버프 지속 시간(magnetBoostDuration)과 정확히 같은 시간만큼 유지된 뒤 삭제됩니다.
    // ==========================================
    private bool TryActivateMagnetBoost()
    {
        if (cooldownTimers[PlayerSkillType.MagnetBoost] > 0f) return false;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendPixels(PixelColor.Green, magnetBoostCost)) return false;

        cooldownTimers[PlayerSkillType.MagnetBoost] = magnetBoostCooldown;

        if (magnet != null)
        {
            magnet.SetTemporaryMultiplier(magnetBoostMultiplier, magnetBoostDuration);
        }

        SpawnSkillEffect(magnetBoostEffectPrefab, "MagnetBoost", magnetBoostDuration, 0f);
        return true;
    }

    // ==========================================
    // 공통 이펙트 소환 : 플레이어 크기에 비례해 스케일을 맞추고,
    // 지정된 lifeTime(=스킬 지속 시간)이 끝나면 자동으로 삭제됩니다.
    // followOffsetWorld가 0보다 크면 플레이어 뒤쪽에서 따라오는 잔상 연출이 됩니다.
    // ==========================================
    private void SpawnSkillEffect(GameObject prefab, string skillName, float lifeTime, float followOffsetWorld)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[{skillName}] 이펙트 프리팹 슬롯이 비어있어 연출이 생성되지 않습니다. 인스펙터에서 프리팹을 연결해 주세요.");
            return;
        }

        GameObject spawned = Instantiate(prefab, transform.position, transform.rotation);

        PlayerEffectFollower follower = spawned.GetComponent<PlayerEffectFollower>();
        if (follower == null) follower = spawned.AddComponent<PlayerEffectFollower>();
        follower.Setup(transform, followOffsetWorld, effectScaleMultiplier);

        Destroy(spawned, lifeTime);
    }
}
