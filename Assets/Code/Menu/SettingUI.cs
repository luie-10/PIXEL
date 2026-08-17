using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject settingsPanel;

    [Header("Toggle References")]
    public Toggle rotateModeToggle;
    public Toggle flipModeToggle;

    [Header("Skill Key Warning")]
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private string warningMessage = "설정되지 않은 키가 있습니다";
    [SerializeField] private float warningDisplayDuration = 2f;

    private Coroutine warningCoroutine;

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

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    public void OpenPanel()
    {
        Debug.Log("[SettingsUI] OpenPanel() 함수 실행됨");

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
            Debug.LogWarning("[SettingsUI] SettingsManager 싱글턴 인스턴스를 찾을 수 없습니다.");
            return;
        }

        ControlType current = SettingsManager.Instance.currentControlType;
        Debug.Log($"[SettingsUI] 현재 저장된 조작 방식: {current}");

        if (rotateModeToggle != null)
            rotateModeToggle.isOn = (current == ControlType.RotateAndMove);

        if (flipModeToggle != null)
            flipModeToggle.isOn = (current == ControlType.FlipAnd8Way);
    }

    public void CloseAndSavePanel()
    {
        Debug.Log("[SettingsUI] CloseAndSavePanel() 호출됨");

        // 스킬 키 중 미설정된 키가 있으면 닫지 않고 경고만 표시합니다.
        if (SettingsManager.Instance != null && SettingsManager.Instance.HasAnyUnboundSkillKey())
        {
            Debug.Log("[SettingsUI] 미설정 스킬 키가 존재하여 패널을 닫지 않습니다.");
            ShowUnboundKeyWarning();
            return;
        }

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

    private void ShowUnboundKeyWarning()
    {
        if (warningText == null) return;

        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(CoShowWarning());
    }

    private IEnumerator CoShowWarning()
    {
        warningText.text = warningMessage;
        warningText.color = Color.red;
        warningText.gameObject.SetActive(true);

        yield return new WaitForSeconds(warningDisplayDuration);

        warningText.gameObject.SetActive(false);
        warningCoroutine = null;
    }
}
