using TMPro;
using UnityEngine;

/// <summary>
/// CardView의 자동 참조 탐색과 누락 진단을 담당합니다.
/// </summary>
public sealed partial class CardView
{
    /// <summary>
    /// 지정한 이름의 자식에서 컴포넌트를 찾습니다.
    /// </summary>
    private static T FindComponent<T>(
        Transform root,
        string objectName
    ) where T : Component
    {
        Transform found = FindChildRecursive(root, objectName);

        if (found == null)
            return null;

        T component = found.GetComponent<T>();

        return component != null
            ? component
            : found.GetComponentInChildren<T>(true);
    }

    /// <summary>
    /// 지정한 이름의 자식에서 TMP 텍스트를 찾습니다.
    /// </summary>
    private static void ResolveText(
        Transform root,
        string objectName,
        ref TMP_Text text
    )
    {
        if (root == null || text != null)
            return;

        Transform found = FindChildRecursive(root, objectName);

        if (found == null)
            return;

        text = found.GetComponent<TMP_Text>();

        if (text == null)
            text = found.GetComponentInChildren<TMP_Text>(true);
    }

    /// <summary>
    /// 전체 자식 계층을 재귀적으로 탐색합니다.
    /// </summary>
    private static Transform FindChildRecursive(
        Transform root,
        string objectName
    )
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(
                root.GetChild(i),
                objectName
            );

            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// 필수 UI 누락을 Console에 표시합니다.
    /// </summary>
    private void WarnMissingReferences()
    {
        WarnIfMissing(cardTitle == null, "CardTitle");
        WarnIfMissing(cardArt == null, "CardArt");
        WarnIfMissing(skillEffectIcon == null, "SkillEffectIcon");
        WarnIfMissing(usingAmoutText == null, "SkillAttributeIconR/UsingAmout");
        WarnIfMissing(effectDetailText == null, "EffectDetail/CardDetailInformation");
        WarnIfMissing(passiveDetailText == null, "PassiveDetail/CardDetailInformation");
    }

    /// <summary>
    /// 누락된 UI와 카드 오브젝트를 함께 표시합니다.
    /// </summary>
    private void WarnIfMissing(bool missing, string uiName)
    {
        if (!missing)
            return;

        Debug.LogWarning(
            $"[CardView] '{name}'에서 '{uiName}' UI를 찾지 못했습니다.",
            this
        );
    }
}
