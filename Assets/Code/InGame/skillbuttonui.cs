using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스킬 버튼 하나에 붙는 범용 컨트롤러입니다.
/// 어떤 스킬을 담당할지는 인스펙터의 skillType 필드로 지정합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class SkillButtonUI : MonoBehaviour
{
    [Header("이 버튼이 담당하는 스킬")]
    public PlayerSkillType skillType;

    [Header("참조")]
    [SerializeField] private PlayerSkillController skillController;

    [Header("UI 요소")]
    [Tooltip("쿨타임 동안 위에서 덮이는 이미지. Image Type을 Filled(방식 Radial 360 등)로 설정해두세요.")]
    [SerializeField] private Image cooldownOverlay;
    [Tooltip("스킬 코스트(필요 픽셀 개수)를 표시할 텍스트. 없으면 비워둬도 됩니다.")]
    [SerializeField] private TextMeshProUGUI costText;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);

        if (skillController == null)
        {
            skillController = FindObjectOfType<PlayerSkillController>();
        }
    }

    private void Start()
    {
        if (costText != null && skillController != null)
        {
            costText.text = skillController.GetPixelCost(skillType).ToString();
        }
    }

    private void Update()
    {
        if (skillController == null || GameManager.Instance == null) return;

        UpdateCooldownVisual();
        UpdateInteractable();
    }

    private void UpdateCooldownVisual()
    {
        if (cooldownOverlay == null) return;

        float duration = skillController.GetCooldownDuration(skillType);
        float remaining = skillController.GetRemainingCooldown(skillType);

        if (duration <= 0f)
        {
            cooldownOverlay.fillAmount = 0f;
            return;
        }

        cooldownOverlay.fillAmount = Mathf.Clamp01(remaining / duration);
    }

    private void UpdateInteractable()
    {
        bool onCooldown = skillController.IsOnCooldown(skillType);

        int cost = skillController.GetPixelCost(skillType);
        PixelColor color = skillController.GetPixelColor(skillType);
        int owned = GameManager.Instance.GetOwnedPixelCount(color);
        bool affordable = owned >= cost;

        button.interactable = !onCooldown && affordable;
    }

    private void HandleClick()
    {
        if (skillController == null) return;
        skillController.ActivateSkill(skillType);
    }
}
