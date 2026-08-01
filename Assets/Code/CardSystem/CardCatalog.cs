using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프로젝트에서 사용할 카드 에셋을 등록하고 무작위 추첨 및 ID 검색을 제공합니다.
/// </summary>
[CreateAssetMenu(fileName = "CardCatalog", menuName = "Cards/Card Catalog")]
public sealed class CardCatalog : ScriptableObject
{
    [SerializeField] private List<CardDefinition> cards = new List<CardDefinition>();

    public IReadOnlyList<CardDefinition> Cards => cards;

    /// <summary>
    /// 중복 없이 카드를 추첨합니다.
    /// excludedCards가 있으면 해당 카드를 가능한 한 뒤로 미뤄 리롤 중 같은 카드가 다시 나오는 일을 줄입니다.
    /// </summary>
    public List<CardDefinition> DrawUnique(
        int count,
        int? seed = null,
        IEnumerable<CardDefinition> excludedCards = null
    )
    {
        HashSet<CardDefinition> excluded = excludedCards != null
            ? new HashSet<CardDefinition>(excludedCards)
            : new HashSet<CardDefinition>();

        HashSet<CardDefinition> used = new HashSet<CardDefinition>();
        List<CardDefinition> preferred = new List<CardDefinition>();
        List<CardDefinition> fallback = new List<CardDefinition>();

        for (int i = 0; i < cards.Count; i++)
        {
            CardDefinition card = cards[i];
            if (card == null || !used.Add(card)) continue;

            (excluded.Contains(card) ? fallback : preferred).Add(card);
        }

        System.Random random = new System.Random(
            seed ?? Guid.NewGuid().GetHashCode()
        );

        Shuffle(preferred, random);
        Shuffle(fallback, random);
        preferred.AddRange(fallback);

        count = Mathf.Clamp(count, 0, preferred.Count);
        return preferred.GetRange(0, count);
    }

    /// <summary>
    /// 저장된 Card ID와 일치하는 카드 에셋을 반환합니다.
    /// </summary>
    public CardDefinition FindById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId)) return null;

        for (int i = 0; i < cards.Count; i++)
        {
            CardDefinition card = cards[i];

            if (card != null &&
                string.Equals(card.CardId, cardId, StringComparison.Ordinal))
            {
                return card;
            }
        }

        return null;
    }

    /// <summary>
    /// Fisher-Yates 방식으로 목록을 섞습니다.
    /// </summary>
    private static void Shuffle(
        List<CardDefinition> list,
        System.Random random
    )
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int index = random.Next(i + 1);
            CardDefinition temporary = list[i];
            list[i] = list[index];
            list[index] = temporary;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Inspector 수정 시 비어 있거나 중복된 Card ID를 검사합니다.
    /// </summary>
    private void OnValidate()
    {
        HashSet<string> usedIds = new HashSet<string>();

        for (int i = 0; i < cards.Count; i++)
        {
            CardDefinition card = cards[i];
            if (card == null) continue;

            if (string.IsNullOrWhiteSpace(card.CardId))
            {
                Debug.LogWarning(
                    $"[CardCatalog] '{card.name}'의 Card ID가 비어 있습니다.",
                    card
                );
            }
            else if (!usedIds.Add(card.CardId))
            {
                Debug.LogError(
                    $"[CardCatalog] 중복 Card ID: {card.CardId}",
                    card
                );
            }
        }
    }
#endif
}
