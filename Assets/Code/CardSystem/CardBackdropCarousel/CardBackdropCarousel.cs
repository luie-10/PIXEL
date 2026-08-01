using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 데이터, 슬롯 구성, 선택 상태와 씬 전달을 관리합니다.
/// 입력 이벤트는 Events 파일, 시각 연출은 Animation 파일에 분리되어 있습니다.
/// </summary>
public sealed partial class CardBackdropCarousel : MonoBehaviour
{
    // 어떤 상세 패널을 열지 구분하기 위한 내부 열거형입니다.
    private enum DetailKind
    {
        Effect,
        Passive
    }

    // 아래 항목은 카드 데이터 추첨과 테스트 재현에 사용합니다.
    [Header("Card Data")]
    [SerializeField] private CardCatalog cardCatalog;

    [Tooltip("테스트할 때 같은 카드 조합을 재현합니다.")]
    [SerializeField] private bool useFixedRandomSeed;

    [SerializeField] private int randomSeed = 12345;

    // 중앙, 왼쪽, 오른쪽 슬롯의 CardView를 연결해 주시면 됩니다.
    [Header("Card Views")]
    [SerializeField] private CardView cardView;
    [SerializeField] private CardView cardViewL;
    [SerializeField] private CardView cardViewR;

    [Tooltip("CardView를 직접 연결하지 않았을 때 자동으로 찾을 중앙 카드 오브젝트 이름입니다.")]
    [SerializeField] private string cardViewObjectName = "CardBackdrop";

    [Tooltip("CardView를 직접 연결하지 않았을 때 자동으로 찾을 왼쪽 카드 오브젝트 이름입니다.")]
    [SerializeField] private string cardViewLObjectName = "CardBackdropL";

    [Tooltip("CardView를 직접 연결하지 않았을 때 자동으로 찾을 오른쪽 카드 오브젝트 이름입니다.")]
    [SerializeField] private string cardViewRObjectName = "CardBackdropR";

    // 선택한 카드를 실제 캐릭터에 전달할 대상입니다.
    [Header("Card Receiver")]
    [SerializeField] private CharacterCardLoadout targetCharacter;

    [Header("Arrow Images")]
    [SerializeField] private Image leftButton;
    [SerializeField] private Image rightButton;

