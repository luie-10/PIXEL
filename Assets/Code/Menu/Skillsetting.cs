using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 화면에서 스킬 키를 다시 지정하는 버튼입니다.
/// 버튼을 누르면 다음 키 입력을 기다리는 상태가 되고,
/// 그 상태에서 누른 키가 이 스킬에 새로 지정됩니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class SkillKeyBindButton : MonoBehaviour
{
    [Header("이 버튼이 담당하는 스킬")]
    public PlayerSkillType skillType;
    // 기존 [Header("UI 요소")] 아래에 추가
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color unboundColor = Color.red;

    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private string waitingMessage = "키 입력...";

    private Button button;
    private bool isWaitingForInput = false;

    // KeyCode의 모든 값을 한 번만 캐싱해서 매 프레임 GC 발생을 막습니다.
    private static readonly KeyCode[] AllKeyCodes = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(BeginRebind);
    }

    private void OnEnable()
    {
        RefreshText();

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SkillKeyBindingsChanged += RefreshText;
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SkillKeyBindingsChanged -= RefreshText;

        isWaitingForInput = false;
    }

    private void Update()
    {
        if (!isWaitingForInput) return;

        // ESC를 누르면 리바인딩을 취소합니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isWaitingForInput = false;
            RefreshText();
            return;
        }

        foreach (KeyCode code in AllKeyCodes)
        {
            if (code == KeyCode.None) continue;

            // 마우스 왼쪽 클릭은 이 버튼 자체를 누르는 동작과 겹치므로 제외합니다.
            if (code == KeyCode.Mouse0) continue;

            if (Input.GetKeyDown(code))
            {
                CompleteRebind(code);
                return;
            }
        }
    }

    private void BeginRebind()
    {
        if (isWaitingForInput) return;

        isWaitingForInput = true;

        if (keyText != null)
            keyText.text = waitingMessage;
    }

    private void CompleteRebind(KeyCode newKey)
    {
        isWaitingForInput = false;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetSkillKey(skillType, newKey);

        RefreshText();
    }

    private void RefreshText()
    {
        if (keyText == null || SettingsManager.Instance == null) return;

        KeyCode current = SettingsManager.Instance.GetSkillKey(skillType);
        bool isUnbound = current == KeyCode.None;

        keyText.text = isUnbound ? "미설정" : current.ToString();
        keyText.color = isUnbound ? unboundColor : normalColor;
    }

}
