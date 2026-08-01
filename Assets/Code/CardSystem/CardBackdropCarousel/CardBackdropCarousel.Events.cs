using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 카드 클릭, 화살표, 상세 패널, Confirm, 리롤 입력을 담당합니다.
/// </summary>
public sealed partial class CardBackdropCarousel
{
    /// <summary>
    /// 부모 Canvas와 BackShadow를 찾고 카드 외부 클릭 닫기 이벤트를 등록합니다.
    /// </summary>
    private void FindCanvasAndBackShadow()
    {
        if (targetCanvas == null)
        {
            targetCanvas =
                cardView.GetComponentInParent<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogWarning(
                "[CardBackdropCarousel] 부모 Canvas를 찾지 못했습니다.",
                this
            );

            return;
        }

        if (backShadow == null)
        {
            Transform found =
                FindChildRecursive(
                    targetCanvas.transform,
                    backShadowObjectName
                );

            if (found != null)
            {
                backShadow =
                    found.GetComponent<Image>();
            }
        }

        if (backShadow == null)
        {
            Debug.LogWarning(
                $"[CardBackdropCarousel] '{backShadowObjectName}' Image를 찾지 못했습니다.",
                this
            );

            return;
        }

        SetGraphicAlpha(backShadow, 0f);
        SetBackShadowInteractive(false);

        EventTrigger trigger =
            GetOrCreateEventTrigger(
                backShadow.gameObject
            );

        AddTrigger(
            trigger,
            EventTriggerType.PointerClick,
            eventData =>
            {
                eventData.Use();
                CloseFromOutsideClick();
            }
        );
    }

