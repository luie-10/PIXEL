using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 색상과 테마별 Sprite 변경을 담당합니다.
/// CardPrefab 루트, CardArt, 카드 데이터용 SkillEffectIcon은 변경하지 않습니다.
/// </summary>
public sealed partial class CardView
{
    /// <summary>
    /// 일반 Image 한 개의 색상별 Sprite를 보관합니다.
    /// </summary>
    [Serializable]
    private sealed class ThemeSpriteBinding
    {
        [SerializeField] private Image target;
        [SerializeField] private Sprite blue;
        [SerializeField] private Sprite red;
        [SerializeField] private Sprite yellow;
        [SerializeField] private Sprite green;

        public Image Target => target;

        public ThemeSpriteBinding(Image image)
        {
            target = image;
            blue = image != null ? image.sprite : null;
            red = blue;
            yellow = blue;
            green = blue;
        }

        public Sprite Get(CardDefinition.CardColor color)
        {
            return SelectSprite(
                color,
                blue,
                red,
                yellow,
                green
            );
        }
    }

    [Header("SkillAttributeButtons Sprites")]
    [SerializeField] private Sprite iconBlueType;
    [SerializeField] private Sprite iconRedType;
    [SerializeField] private Sprite iconYellowType;
    [SerializeField] private Sprite iconGreenType;

    [Header("SkillAttributeIconR Sprites")]
    [SerializeField] private Sprite attributeIconBlue;
    [SerializeField] private Sprite attributeIconRed;
    [SerializeField] private Sprite attributeIconYellow;
    [SerializeField] private Sprite attributeIconGreen;

    [Header("OnUseTextInside SkillEffectIcon Sprites")]
    [SerializeField] private Sprite onUseSkillIconBlue;
    [SerializeField] private Sprite onUseSkillIconRed;
    [SerializeField] private Sprite onUseSkillIconYellow;
    [SerializeField] private Sprite onUseSkillIconGreen;

    [Header("PassiveTextInside PassiveIcon Sprites")]
    [SerializeField] private Sprite passiveIconBlue;
    [SerializeField] private Sprite passiveIconRed;
    [SerializeField] private Sprite passiveIconYellow;
    [SerializeField] private Sprite passiveIconGreen;

    [Header("Other Theme Image Sprites")]
    [SerializeField] private List<ThemeSpriteBinding> themeSpriteBindings =
        new List<ThemeSpriteBinding>();

    [Header("Text Darkness")]
    [Range(0f, 1f)]
    [SerializeField] private float normalTextDarkness = 0.42f;

    [Range(0f, 1f)]
    [SerializeField] private float detailTextDarkness = 0.88f;

    /// <summary>
    /// 테마 Sprite가 필요한 일반 Image를 자동 등록합니다.
    /// 이미 입력된 색상별 Sprite는 유지합니다.
    /// </summary>
    [ContextMenu("스프라이트 변경 대상 자동 등록")]
    private void CollectThemeSpriteTargets()
    {
        ResolveReferences();

        List<ThemeSpriteBinding> previous =
            new List<ThemeSpriteBinding>(themeSpriteBindings);

        themeSpriteBindings.Clear();

        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (!CanReplaceSprite(image))
                continue;

            ThemeSpriteBinding existing = previous.Find(
                binding => binding != null &&
                           binding.Target == image
            );

            themeSpriteBindings.Add(
                existing ?? new ThemeSpriteBinding(image)
            );
        }

