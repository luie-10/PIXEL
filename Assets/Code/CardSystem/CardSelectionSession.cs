using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필수!

/// <summary>
/// 선택한 카드 한 장을 씬 전환 뒤에도 유지하며, 3_Make_Pixel 씬으로 전환하는 세션입니다.
/// 직접 배치하지 않아도 첫 카드 확정 시 자동으로 생성됩니다.
/// </summary>
public sealed class CardSelectionSession : MonoBehaviour
{
    public static CardSelectionSession Instance { get; private set; }

    [SerializeField] private CardDefinition selectedCard;

    public CardDefinition SelectedCard => selectedCard;
    public string SelectedCardId => selectedCard != null
        ? selectedCard.CardId
        : string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 선택한 카드 한 장을 저장하고 '3_Make_Pixel' 씬으로 전환합니다.
    /// 세션이 없으면 자동으로 생성합니다.
    /// </summary>
    public static void SetSelectedCard(CardDefinition card)
    {
        if (card == null) return;

        // 1. 카드 데이터 저장
        GetOrCreate().selectedCard = card;
        Debug.Log($"[CardSelectionSession] 카드 선택 완료: {card.CardId}");

        // 2. '3_Make_Pixel' 씬으로 이동
        SceneManager.LoadScene("3_Make_Pixel");
    }

    /// <summary>
    /// 현재 선택 정보를 제거합니다.
    /// </summary>
    public static void ClearSelectedCard()
    {
        if (Instance != null)
            Instance.selectedCard = null;
    }

    /// <summary>
    /// 세션 인스턴스를 반환하며 아직 없다면 새 루트 오브젝트로 생성합니다.
    /// </summary>
    private static CardSelectionSession GetOrCreate()
    {
        if (Instance != null) return Instance;

        GameObject sessionObject = new GameObject(
            nameof(CardSelectionSession)
        );

        return sessionObject.AddComponent<CardSelectionSession>();
    }
}