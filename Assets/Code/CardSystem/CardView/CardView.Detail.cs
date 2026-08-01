using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EffectDetail과 PassiveDetail의 텍스트 및 ScrollRect 레이아웃을 담당합니다.
/// </summary>
public sealed partial class CardView
{
    [Header("Detail Text")]
    [SerializeField] private TMP_Text effectDetailText;
    [SerializeField] private TMP_Text passiveDetailText;

    [Header("Detail Layout")]
    [Min(0f)]
    [SerializeField] private float minimumDetailTextHeight = 40f;

    [Min(0f)]
    [SerializeField] private float detailVerticalPadding = 24f;

    [Min(0f)]
    [SerializeField] private float detailHorizontalPadding = 12f;

    /// <summary>
    /// 상세 설명 텍스트 참조를 각 패널 내부에서 찾습니다.
    /// </summary>
    private void ResolveDetailReferences()
    {
        Transform effectRoot =
            FindChildRecursive(transform, "EffectDetail");

        Transform passiveRoot =
            FindChildRecursive(transform, "PassiveDetail");

        ClearReferenceOutsideRoot(effectRoot, ref effectDetailText);
        ClearReferenceOutsideRoot(passiveRoot, ref passiveDetailText);

        ResolveText(
            effectRoot,
            "CardDetailInformation",
            ref effectDetailText
        );

        ResolveText(
            passiveRoot,
            "CardDetailInformation",
            ref passiveDetailText
        );
    }

    /// <summary>
    /// 두 상세 설명문을 교체합니다.
    /// </summary>
    private void SetDetailTexts(
        string effectText,
        string passiveText
    )
    {
        SetDetailText(effectDetailText, effectText);
        SetDetailText(passiveDetailText, passiveText);
    }

    /// <summary>
    /// 상세 텍스트가 전체 문장을 표시하도록 상태를 초기화합니다.
    /// </summary>
    private static void SetDetailText(
        TMP_Text text,
        string value
    )
    {
        if (text == null)
            return;

        text.gameObject.SetActive(true);
        text.enabled = true;
        text.text = value ?? string.Empty;
        text.maxVisibleCharacters = int.MaxValue;
        text.maxVisibleLines = 99999;

        Color color = text.color;
        color.a = 1f;
        text.color = color;

        text.ForceMeshUpdate(true, true);
    }

    /// <summary>
    /// 지정한 상세 패널의 ScrollRect 레이아웃을 다시 계산합니다.
    /// </summary>
    public void RefreshDetailLayout(bool passive)
    {
        ResolveReferences();

        RefreshDetailLayout(
            passive
                ? passiveDetailText
                : effectDetailText
        );
    }

    /// <summary>
    /// 텍스트를 Viewport 왼쪽 위에 배치하고 Content 높이를 맞춥니다.
    /// </summary>
    private void RefreshDetailLayout(TMP_Text text)
    {
        if (text == null)
            return;

        RectTransform textRect = text.rectTransform;
        ScrollRect scrollRect =
            FindParentComponent<ScrollRect>(textRect.transform);

        if (scrollRect == null)
        {
            Debug.LogWarning(
                $"[CardView] '{textRect.name}'의 부모에서 ScrollRect를 찾지 못했습니다.",
                textRect
            );
            return;
        }

        RectTransform viewport = scrollRect.viewport;

        if (viewport == null)
        {
            viewport = FindChildRecursive(
                scrollRect.transform,
                "Viewport"
            ) as RectTransform;

            scrollRect.viewport = viewport;
        }

        RectTransform content = scrollRect.content;

        if (content == null)
        {
            content = FindChildRecursive(
                scrollRect.transform,
                "Content"
            ) as RectTransform;

            if (content == null)
                content = textRect.parent as RectTransform;

            scrollRect.content = content;
        }

        if (viewport == null || content == null)
        {
            Debug.LogWarning(
                $"[CardView] '{scrollRect.name}'의 Viewport 또는 Content를 찾지 못했습니다.",
                scrollRect
            );
            return;
        }

        if (viewport.GetComponent<RectMask2D>() == null &&
            viewport.GetComponent<Mask>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
        }

        scrollRect.enabled = true;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.StopMovement();

        Canvas.ForceUpdateCanvases();

        float viewportWidth = Mathf.Max(viewport.rect.width, 1f);
        float viewportHeight = Mathf.Max(viewport.rect.height, 1f);
        float textWidth = Mathf.Max(
            viewportWidth - detailHorizontalPadding * 2f - 2f,
            1f
        );

        DisableAutomaticLayout(content);
        DisableAutomaticLayout(textRect);

        // Stretch 대신 실제 Viewport 너비를 사용하여 오른쪽 잘림을 방지합니다.
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.localScale = Vector3.one;
        content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            viewportWidth
        );

        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.localScale = Vector3.one;

        text.gameObject.SetActive(true);
        text.enabled = true;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.margin = Vector4.zero;
        text.maxVisibleCharacters = int.MaxValue;
        text.maxVisibleLines = 99999;
        text.ForceMeshUpdate(true, true);

        float preferredHeight = Mathf.Max(
            minimumDetailTextHeight,
            text.GetPreferredValues(
                text.text,
                textWidth,
                Mathf.Infinity
            ).y
        );

        float topPadding = detailVerticalPadding * 0.5f;

        textRect.anchoredPosition = new Vector2(
            detailHorizontalPadding,
            -topPadding
        );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            textWidth
        );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            preferredHeight
        );

        content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(
                viewportHeight,
                preferredHeight + detailVerticalPadding
            )
        );

        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition = 1f;

        // ponytail: 복잡한 레이아웃 테스트 대신 핵심 불변식만 실행 중 확인합니다.
        Debug.Assert(
            content.rect.height + 0.5f >= viewport.rect.height,
            "[CardView] 상세 Content 높이는 Viewport보다 작을 수 없습니다.",
            this
        );
    }

    /// <summary>
    /// 수동 RectTransform 계산과 충돌하는 자동 레이아웃을 끕니다.
    /// </summary>
    private static void DisableAutomaticLayout(RectTransform rect)
    {
        if (rect == null)
            return;

        ContentSizeFitter fitter =
            rect.GetComponent<ContentSizeFitter>();

        if (fitter != null)
            fitter.enabled = false;

        LayoutGroup layoutGroup =
            rect.GetComponent<LayoutGroup>();

        if (layoutGroup != null)
            layoutGroup.enabled = false;
    }

    /// <summary>
    /// 부모 계층에서 지정한 컴포넌트를 찾습니다.
    /// </summary>
    private static T FindParentComponent<T>(
        Transform child
    ) where T : Component
    {
        Transform current = child;

        while (current != null)
        {
            T component = current.GetComponent<T>();

            if (component != null)
                return component;

            current = current.parent;
        }

        return null;
    }

    /// <summary>
    /// 잘못 저장된 상세 텍스트 참조를 초기화합니다.
    /// </summary>
    private static void ClearReferenceOutsideRoot(
        Transform root,
        ref TMP_Text text
    )
    {
        if (root != null &&
            text != null &&
            text.transform != root &&
            !text.transform.IsChildOf(root))
        {
            text = null;
        }
    }
}
