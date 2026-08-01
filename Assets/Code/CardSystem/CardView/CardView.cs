using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 데이터 바인딩과 기본 UI 참조만 담당합니다.
/// 테마와 상세 스크롤 처리는 partial 파일로 분리되어 있습니다.
/// </summary>
public sealed partial class CardView : MonoBehaviour
{
    [Header("Card Data UI")]
    [SerializeField] private TMP_Text cardTitle;
    [SerializeField] private Image cardArt;
    [SerializeField] private Image skillEffectIcon;

    [Tooltip("SkillAttributeIconR/UsingAmout의 TextMeshPro 텍스트입니다.")]
    [SerializeField] private TMP_Text usingAmoutText;

    /// <summary>
    /// 현재 표시 중인 카드 데이터입니다.
    /// </summary>
    public CardDefinition Definition { get; private set; }

    /// <summary>
    /// 캐러셀에서 카드 위치와 회전을 제어할 RectTransform입니다.
    /// </summary>
    public RectTransform RectTransform => transform as RectTransform;

    private void Awake()
    {
        ResolveReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    /// <summary>
    /// 연결되지 않은 UI를 카드 자식 계층에서 자동으로 찾습니다.
    /// </summary>
    [ContextMenu("카드 UI 자동 연결")]
    private void ResolveReferences()
    {
        ResolveText(transform, "CardTitle", ref cardTitle);

        if (cardArt == null)
            cardArt = FindComponent<Image>(transform, "CardArt");

        if (skillEffectIcon == null)
            skillEffectIcon = FindComponent<Image>(transform, "SkillEffectIcon");

        Transform attributeRoot =
            FindChildRecursive(transform, "SkillAttributeIconR");

        ResolveText(attributeRoot, "UsingAmout", ref usingAmoutText);
        ResolveDetailReferences();
    }

    /// <summary>
    /// 카드 정보를 UI에 적용합니다.
    /// OnUseText와 PassiveText의 프리팹 문구는 변경하지 않습니다.
    /// </summary>
    public void Bind(CardDefinition definition)
    {
        ResolveReferences();
        Definition = definition;

        if (definition == null)
        {
            ClearDynamicData();

            Debug.LogWarning(
                $"[CardView] '{name}'에 null 카드가 전달되었습니다.",
                this
            );
            return;
        }

        SetText(cardTitle, definition.DisplayName);
        SetImage(cardArt, definition.CardArt);
        SetImage(skillEffectIcon, definition.SkillEffectIcon);

        SetDetailTexts(
            definition.EffectDetailText,
            definition.PassiveDetailText
        );

        SetUsingAmout(definition.FormattedUsingAmout);
        ApplyTheme(definition.Color);

        // 비활성 상세 패널에서도 기본 Content 크기를 먼저 계산합니다.
        RefreshDetailLayout(false);
        RefreshDetailLayout(true);

        WarnMissingReferences();
    }

    /// <summary>
    /// 카드별로 바뀌는 값만 비웁니다.
    /// </summary>
    private void ClearDynamicData()
    {
        SetText(cardTitle, string.Empty);
        SetText(usingAmoutText, string.Empty);
        SetImage(cardArt, null);
        SetImage(skillEffectIcon, null);
        SetDetailTexts(string.Empty, string.Empty);
    }

    /// <summary>
    /// SkillAttributeIconR 아래의 UsingAmout 텍스트에 카드 값을 적용합니다.
    /// </summary>
    private void SetUsingAmout(string value)
    {
        value ??= string.Empty;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text.name != "UsingAmout" ||
                !HasAncestorNamed(text.transform, "SkillAttributeIconR"))
            {
                continue;
            }

            text.gameObject.SetActive(true);
            text.enabled = true;
            text.text = value;
            text.maxVisibleCharacters = int.MaxValue;
            text.ForceMeshUpdate(true, true);
        }
    }

    /// <summary>
    /// TMP 텍스트에 값을 안전하게 적용합니다.
    /// </summary>
    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    /// <summary>
    /// Image에 Sprite를 적용하고 표시 상태를 함께 변경합니다.
    /// </summary>
    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