    /// <summary>
    /// Canvas의 RerollButton과 내부 Desc를 찾아 초기 리롤 횟수와 클릭 이벤트를 설정합니다.
    /// </summary>
    private void InitializeRerollButton()
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning(
                "[CardBackdropCarousel] Canvas가 없어 RerollButton을 초기화하지 못했습니다.",
                this
            );
            return;
        }

        if (rerollButton == null)
        {
            Transform found = FindChildRecursive(
                targetCanvas.transform,
                rerollButtonObjectName
            );

            if (found != null)
            {
                rerollButton = found.GetComponent<Button>();

                // Button 컴포넌트가 빠져 있어도 기존 Graphic을 사용해 자동으로 보완합니다.
                if (rerollButton == null)
                {
                    rerollButton = found.gameObject.AddComponent<Button>();
                    rerollButton.targetGraphic = found.GetComponent<Graphic>();
                }
            }
        }

        if (rerollButton == null)
        {
            Debug.LogWarning(
                $"[CardBackdropCarousel] '{rerollButtonObjectName}'을 찾지 못했습니다.",
                this
            );
            return;
        }

        if (rerollDesc == null)
        {
            Transform foundDesc = FindChildRecursive(
                rerollButton.transform,
                rerollDescObjectName
            );

            if (foundDesc != null)
                rerollDesc = foundDesc.GetComponent<TMP_Text>();
        }

        if (rerollDesc == null)
        {
            Debug.LogWarning(
                $"[CardBackdropCarousel] '{rerollButtonObjectName}/{rerollDescObjectName}' TMP_Text를 찾지 못했습니다.",
                rerollButton
            );
        }

        rerollButtonOriginalScale = rerollButton.transform.localScale;
        rerollsRemaining = Mathf.Max(0, maxRerolls);

        rerollButton.onClick.RemoveListener(RequestReroll);
        rerollButton.onClick.AddListener(RequestReroll);

        UpdateRerollUI();
    }

    /// <summary>
    /// 남은 횟수가 있고 카드가 확정되지 않았을 때 리롤을 시작합니다.
    /// 선택 또는 상세 상태는 즉시 초기화한 뒤 같은 카드 전환 효과를 재사용합니다.
    /// </summary>
    private void RequestReroll()
    {
        if (!CanReroll())
            return;

        rerollsRemaining--;

        if (rerollButtonCoroutine != null)
            StopCoroutine(rerollButtonCoroutine);

        rerollButtonCoroutine = StartCoroutine(PlayRerollButtonPress());
        StartCoroutine(PlayCardTransition(0, true));
    }

    /// <summary>
    /// 남은 리롤 횟수를 Desc에 표시하고 사용 가능 상태를 갱신합니다.
    /// </summary>
    private void UpdateRerollUI()
    {
        if (rerollDesc != null)
            rerollDesc.text = $"{rerollsRemaining}/{Mathf.Max(0, maxRerolls)}";

        if (rerollButton != null)
            rerollButton.interactable = CanReroll();
    }

    /// <summary>
    /// 리롤 가능 여부를 확인합니다.
    /// 카드 확정 이후 또는 횟수가 0일 때에는 버튼이 비활성화됩니다.
    /// </summary>
    private bool CanReroll()
    {
        return rerollsRemaining > 0 &&
               !isMoving &&
               !isConfirmAnimating &&
               !hasConfirmedCard;
    }

    /// <summary>
    /// 카드 루트에 호버, 호버 종료, 선택 클릭 이벤트를 등록합니다.
    /// </summary>
    private void RegisterCardEvents(
        RuntimeCard card
    )
    {
        EventTrigger trigger =
            GetOrCreateEventTrigger(
                card.Rect.gameObject
            );

        AddTrigger(
            trigger,
            EventTriggerType.PointerEnter,
            _ => StartCardHover(card)
        );

        AddTrigger(
            trigger,
            EventTriggerType.PointerExit,
            _ => EndCardHover(card)
        );

        AddTrigger(
            trigger,
            EventTriggerType.PointerClick,
            _ => SelectCard(card)
        );
    }

    /// <summary>
    /// Confirm 오브젝트에 호버 및 확정 클릭 이벤트를 등록합니다.
    /// </summary>
    private void RegisterConfirmEvents(
        RuntimeCard card
    )
    {
        if (card.ConfirmRect == null ||
            card.ConfirmGraphic == null)
        {
            Debug.LogWarning(
                $"[CardBackdropCarousel] '{card.Rect.name}/Confirm'을 찾지 못했습니다.",
                card.Rect
            );

            return;
        }

        card.ConfirmGraphic.raycastTarget = true;

        EventTrigger trigger =
            GetOrCreateEventTrigger(
                card.ConfirmRect.gameObject
            );

        AddTrigger(
            trigger,
            EventTriggerType.PointerEnter,
            _ => StartConfirmHover(card)
        );

        AddTrigger(
            trigger,
            EventTriggerType.PointerExit,
            _ => EndConfirmHover(card)
        );

        AddTrigger(
            trigger,
            EventTriggerType.PointerClick,
            eventData =>
            {
                eventData.Use();
                ConfirmCard(card);
            }
        );
    }

    /// <summary>
    /// OnUseText, PassiveText, SkillEffectIcon에 상세 패널 열기 이벤트를 등록합니다.
    /// </summary>
    private void RegisterDetailSelectorEvents(
        RuntimeCard card
    )
    {
        RegisterDetailSelector(
            card.OnUseRect,
            card.OnUseGraphic,
            card,
            DetailKind.Effect
        );

        RegisterDetailSelector(
            card.PassiveTextRect,
            card.PassiveTextGraphic,
            card,
            DetailKind.Passive
        );

        if (card.SkillEffectIcon != null)
        {
            RegisterDetailSelector(
                card.SkillEffectIcon.rectTransform,
                card.SkillEffectIcon,
                card,
                DetailKind.Effect
            );
        }
    }

    /// <summary>
    /// 지정한 UI 요소를 클릭하면 해당 종류의 상세 패널을 열도록 연결합니다.
    /// </summary>
    private void RegisterDetailSelector(
        RectTransform targetRect,
        Graphic targetGraphic,
        RuntimeCard card,
        DetailKind kind
    )
    {
        if (targetRect == null ||
            targetGraphic == null)
        {
            return;
        }

        targetGraphic.raycastTarget = true;

        EventTrigger trigger =
            GetOrCreateEventTrigger(
                targetRect.gameObject
            );

        AddTrigger(
            trigger,
            EventTriggerType.PointerClick,
            eventData =>
            {
                eventData.Use();
                RequestOpenDetail(card, kind);
            }
        );
    }

    /// <summary>
    /// Image에 간단한 클릭 콜백을 EventTrigger로 등록합니다.
    /// </summary>
    private void RegisterImageClick(
        Image image,
        UnityAction action
    )
    {
        if (image == null)
        {
            return;
        }

        image.raycastTarget = true;

        EventTrigger trigger =
            GetOrCreateEventTrigger(
                image.gameObject
            );

        AddTrigger(
            trigger,
            EventTriggerType.PointerClick,
            eventData =>
            {
                eventData.Use();
                action.Invoke();
            }
        );
    }

    /// <summary>
    /// 카드 배열을 왼쪽 방향으로 한 칸 전환합니다.
    /// </summary>
    private void MoveLeft()
    {
        if (CanMoveCards())
        {
            StartCoroutine(
                PlayCardTransition(1, false)
            );
        }
    }

    /// <summary>
    /// 카드 배열을 오른쪽 방향으로 한 칸 전환합니다.
    /// </summary>
    private void MoveRight()
    {
        if (CanMoveCards())
        {
            StartCoroutine(
                PlayCardTransition(-1, false)
            );
        }
    }

    /// <summary>
    /// 현재 중앙 카드에만 약한 호버 애니메이션을 시작합니다.
    /// </summary>
    private void StartCardHover(
        RuntimeCard card
    )
    {
        if (isMoving ||
            isSelecting ||
            isConfirmAnimating ||
            isDetailAnimating ||
            selectedCard != null ||
            card != GetFrontCard())
        {
            return;
        }

        StopCardHover();

        hoveredCard = card;

        cardHoverCoroutine =
            StartCoroutine(
                PlayCardHover(card)
            );
    }

    /// <summary>
    /// 호버가 끝난 카드를 원래 슬롯 상태로 복귀시킵니다.
    /// </summary>
    private void EndCardHover(
        RuntimeCard card
    )
    {
        if (card != hoveredCard ||
            selectedCard != null)
        {
            return;
        }

        StopCardHover();

        cardHoverCoroutine =
            StartCoroutine(
                ReturnCardFromHover(card)
            );
    }

    /// <summary>
    /// 중앙 카드를 선택 상태로 만들고 선택 위치로 이동시킵니다.
    /// </summary>
    private void SelectCard(
        RuntimeCard card
    )
    {
        if (card != GetFrontCard() ||
            selectedCard != null ||
            isMoving ||
            isSelecting ||
            isConfirmAnimating ||
            isDetailAnimating ||
            card.View.Definition == null)
        {
            return;
        }

        StopCardHover();
        RestoreCardAlphas();
        CloseAllDetailsInstantly();

        selectedCard = card;

        // Unity UI에서는 같은 부모 아래에서 마지막 자식이 가장 앞에 그려집니다.
        BringCardToTop(card);

        SetBackShadowInteractive(true);

        if (selectionCoroutine != null)
        {
            StopCoroutine(
                selectionCoroutine
            );
        }

        selectionCoroutine =
            StartCoroutine(
                PlayCardSelection(card)
            );
    }

    /// <summary>
    /// 선택 상태를 확인한 뒤 사용 효과 또는 패시브 상세 패널 열기를 요청합니다.
    /// </summary>
    private void RequestOpenDetail(
        RuntimeCard card,
        DetailKind kind
    )
    {
        if (card == null ||
            isMoving ||
            isConfirmAnimating ||
            isDetailAnimating)
        {
            return;
        }

        if (selectedCard == null)
        {
            if (card != GetFrontCard())
            {
                return;
            }

            StartCoroutine(
                SelectThenOpenDetail(
                    card,
                    kind
                )
            );

            return;
        }

        if (selectedCard != card ||
            isSelecting)
        {
            return;
        }

        StartDetailAnimation(
            card,
            kind
        );
    }

    /// <summary>
    /// 지정한 상세 패널의 열기 애니메이션을 시작합니다.
    /// </summary>
    private void StartDetailAnimation(
        RuntimeCard card,
        DetailKind kind
    )
    {
        DetailState state =
            kind == DetailKind.Effect
                ? card.EffectDetail
                : card.PassiveDetail;

        if (state == null ||
            state.Rect == null)
        {
            Debug.LogWarning(
                $"[CardBackdropCarousel] {kind} Detail을 찾지 못했습니다.",
                card.Rect
            );

            return;
        }

        if (detailCoroutine != null)
        {
            StopCoroutine(
                detailCoroutine
            );
        }

        detailCoroutine =
            StartCoroutine(
                PlayDetailOpen(
                    card,
                    state,
                    kind
                )
            );
    }

    /// <summary>
    /// 선택한 OnUseText 또는 PassiveText의 강조 애니메이션을 시작합니다.
    /// </summary>
    private void StartSelectorAnimation(
        RuntimeCard card,
        DetailKind kind
    )
    {
        if (selectorCoroutine != null)
        {
            StopCoroutine(
                selectorCoroutine
            );
        }

        ResetSelectorVisuals(card);

        selectorCoroutine =
            StartCoroutine(
                PlaySelectorAnimation(
                    card,
                    kind
                )
            );
    }

    /// <summary>
    /// 선택 카드의 Confirm 버튼 호버 애니메이션을 시작합니다.
    /// </summary>
    private void StartConfirmHover(
        RuntimeCard card
    )
    {
        if (selectedCard != card ||
            isSelecting ||
            isConfirmAnimating ||
            card.ConfirmRect == null ||
            card.ConfirmGraphic == null)
        {
            return;
        }

        StopConfirmHover();

        confirmHoverOwner =
            card;

        confirmHoverCoroutine =
            StartCoroutine(
                PlayConfirmHover(card)
            );
    }

    /// <summary>
    /// Confirm 버튼에서 포인터가 벗어나면 원래 상태로 복귀시킵니다.
    /// </summary>
    private void EndConfirmHover(
        RuntimeCard card
    )
    {
        if (confirmHoverOwner != card ||
            isConfirmAnimating)
        {
            return;
        }

        StopConfirmHover();

        confirmHoverCoroutine =
            StartCoroutine(
                ReturnConfirmFromHover(card)
            );
    }

    /// <summary>
    /// 현재 선택한 CardDefinition을 캐릭터에 전달하고 확정 이벤트와 애니메이션을 실행합니다.
    /// </summary>
    private void ConfirmCard(
        RuntimeCard card
    )
    {
        if (selectedCard != card ||
            isSelecting ||
            isConfirmAnimating ||
            isDetailAnimating ||
            hasConfirmedCard ||
            card.View.Definition == null)
        {
            return;
        }

        CardDefinition definition =
            card.View.Definition;

        // 선택한 카드 한 장을 씬 전환 뒤에도 유지하도록 세션에 저장합니다.
        CardSelectionSession.SetSelectedCard(definition);

        // 같은 씬에 실제 캐릭터가 있다면 즉시 적용하며, 다른 씬이라면 시작 스크립트가 적용합니다.
        if (targetCharacter != null)
            targetCharacter.AssignCard(definition);

        hasConfirmedCard = true;
        UpdateRerollUI();

        CardConfirmed?.Invoke(
            definition
        );

        StopConfirmHover();

        if (confirmClickCoroutine != null)
        {
            StopCoroutine(
                confirmClickCoroutine
            );
        }

        confirmClickCoroutine =
            StartCoroutine(
                PlayConfirmClick(card)
            );
    }

    /// <summary>
    /// 카드 바깥의 BackShadow를 클릭했을 때 현재 선택을 해제합니다.
    /// </summary>
    private void CloseFromOutsideClick()
    {
        if (selectedCard == null ||
            isMoving ||
            isSelecting ||
            isConfirmAnimating ||
            isDetailAnimating)
        {
            return;
        }

        DeselectSelectedCard();
    }

    /// <summary>
    /// 외부 코드에서도 호출할 수 있도록 현재 선택 카드의 복귀를 요청합니다.
    /// </summary>
    public void DeselectSelectedCard()
    {
        if (selectedCard == null ||
            isMoving ||
            isSelecting ||
            isConfirmAnimating ||
            isDetailAnimating)
        {
            return;
        }

        StartCoroutine(
            ReturnSelectedCard(
                selectedCard
            )
        );
    }

    /// <summary>
    /// 대상 오브젝트의 EventTrigger를 가져오며, 없으면 새로 추가합니다.
    /// </summary>
    private static EventTrigger GetOrCreateEventTrigger(
        GameObject target
    )
    {
        EventTrigger trigger =
            target.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger =
                target.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers =
                new List<EventTrigger.Entry>();
        }

        return trigger;
    }

    /// <summary>
    /// EventTrigger에 지정한 이벤트 종류와 콜백을 추가합니다.
    /// </summary>
    private static void AddTrigger(
        EventTrigger trigger,
        EventTriggerType eventType,
        UnityAction<BaseEventData> callback
    )
    {
        EventTrigger.Entry entry =
            new EventTrigger.Entry
            {
                eventID = eventType,
                callback =
                    new EventTrigger.TriggerEvent()
            };

        entry.callback.AddListener(
            callback
        );

        trigger.triggers.Add(
            entry
        );
    }
}