        Debug.Log(
            $"[CardView] '{name}'에 Sprite 변경 대상 {themeSpriteBindings.Count}개를 등록했습니다.",
            this
        );
    }

    /// <summary>
    /// 카드 루트는 유지하고 텍스트, 지정 이미지, 일반 Sprite 테마를 적용합니다.
    /// </summary>
    public void ApplyTheme(CardDefinition.CardColor cardColor)
    {
        Color themeColor = GetThemeColor(cardColor);
        Color normalTextColor = Color.Lerp(
            themeColor,
            Color.black,
            normalTextDarkness
        );

        Color detailColor = Color.Lerp(
            themeColor,
            Color.black,
            detailTextDarkness
        );

        // 색상 변경 대상입니다. 모든 상세 패널의 CardBorder도 포함됩니다.
        ApplyNamedImageColors("Confirm", themeColor);
        ApplyNamedImageColors("CardBorder", themeColor);
        ApplyNamedImageColors("SkillEffectIcon2", themeColor);

        ApplyAllTextColors(normalTextColor);
        ApplyGraphicColor(effectDetailText, detailColor);
        ApplyGraphicColor(passiveDetailText, detailColor);

        ApplyNamedSprite(
            "SkillAttributeButtons",
            SelectSprite(
                cardColor,
                iconBlueType,
                iconRedType,
                iconYellowType,
                iconGreenType
            )
        );

        ApplyNamedSprite(
            "SkillAttributeIconR",
            SelectSprite(
                cardColor,
                attributeIconBlue,
                attributeIconRed,
                attributeIconYellow,
                attributeIconGreen
            )
        );

        ApplyNestedNamedSprite(
            "OnUseTextInside",
            "SkillEffectIcon",
            SelectSprite(
                cardColor,
                onUseSkillIconBlue,
                onUseSkillIconRed,
                onUseSkillIconYellow,
                onUseSkillIconGreen
            )
        );

        ApplyNestedNamedSprite(
            "PassiveTextInside",
            "PassiveIcon",
            SelectSprite(
                cardColor,
                passiveIconBlue,
                passiveIconRed,
                passiveIconYellow,
                passiveIconGreen
            )
        );

        ApplyThemeSprites(cardColor);
    }

    /// <summary>
    /// 카드 색상에 대응하는 테마 색상을 반환합니다.
    /// </summary>
    private static Color GetThemeColor(CardDefinition.CardColor color)
    {
        switch (color)
        {
            case CardDefinition.CardColor.Red:
                return new Color32(0xF0, 0x08, 0x69, 0xFF);

            case CardDefinition.CardColor.Yellow:
                return new Color32(0xF0, 0xD4, 0x08, 0xFF);

            case CardDefinition.CardColor.Green:
                return new Color32(0x12, 0xBC, 0x76, 0xFF);

            default:
                return new Color32(0x66, 0x95, 0xFF, 0xFF);
        }
    }

    /// <summary>
    /// 네 Sprite 중 현재 카드 색상에 맞는 항목을 반환합니다.
    /// </summary>
    private static Sprite SelectSprite(
        CardDefinition.CardColor color,
        Sprite blue,
        Sprite red,
        Sprite yellow,
        Sprite green
    )
    {
        switch (color)
        {
            case CardDefinition.CardColor.Red:
                return red;

            case CardDefinition.CardColor.Yellow:
                return yellow;

            case CardDefinition.CardColor.Green:
                return green;

            default:
                return blue;
        }
    }

    /// <summary>
    /// 지정한 이름의 모든 Image에 색상을 적용합니다.
    /// </summary>
    private void ApplyNamedImageColors(string objectName, Color color)
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name == objectName)
                ApplyGraphicColor(images[i], color);
        }
    }

    /// <summary>
    /// 카드 내부의 모든 TMP 텍스트에 테마 색상을 적용합니다.
    /// </summary>
    private void ApplyAllTextColors(Color color)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
            ApplyGraphicColor(texts[i], color);
    }

    /// <summary>
    /// 이름이 같은 모든 Image에 지정 Sprite를 적용합니다.
    /// </summary>
    private void ApplyNamedSprite(string objectName, Sprite sprite)
    {
        if (sprite == null)
        {
            WarnMissingSprite(objectName);
            return;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        bool applied = false;

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name != objectName)
                continue;

            ApplySprite(images[i], sprite);
            applied = true;
        }

        if (!applied)
            WarnMissingImage(objectName);
    }

    /// <summary>
    /// 지정한 부모 내부의 특정 Image에 Sprite를 적용합니다.
    /// </summary>
    private void ApplyNestedNamedSprite(
        string parentName,
        string imageName,
        Sprite sprite
    )
    {
        if (sprite == null)
        {
            WarnMissingSprite($"{parentName}/{imageName}");
            return;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        bool applied = false;

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image.name != imageName ||
                !HasAncestorNamed(image.transform, parentName))
            {
                continue;
            }

            ApplySprite(image, sprite);
            applied = true;
        }

        if (!applied)
            WarnMissingImage($"{parentName}/{imageName}");
    }

    /// <summary>
    /// 일반 테마 이미지 목록에 색상별 Sprite를 적용합니다.
    /// </summary>
    private void ApplyThemeSprites(CardDefinition.CardColor color)
    {
        for (int i = 0; i < themeSpriteBindings.Count; i++)
        {
            ThemeSpriteBinding binding = themeSpriteBindings[i];

            if (binding == null ||
                binding.Target == null ||
                !CanReplaceSprite(binding.Target))
            {
                continue;
            }

            Sprite sprite = binding.Get(color);

            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[CardView] '{binding.Target.name}'의 {color} Sprite가 비어 있습니다.",
                    binding.Target
                );
                continue;
            }

            ApplySprite(binding.Target, sprite);
        }
    }

    /// <summary>
    /// 일반 Sprite 교체 대상인지 확인합니다.
    /// </summary>
    private bool CanReplaceSprite(Image image)
    {
        if (image == null ||
            image.transform == transform ||
            image == cardArt ||
            image == skillEffectIcon)
        {
            return false;
        }

        if (image.name == "SkillEffectIcon" &&
            HasAncestorNamed(image.transform, "OnUseTextInside"))
        {
            return false;
        }

        if (image.name == "PassiveIcon" &&
            HasAncestorNamed(image.transform, "PassiveTextInside"))
        {
            return false;
        }

        return image.name != "Confirm" &&
               image.name != "CardBorder" &&
               image.name != "SkillEffectIcon2" &&
               image.name != "SkillAttributeButtons" &&
               image.name != "SkillAttributeIconR";
    }

    /// <summary>
    /// 대상의 부모 계층에 지정 이름이 존재하는지 확인합니다.
    /// </summary>
    private bool HasAncestorNamed(
        Transform target,
        string ancestorName
    )
    {
        Transform current = target != null
            ? target.parent
            : null;

        while (current != null && current != transform.parent)
        {
            if (current.name == ancestorName)
                return true;

            if (current == transform)
                break;

            current = current.parent;
        }

        return false;
    }

    /// <summary>
    /// 기존 알파를 유지하고 RGB만 변경합니다.
    /// </summary>
    private static void ApplyGraphicColor(Graphic graphic, Color color)
    {
        if (graphic == null)
            return;

        color.a = graphic.color.a;
        graphic.color = color;
    }

    /// <summary>
    /// Sprite를 적용하고 기존 알파를 유지한 채 색상 틴트를 제거합니다.
    /// </summary>
    private static void ApplySprite(Image image, Sprite sprite)
    {
        image.sprite = sprite;

        Color white = Color.white;
        white.a = image.color.a;
        image.color = white;
    }

    private void WarnMissingSprite(string targetName)
    {
        Debug.LogWarning(
            $"[CardView] '{name}'의 '{targetName}'용 Sprite가 비어 있습니다.",
            this
        );
    }

    private void WarnMissingImage(string targetName)
    {
        Debug.LogWarning(
            $"[CardView] '{name}'에서 '{targetName}' Image를 찾지 못했습니다.",
            this
        );
    }
}
