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