using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Inspector에서도 CardDefinition을 전달받을 수 있도록 만든 UnityEvent 형식입니다.
/// </summary>
[Serializable]
public sealed class CardDefinitionEvent : UnityEvent<CardDefinition>
{
}

/// <summary>
/// 캐릭터가 장착한 카드 한 장을 보관하고 해당 카드의 ScriptableObject 효과를 적용합니다.
/// 새 카드가 들어오면 기존 카드 효과를 제거한 뒤 교체하므로 여러 카드가 중첩되지 않습니다.
/// </summary>
public sealed class CharacterCardLoadout : MonoBehaviour
{
    [Header("Current Card")]
    [SerializeField] private CardDefinition equippedCard;

    [Header("Effect Target")]
    [Tooltip("효과를 적용할 대상입니다. 비워 두면 이 오브젝트를 사용합니다.")]
    [SerializeField] private GameObject effectTarget;

    [Header("Events")]
    [SerializeField] private CardDefinitionEvent onCardAssigned =
        new CardDefinitionEvent();

    [SerializeField] private UnityEvent onCardCleared =
        new UnityEvent();

    public CardDefinition EquippedCard => equippedCard;
    public string EquippedCardId => equippedCard != null
        ? equippedCard.CardId
        : string.Empty;

    public event Action<CardDefinition> CardAssigned;
    public event Action CardCleared;

    private bool effectsApplied;

    private void Awake()
    {
        if (effectTarget == null)
            effectTarget = gameObject;
    }

    /// <summary>
    /// 카드 한 장만 장착합니다.
    /// 이미 다른 카드가 있다면 기존 효과를 제거한 뒤 새 효과를 적용합니다.
    /// </summary>
    public bool AssignCard(CardDefinition card)
    {
        if (card == null)
        {
            Debug.LogWarning(
                "[CharacterCardLoadout] null 카드는 장착할 수 없습니다.",
                this
            );
            return false;
        }

        if (equippedCard == card && effectsApplied)
            return true;

        if (effectsApplied)
            RemoveEffects(equippedCard);

        equippedCard = card;
        ApplyEffects(card);
        effectsApplied = true;

        onCardAssigned.Invoke(card);
        CardAssigned?.Invoke(card);

        Debug.Log(
            $"[CharacterCardLoadout] 카드 장착: {card.DisplayName} ({card.CardId})",
            this
        );

        return true;
    }

    /// <summary>
    /// 현재 카드의 효과를 제거하고 장착 상태를 비웁니다.
    /// </summary>
    public void ClearCard()
    {
        if (equippedCard == null) return;

        if (effectsApplied)
            RemoveEffects(equippedCard);

        equippedCard = null;
        effectsApplied = false;

        onCardCleared.Invoke();
        CardCleared?.Invoke();
    }

    /// <summary>
    /// 저장된 Card ID를 카탈로그에서 찾아 다시 장착합니다.
    /// </summary>
    public bool RestoreFromId(CardCatalog catalog, string cardId)
    {
        if (catalog == null)
        {
            Debug.LogWarning(
                "[CharacterCardLoadout] CardCatalog가 없습니다.",
                this
            );
            return false;
        }

        CardDefinition card = catalog.FindById(cardId);

        if (card == null)
        {
            Debug.LogWarning(
                $"[CharacterCardLoadout] Card ID를 찾지 못했습니다: {cardId}",
                this
            );
            return false;
        }

        return AssignCard(card);
    }

    /// <summary>
    /// 카드에 등록된 모든 효과를 순서대로 적용합니다.
    /// </summary>
    private void ApplyEffects(CardDefinition card)
    {
        IReadOnlyList<CardEffect> effects = card.Effects;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null)
                effects[i].Apply(effectTarget);
        }
    }

    /// <summary>
    /// 적용 순서의 반대로 효과를 제거하여 중첩 의존성을 안전하게 되돌립니다.
    /// </summary>
    private void RemoveEffects(CardDefinition card)
    {
        if (card == null) return;

        IReadOnlyList<CardEffect> effects = card.Effects;

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (effects[i] != null)
                effects[i].Remove(effectTarget);
        }
    }
}
