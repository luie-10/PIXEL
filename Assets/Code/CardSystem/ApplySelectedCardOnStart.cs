using UnityEngine;

/// <summary>
/// 게임 씬에서 이전 씬에 저장된 카드 한 장을 현재 캐릭터에 적용합니다.
/// CharacterCardLoadout과 같은 캐릭터 오브젝트에 추가해 주시면 됩니다.
/// </summary>
public sealed class ApplySelectedCardOnStart : MonoBehaviour
{
    [SerializeField] private CharacterCardLoadout loadout;

    private void Awake()
    {
        if (loadout == null)
            loadout = GetComponent<CharacterCardLoadout>();
    }

    private void Start()
    {
        ApplySelectedCard();
    }

    /// <summary>
    /// 세션에 카드가 있으면 CharacterCardLoadout에 전달합니다.
    /// </summary>
    public bool ApplySelectedCard()
    {
        CardDefinition card = CardSelectionSession.Instance != null
            ? CardSelectionSession.Instance.SelectedCard
            : null;

        if (loadout == null || card == null)
        {
            Debug.LogWarning(
                "[ApplySelectedCardOnStart] Loadout 또는 선택 카드가 없습니다.",
                this
            );
            return false;
        }

        return loadout.AssignCard(card);
    }
}
