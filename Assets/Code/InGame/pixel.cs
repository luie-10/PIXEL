using UnityEngine;

/// <summary>
/// 색상별 픽셀 내구도와, 설치된 픽셀 개수에 따라 선형으로 강화되는
/// 패시브 능력치 계수를 한 곳에 모아 관리하는 밸런스 데이터입니다.
/// Project 창에서 Create > Pixel > Pixel Color Config로 생성한 뒤,
/// Player 프리팹의 PlayerPixelBody / PlayerHealthController에 연결해 주세요.
/// </summary>
[CreateAssetMenu(fileName = "PixelColorConfig", menuName = "Pixel/Pixel Color Config")]
public class PixelColorConfig : ScriptableObject
{
    [System.Serializable]
    public class DurabilityEntry
    {
        public PixelColor color;
        [Min(1)] public int maxDurability = 20;
    }

    [Header("색상별 기본 내구도")]
    [Tooltip("Black/None은 파괴되지 않는 것으로 취급하므로 목록에 넣지 않아도 됩니다.")]
    [SerializeField]
    private DurabilityEntry[] durabilityTable = new DurabilityEntry[]
    {
        new DurabilityEntry { color = PixelColor.Red,    maxDurability = 20 },
        new DurabilityEntry { color = PixelColor.Blue,   maxDurability = 50 },
        new DurabilityEntry { color = PixelColor.Yellow, maxDurability = 20 },
        new DurabilityEntry { color = PixelColor.Green,  maxDurability = 30 },
    };

    [Header("빨강(공격) - 배치 개수당 선형 강화")]
    [Tooltip("빨간 픽셀 1개당 증가하는 공격력")]
    public float redAttackPerPixel = 5f;

    [Header("노랑(기동) - 배치 개수당 선형 강화")]
    [Tooltip("노란 픽셀 1개당 증가하는 이동속도 배율 (0.03 = 3%)")]
    public float yellowMoveSpeedPercentPerPixel = 0.03f;

    [Header("초록(재생) - 배치 개수당 선형 강화")]
    [Tooltip("초록 픽셀 1개당 증가하는 픽셀 수집(자석) 범위")]
    public float greenPickupRangePerPixel = 0.1f;

    [Tooltip("초록 픽셀 중심 3x3(반경 1칸) 범위 내 픽셀 내구도 회복 주기(초)")]
    public float greenRegenInterval = 5f;

    [Tooltip("회복 주기마다 회복되는 내구도. 초록 픽셀 범위가 겹치면 각각 더해져 중첩됩니다.")]
    public int greenRegenAmountPerTick = 3;

    [Header("파랑(방어) - 라운드 시작 시 인접 픽셀 보너스")]
    [Tooltip("파란 픽셀과 인접한 픽셀에 라운드 시작 시 부여되는 추가 체력 = 배치된 파란 픽셀 수 / 이 값")]
    public float blueAdjacentBonusDivider = 2f;

    [Header("피격 무적")]
    [Tooltip("적과 접촉해 데미지를 입은 뒤 무적으로 유지되는 시간(초)")]
    public float hitInvincibleDuration = 0.2f;

    [Tooltip("무적 시간 동안 스프라이트가 깜빡이는 간격(초)")]
    public float hitFlashInterval = 0.05f;

    /// <summary>
    /// 지정한 색상의 기본 최대 내구도를 반환합니다.
    /// 목록에 없는 색상(Black, None 등)은 파괴되지 않는 것으로 간주해 매우 큰 값을 반환합니다.
    /// </summary>
    public int GetBaseDurability(PixelColor color)
    {
        for (int i = 0; i < durabilityTable.Length; i++)
        {
            if (durabilityTable[i].color == color)
                return durabilityTable[i].maxDurability;
        }

        return int.MaxValue;
    }
}
