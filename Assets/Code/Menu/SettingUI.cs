using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject settingsPanel;

    [Header("Toggle References")]
    public Toggle rotateModeToggle;
    public Toggle flipModeToggle;

    private void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("[SettingsUI] Start: settingsPanel 비활성화 완료");
        }
        else
        {
            Debug.LogError("[SettingsUI] 오류: settingsPanel이 할당되지 않았습니다!");
        }
    }

    public void OpenPanel()
    {
        Debug.Log("[SettingsUI] OpenPanel() 함수 진입함");

        if (settingsPanel == null)
        {
            Debug.LogError("[SettingsUI] 오류: settingsPanel이 Null입니다. UI 패널 오브젝트를 연결해 주세요.");
            return;
        }

        settingsPanel.SetActive(true);
        Debug.Log($"[SettingsUI] settingsPanel 활성화 상태: {settingsPanel.activeSelf}");

        InitToggleState();
    }

    private void InitToggleState()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("[SettingsUI] SettingsManager 싱글톤 인스턴스를 찾을 수 없습니다.");
            return;
        }

        ControlType current = SettingsManager.Instance.currentControlType;
        Debug.Log($"[SettingsUI] 현재 설정된 조작 방식: {current}");

        if (rotateModeToggle != null)
            rotateModeToggle.isOn = (current == ControlType.RotateAndMove);

        if (flipModeToggle != null)
            flipModeToggle.isOn = (current == ControlType.FlipAnd8Way);
    }

    public void CloseAndSavePanel()
    {
        Debug.Log("[SettingsUI] CloseAndSavePanel() 호출됨");
        SaveCurrentToggleSelection();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void SaveCurrentToggleSelection()
    {
        if (SettingsManager.Instance == null) return;

        ControlType selectedType = ControlType.RotateAndMove;

        if (rotateModeToggle != null && rotateModeToggle.isOn)
        {
            selectedType = ControlType.RotateAndMove;
        }
        else if (flipModeToggle != null && flipModeToggle.isOn)
        {
            selectedType = ControlType.FlipAnd8Way;
        }

        SettingsManager.Instance.SaveSettings(selectedType);
    }
}