    // RerollButton과 내부 Desc를 연결해 주시면 됩니다.
    // 비워 두면 Canvas에서 같은 이름의 오브젝트를 자동으로 찾습니다.
    [Header("Reroll")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollDesc;
    [SerializeField] private string rerollButtonObjectName = "RerollButton";
    [SerializeField] private string rerollDescObjectName = "Desc";
    [SerializeField, Min(0)] private int maxRerolls = 3;

    [Header("Canvas Objects")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Image backShadow;
    [SerializeField] private string backShadowObjectName = "BackShadow";

    [Header("Rendering Order")]
    [Tooltip("중앙 또는 선택된 카드를 Hierarchy의 마지막 자식으로 보내 가장 앞에 표시합니다.")]
    [SerializeField] private bool keepFrontCardOnTop = true;

    public event Action<CardDefinition> CardConfirmed;



    /// <summary>
    /// 필수 참조를 검사하고 카드 슬롯, 이벤트, 랜덤 카드 데이터를 초기화합니다.
    /// </summary>
    private void Awake()
    {
        ResolveCardViews();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        Canvas.ForceUpdateCanvases();

        orderedCards = new[]
        {
            BuildRuntimeCard(cardViewL),
            BuildRuntimeCard(cardView),
            BuildRuntimeCard(cardViewR)
        };

        slotPoses = new[]
        {
            new SlotPose(cardViewL.RectTransform),
            new SlotPose(cardView.RectTransform),
            new SlotPose(cardViewR.RectTransform)
        };

        FindCanvasAndBackShadow();
        InitializeRerollButton();

        for (int i = 0; i < orderedCards.Length; i++)
        {
            RegisterCardEvents(orderedCards[i]);
            RegisterConfirmEvents(orderedCards[i]);
            RegisterDetailSelectorEvents(orderedCards[i]);
            ResetSelectorVisuals(orderedCards[i]);
        }

        RegisterImageClick(leftButton, MoveLeft);
        RegisterImageClick(rightButton, MoveRight);

        DealRandomCards();
        BringFrontCardToTop();

        // ponytail: 별도 테스트 프레임워크 대신 필수 런타임 구성을 즉시 검증합니다.
        Debug.Assert(
            orderedCards.Length == 3,
            "[CardBackdropCarousel] 카드 슬롯은 정확히 3개여야 합니다.",
            this
        );
    }

    /// <summary>
    /// 런타임에 등록한 RerollButton 이벤트를 안전하게 해제합니다.
    /// </summary>
    private void OnDestroy()
    {
        if (rerollButton != null)
            rerollButton.onClick.RemoveListener(RequestReroll);
    }

    /// <summary>
    /// 상세 설명의 자동 스크롤과 키보드 카드 전환 입력을 처리합니다.
    /// </summary>
    private void Update()
    {
        UpdateActiveDetailAutoScroll();

        if (!CanMoveCards())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.A))
        {
            MoveLeft();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) ||
                 Input.GetKeyDown(KeyCode.D))
        {
            MoveRight();
        }
    }

    /// <summary>
    /// 런타임에 생성된 캐릭터의 카드 장착 컴포넌트를 연결합니다.
    /// </summary>
    public void SetTargetCharacter(
        CharacterCardLoadout target
    )
    {
        targetCharacter = target;
    }

    /// <summary>
    /// CardCatalog에서 중복 없이 카드 3장을 뽑아 각 CardView에 표시합니다.
    /// </summary>
    public void DealRandomCards()
    {
        DealRandomCards(false);
    }

    /// <summary>
    /// 리롤 시에는 현재 표시 중인 카드를 가능한 한 제외하여 새 카드를 우선 배정합니다.
    /// 카드 수가 부족하면 기존 카드로 빈 슬롯을 채웁니다.
    /// </summary>
    private void DealRandomCards(bool avoidCurrentCards)
    {
        if (cardCatalog == null)
        {
            Debug.LogError(
                "[CardBackdropCarousel] CardCatalog가 연결되지 않았습니다.",
                this
            );
            return;
        }

        List<CardDefinition> excludedCards = null;

        if (avoidCurrentCards)
        {
            excludedCards = new List<CardDefinition>(orderedCards.Length);

            for (int i = 0; i < orderedCards.Length; i++)
            {
                CardDefinition current = orderedCards[i].View.Definition;

                if (current != null)
                    excludedCards.Add(current);
            }
        }

        int? seed = useFixedRandomSeed
            ? randomSeed + dealSequence
            : null;

        dealSequence++;

        List<CardDefinition> drawnCards = cardCatalog.DrawUnique(
            orderedCards.Length,
            seed,
            excludedCards
        );

        if (drawnCards.Count < orderedCards.Length)
        {
            Debug.LogError(
                $"[CardBackdropCarousel] 카드가 부족합니다. " +
                $"필요: {orderedCards.Length}, 현재: {drawnCards.Count}",
                this
            );
        }

        for (int i = 0; i < orderedCards.Length; i++)
        {
            CardDefinition definition = i < drawnCards.Count
                ? drawnCards[i]
                : null;

            orderedCards[i].View.Bind(definition);

            if (definition != null)
            {
                Debug.Log(
                    $"[CardBackdropCarousel] 슬롯 {i}에 카드 적용: " +
                    $"{definition.DisplayName} ({definition.CardId})",
                    orderedCards[i].View
                );
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// 직접 연결되지 않은 카드 뷰를 Canvas 안에서 이름으로 찾아 연결합니다.
    /// 카드 루트에 CardView가 없다면 자동으로 추가합니다.
    /// </summary>
    private void ResolveCardViews()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        Transform searchRoot = targetCanvas != null
            ? targetCanvas.transform
            : transform.root;

        cardView = ResolveCardView(
            cardView,
            searchRoot,
            cardViewObjectName
        );

        cardViewL = ResolveCardView(
            cardViewL,
            searchRoot,
            cardViewLObjectName
        );

        cardViewR = ResolveCardView(
            cardViewR,
            searchRoot,
            cardViewRObjectName
        );
    }

    /// <summary>
    /// 지정한 이름의 카드 루트를 찾고 CardView를 반환합니다.
    /// </summary>
    private CardView ResolveCardView(
        CardView current,
        Transform searchRoot,
        string objectName
    )
    {
        if (current != null)
            return current;

        if (searchRoot == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform found = FindChildRecursive(searchRoot, objectName);

        if (found == null)
        {
            Debug.LogError(
                $"[CardBackdropCarousel] '{objectName}' 오브젝트를 찾지 못했습니다.",
                this
            );
            return null;
        }

        CardView view = found.GetComponent<CardView>();

        if (view == null)
        {
            view = found.gameObject.AddComponent<CardView>();
            Debug.Log(
                $"[CardBackdropCarousel] '{objectName}'에 CardView를 자동으로 추가했습니다.",
                found
            );
        }

        return view;
    }

    /// <summary>
    /// 중앙·왼쪽·오른쪽 CardView가 모두 연결되어 있는지 확인합니다.
    /// </summary>
    private bool ValidateReferences()
    {
        bool valid =
            cardView != null &&
            cardViewL != null &&
            cardViewR != null;

        if (!valid)
        {
            Debug.LogError(
                "[CardBackdropCarousel] CardView 3개를 모두 연결해야 합니다.",
                this
            );
        }

        return valid;
    }

    /// <summary>
    /// 카드 루트 아래의 버튼, 텍스트, 아이콘, 상세 패널 참조를 찾아 캐시합니다.
    /// </summary>
    private RuntimeCard BuildRuntimeCard(
        CardView view
    )
    {
        RectTransform rect =
            view.RectTransform;

        CanvasGroup canvasGroup =
            rect.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                rect.gameObject.AddComponent<CanvasGroup>();
        }

        RectTransform confirmRect =
            FindRectTransform(
                rect,
                confirmObjectName
            );

        Graphic confirmGraphic =
            FindGraphic(confirmRect);

        RectTransform onUseRect =
            FindRectTransform(
                rect,
                onUseTextObjectName
            );

        Graphic onUseGraphic =
            FindGraphic(onUseRect);

        RectTransform passiveTextRect =
            FindRectTransform(
                rect,
                passiveTextObjectName
            );

        Graphic passiveTextGraphic =
            FindGraphic(passiveTextRect);

        Graphic skillEffectIcon =
            FindGraphic(
                FindRectTransform(
                    rect,
                    skillEffectIconObjectName
                )
            );

        RuntimeCard runtime =
            new RuntimeCard
            {
                View = view,
                Rect = rect,
                CanvasGroup = canvasGroup,
                OriginalAlpha = canvasGroup.alpha,

                ConfirmRect = confirmRect,
                ConfirmGraphic = confirmGraphic,
                ConfirmOriginalScale =
                    confirmRect != null
                        ? confirmRect.localScale
                        : Vector3.one,
                ConfirmOriginalAlpha =
                    confirmGraphic != null
                        ? confirmGraphic.color.a
                        : 1f,

                OnUseRect = onUseRect,
                OnUseGraphic = onUseGraphic,
                OnUseOriginalScale =
                    onUseRect != null
                        ? onUseRect.localScale
                        : Vector3.one,

                SkillEffectIcon = skillEffectIcon,

                PassiveTextRect = passiveTextRect,
                PassiveTextGraphic = passiveTextGraphic,
                PassiveTextOriginalScale =
                    passiveTextRect != null
                        ? passiveTextRect.localScale
                        : Vector3.one,

                EffectDetail =
                    BuildDetailState(
                        FindRectTransform(
                            rect,
                            effectDetailObjectName
                        )
                    ),

                PassiveDetail =
                    BuildDetailState(
                        FindRectTransform(
                            rect,
                            passiveDetailObjectName
                        )
                    )
            };

        return runtime;
    }

    /// <summary>
    /// 상세 패널 상태를 만들고 ScrollRect의 기본 세로 스크롤을 활성화합니다.
    /// 자동 스크롤과 마우스 휠/드래그 스크롤을 함께 사용할 수 있습니다.
    /// </summary>
    private DetailState BuildDetailState(
        RectTransform detailRect
    )
    {
        if (detailRect == null)
        {
            return null;
        }

        ScrollRect scrollRect =
            detailRect.GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
            scrollRect =
                detailRect.GetComponentInChildren<ScrollRect>(true);
        }

        if (scrollRect != null)
        {
            scrollRect.enabled = true;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.inertia = false;
            scrollRect.movementType =
                ScrollRect.MovementType.Clamped;
            scrollRect.StopMovement();
        }

        DetailState state =
            new DetailState
            {
                Rect = detailRect,
                ScrollRect = scrollRect
            };

        detailRect.localRotation =
            Quaternion.Euler(
                detailEndRotation
            );

        detailRect.gameObject.SetActive(false);

        return state;
    }

    /// <summary>
    /// 다른 애니메이션 또는 선택 상태가 없어 카드 전환이 가능한지 확인합니다.
    /// </summary>
    private bool CanMoveCards()
    {
        return
            !isMoving &&
            !isSelecting &&
            !isConfirmAnimating &&
            !isDetailAnimating &&
            !hasConfirmedCard &&
            selectedCard == null;
    }

    /// <summary>
    /// 현재 중앙 슬롯의 카드를 Hierarchy 마지막 자식으로 보내 가장 앞에 표시합니다.
    /// </summary>
    private void BringFrontCardToTop()
    {
        BringCardToTop(GetFrontCard());
    }

    /// <summary>
    /// 지정한 카드를 같은 부모의 마지막 자식으로 보내 가장 앞에 표시합니다.
    /// </summary>
    private void BringCardToTop(RuntimeCard card)
    {
        if (!keepFrontCardOnTop ||
            card == null ||
            card.Rect == null)
        {
            return;
        }

        card.Rect.SetAsLastSibling();
    }

    /// <summary>
    /// 현재 중앙 슬롯에 배치된 카드를 반환합니다.
    /// </summary>
    private RuntimeCard GetFrontCard()
    {
        return orderedCards[1];
    }

    /// <summary>
    /// 자식 계층에서 이름이 일치하는 RectTransform을 찾아 반환합니다.
    /// </summary>
    private static RectTransform FindRectTransform(
        Transform parent,
        string targetName
    )
    {
        return FindChildRecursive(
            parent,
            targetName
        ) as RectTransform;
    }

    /// <summary>
    /// 대상 또는 대상의 자식에서 첫 번째 Graphic 컴포넌트를 찾습니다.
    /// </summary>
    private static Graphic FindGraphic(
        RectTransform target
    )
    {
        if (target == null)
        {
            return null;
        }

        Graphic graphic =
            target.GetComponent<Graphic>();

        if (graphic == null)
        {
            graphic =
                target.GetComponentInChildren<Graphic>(true);
        }

        return graphic;
    }

    /// <summary>
    /// 전체 자식 계층을 재귀적으로 탐색하여 이름이 일치하는 Transform을 찾습니다.
    /// </summary>
    private static Transform FindChildRecursive(
        Transform parent,
        string targetName
    )
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
            {
                return child;
            }

            Transform result =
                FindChildRecursive(
                    child,
                    targetName
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
