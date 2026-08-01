using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 한 장의 기본 정보, 상세 설명, 색상 테마, 실제 게임 효과를 보관합니다.
/// Project 창에서 Create > Cards > Card Definition으로 생성하면 됩니다.
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Definition")]
public sealed class CardDefinition : ScriptableObject
{
    /// <summary>
    /// 카드에서 사용할 네 가지 색상 테마입니다.
    /// </summary>
    public enum CardColor
    {
        None,
        Red,
        Blue,
        Yellow,
        Green
    }

    /// <summary>
    /// UsingAmout를 횟수 또는 직접 작성한 문자열로 표시합니다.
    /// </summary>
    public enum UsingAmoutMode
    {
        Count,
        Custom
    }

    // 저장 및 네트워크 전달에 사용할 고유 ID와 화면 표시 이름입니다.
    [Header("Identity")]
    [SerializeField] private string cardId;
    [SerializeField] private string displayName;

    // 카드 일러스트와 앞면 스킬 아이콘입니다.
    [Header("Visual")]
    [SerializeField] private Sprite cardArt;
    [SerializeField] private Sprite skillEffectIcon;

    // 카드 프리팹 전체에 적용할 색상 테마입니다.
    [Header("Color Theme")]
    [SerializeField] private CardColor color = CardColor.Blue;

    // OnUseText와 PassiveText의 고정 문구는 변경하지 않고 상세 설명만 카드별로 변경합니다.
    [Header("Detail Text Only")]
    [TextArea(3, 12)]
    [SerializeField] private string effectDetailText;

    [TextArea(3, 12)]
    [SerializeField] private string passiveDetailText;

    // SkillAttributeIconR 아래의 UsingAmout에 표시할 값을 카드별로 설정합니다.
    [Header("Skill Attribute")]
    [Tooltip("Count를 선택하면 'X 숫자' 형식으로 표시합니다.")]
    [SerializeField]
    private UsingAmoutMode usingAmoutMode =
        UsingAmoutMode.Count;

    [Tooltip("SkillAttributeIconR/UsingAmout에 표시할 값입니다. 예: 1, 3, 25%")]
    [SerializeField] private string usingAmout;

    // 카드가 장착될 때 적용할 ScriptableObject 효과 목록입니다.
    [Header("Gameplay Effects")]
    [SerializeField] private List<CardEffect> effects = new List<CardEffect>();

    // 외부 코드에서는 데이터를 읽기만 할 수 있도록 제공합니다.
    public string CardId => cardId;
    public string DisplayName => displayName;
    public Sprite CardArt => cardArt;
    public Sprite SkillEffectIcon => skillEffectIcon;
    public CardColor Color => color;
    public string EffectDetailText => effectDetailText;
    public string PassiveDetailText => passiveDetailText;
    public string UsingAmout => usingAmout;

    /// <summary>
    /// Count일 때는 항상 'X 숫자', Custom일 때는 입력값 그대로 반환합니다.
    /// </summary>
    public string FormattedUsingAmout
    {
        get
        {
            string value = usingAmout?.Trim();

            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (usingAmoutMode != UsingAmoutMode.Count)
                return value;

            // 사용자가 이미 X, x, ×를 입력해도 중복되지 않도록 정리합니다.
            value = value.TrimStart('X', 'x', '×', ' ');

            return $"X {value}";
        }
    }

    public IReadOnlyList<CardEffect> Effects => effects;

#if UNITY_EDITOR
    /// <summary>
    /// Inspector에서 값이 수정될 때 ID 공백과 중복 효과를 정리합니다.
    /// </summary>
    private void OnValidate()
    {
        cardId = cardId?.Trim();
        usingAmout = usingAmout?.Trim();

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (effects[i] == null || effects.IndexOf(effects[i]) != i)
                effects.RemoveAt(i);
        }
    }
#endif
}