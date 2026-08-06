using UnityEngine;

// 조작 방식 Enum
public enum ControlType
{
    RotateAndMove = 0, // 회전하며 전진 (기본)
    FlipAnd8Way = 1    // 8방향 이동 + 좌우 반전
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Current Settings")]
    public ControlType currentControlType = ControlType.RotateAndMove;

    [Header("Round Management")]
    [SerializeField] private int currentRoundIndex = 0; // 0부터 시작하는 인덱스
    public int CurrentRoundIndex => currentRoundIndex;      // 현재 라운드 인덱스 (0, 1, 2...)
    public int CurrentRound => currentRoundIndex + 1;        // 실제 표시용 라운드 번호 (1, 2, 3...)

    private const string ControlTypeKey = "SavedControlType";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 변경 시 파괴되지 않음
            LoadSettings(); // 기존 설정 불러오기
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// RoundTimer 등에서 라운드가 시작될 때 라운드 인덱스를 설정합니다.
    /// </summary>
    public void SetRoundIndex(int index)
    {
        currentRoundIndex = index;
        Debug.Log($"[SettingsManager] 현재 라운드 설정됨: {CurrentRound}라운드 (Index: {currentRoundIndex})");
    }

    /// <summary>
    /// 새로운 게임을 시작할 때 라운드를 1라운드(인덱스 0)로 초기화합니다.
    /// </summary>
    public void ResetRound()
    {
        currentRoundIndex = 0;
        Debug.Log("[SettingsManager] 라운드가 1라운드로 초기화되었습니다.");
    }

    // 설정 저장 (UI에서 호출)
    public void SaveSettings(ControlType newType)
    {
        currentControlType = newType;
        PlayerPrefs.SetInt(ControlTypeKey, (int)currentControlType);
        PlayerPrefs.Save();
        Debug.Log($"[SettingsManager] 조작 방식 저장 완료: {currentControlType}");
    }

    // 설정 불러오기
    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(ControlTypeKey))
        {
            currentControlType = (ControlType)PlayerPrefs.GetInt(ControlTypeKey);
        }
        else
        {
            currentControlType = ControlType.RotateAndMove; // 기본값
        }
    }
}