using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CardBackdropCarousel에서 사용하는 모든 시각 애니메이션과 시각 상태 초기화를 모아 둔 파일입니다.
/// 이 파일은 별도의 컴포넌트가 아니므로 GameObject에 추가하지 않으셔도 됩니다.
/// Inspector에는 기존과 동일하게 CardBackdropCarousel 하나만 표시됩니다.
/// </summary>
public sealed partial class CardBackdropCarousel
{
    [SerializeField] private float rerollPressedScale = 0.9f;
    [SerializeField] private float rerollPressDuration = 0.08f;
    [SerializeField] private float rerollReleaseDuration = 0.16f;

    // 카드 전환은 아래로 이동, 즉시 슬롯 교체, 위로 복귀 순서로 실행됩니다.
    [Header("Card Transition")]
    [Tooltip("모든 카드가 아래로 내려가는 시간입니다.")]
    [SerializeField] private float transitionDownDuration = 0.18f;

    [Tooltip("슬롯 교체 후 카드가 다시 올라오는 시간입니다.")]
    [SerializeField] private float transitionUpDuration = 0.24f;

    [SerializeField] private float transitionDropDistance = 24f;

    [Header("Card Hover")]
    [SerializeField] private float cardHoverScale = 1.025f;
    [SerializeField] private float cardHoverAngle = 5f;
    [SerializeField] private float cardHoverEndAngle = 7.5f;
    [SerializeField] private float cardHoverReturnDuration = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float cardHoverOtherAlpha = 0.78f;

    // 카드 선택 시 이동 위치와 주변 UI 표시 값을 설정합니다.
    [Header("Card Selection")]
    [SerializeField] private Vector3 selectedAnchoredPosition =
        new Vector3(0f, -19f, -85f);

    [SerializeField] private float selectDuration = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float selectedOtherCardAlpha = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float selectedShadowAlpha = 125f / 255f;

    [Header("Confirm Object")]
    [SerializeField] private string confirmObjectName = "Confirm";

    [Header("Confirm Hover")]
    [SerializeField] private float confirmHoverScale = 1.025f;
    [SerializeField] private float confirmPulseDuration = 1.4f;

    [SerializeField, Range(0f, 1f)]
    private float confirmMinimumAlpha = 245f / 255f;

    [SerializeField] private float confirmReturnDuration = 0.25f;

    [Header("Confirm Press")]
    [SerializeField] private float confirmPressedScale = 0.94f;
    [SerializeField] private float confirmPressDuration = 0.08f;
    [SerializeField] private float confirmReleaseDuration = 0.14f;

    [Header("Confirmed Card Animation")]
    [SerializeField] private float confirmedRiseDistance = 30f;
    [SerializeField] private float confirmedDropDistance = 110f;
    [SerializeField] private float confirmedRiseDuration = 0.24f;
    [SerializeField] private float confirmedFallDuration = 0.52f;

    [SerializeField] private float confirmedSpinDegrees = 360f;
    [SerializeField] private float confirmedSpinXTilt = 18f;
    [SerializeField] private float confirmedSpinZTilt = 6f;

    [SerializeField, Range(0f, 1f)]
    private float confirmedEndAlpha = 0f;

    [Header("Detail Object Names")]
    [SerializeField] private string onUseTextObjectName = "OnUseText";
    [SerializeField] private string skillEffectIconObjectName = "SkillEffectIcon";
    [SerializeField] private string passiveTextObjectName = "PassiveText";
    [SerializeField] private string effectDetailObjectName = "EffectDetail";
    [SerializeField] private string passiveDetailObjectName = "PassiveDetail";

    [Header("Detail Selector Animation")]
    [SerializeField, Range(0f, 1f)]
    private float selectorIdleAlpha = 165f / 255f;

    [SerializeField] private float selectorActiveScale = 1.05f;
    [SerializeField] private float selectorAnimationDuration = 0.22f;

    [Header("Detail Card Movement")]
    [SerializeField] private Vector3 detailCardPosition =
        new Vector3(-255f, -20f, -85f);

    [SerializeField, Range(0f, 1f)]
    private float detailShadowAlpha = 210f / 255f;

    [Header("Detail Rotation")]
    [SerializeField] private Vector3 detailStartRotation =
        new Vector3(0f, 90f, 0f);

    [SerializeField] private Vector3 detailOvershootRotation =
        new Vector3(0f, -31f, 0f);

    [SerializeField] private Vector3 detailEndRotation =
        Vector3.zero;

    [SerializeField] private float detailOvershootDuration = 0.22f;
    [SerializeField] private float detailSettleDuration = 0.18f;

    // 상세 패널은 자동 스크롤과 사용자 휠/드래그 스크롤을 함께 지원합니다.
    [Header("Detail Auto Scroll")]
    [Tooltip("상세 설명이 자동으로 아래쪽으로 이동하는 속도입니다.")]
    [SerializeField] private float automaticScrollSpeed = 0.015f;

    [Tooltip("마우스 휠 또는 드래그 후 자동 스크롤을 다시 시작하기까지의 시간입니다.")]
    [SerializeField] private float automaticScrollResumeDelay = 1.5f;

    private RuntimeCard[] orderedCards;
    private SlotPose[] slotPoses;

    private Coroutine cardHoverCoroutine;
    private Coroutine confirmHoverCoroutine;
    private Coroutine confirmClickCoroutine;
    private Coroutine selectionCoroutine;
    private Coroutine detailCoroutine;
    private Coroutine selectorCoroutine;
    private Coroutine rerollButtonCoroutine;

    private RuntimeCard hoveredCard;
    private RuntimeCard selectedCard;
    private RuntimeCard confirmHoverOwner;

    private DetailState activeDetailState;

    private bool isMoving;
    private bool isSelecting;
    private bool isConfirmAnimating;
    private bool isDetailAnimating;
    private bool hasConfirmedCard;

    private int rerollsRemaining;
    private int dealSequence;
    private Vector3 rerollButtonOriginalScale = Vector3.one;
    private float automaticScrollResumeTime;

    /// <summary>
    /// 한 카드 슬롯의 UI 참조와 실행 중 상태를 묶어 보관합니다.
    /// Inspector 데이터와 애니메이션 상태를 반복해서 찾지 않도록 캐시합니다.
    /// </summary>
    private sealed class RuntimeCard
    {
        public CardView View;
        public RectTransform Rect;
        public CanvasGroup CanvasGroup;
        public float OriginalAlpha;

        public RectTransform ConfirmRect;
        public Graphic ConfirmGraphic;
        public Vector3 ConfirmOriginalScale;
        public float ConfirmOriginalAlpha;

        public RectTransform OnUseRect;
        public Graphic OnUseGraphic;
        public Vector3 OnUseOriginalScale;

        public Graphic SkillEffectIcon;

        public RectTransform PassiveTextRect;
        public Graphic PassiveTextGraphic;
        public Vector3 PassiveTextOriginalScale;

        public DetailState EffectDetail;
        public DetailState PassiveDetail;
    }

    /// <summary>
    /// EffectDetail 또는 PassiveDetail의 RectTransform과 ScrollRect를 보관합니다.
    /// </summary>
    private sealed class DetailState
    {
        public RectTransform Rect;
        public ScrollRect ScrollRect;
    }

    /// <summary>
    /// 왼쪽·중앙·오른쪽 슬롯의 위치, 회전, 크기 정보를 보관합니다.
    /// 카드 전환 시 이 값을 즉시 적용하여 슬롯 모양을 정확히 맞춥니다.
    /// </summary>
    private struct SlotPose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Vector2 SizeDelta;

        /// <summary>
        /// 현재 RectTransform의 슬롯 상태를 저장합니다.
        /// </summary>
        public SlotPose(
            RectTransform rect
        )
        {
            Position = rect.anchoredPosition3D;
            Rotation = rect.localRotation;
            Scale = rect.localScale;
            SizeDelta = rect.sizeDelta;
        }

        /// <summary>
        /// 저장된 슬롯 상태를 지정한 RectTransform에 적용합니다.
        /// </summary>
        public void Apply(
            RectTransform rect
        )
        {
            rect.anchoredPosition3D = Position;
            rect.localRotation = Rotation;
            rect.localScale = Scale;
            rect.sizeDelta = SizeDelta;
        }
    }

    /// <summary>
    /// RerollButton을 잠시 축소한 뒤 원래 크기로 되돌립니다.
    /// </summary>
    private IEnumerator PlayRerollButtonPress()
    {
        if (rerollButton == null)
            yield break;

        Transform buttonTransform = rerollButton.transform;
        Vector3 startScale = buttonTransform.localScale;
        Vector3 pressedScale = rerollButtonOriginalScale * rerollPressedScale;
        float elapsed = 0f;

        while (elapsed < rerollPressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(
                elapsed / Mathf.Max(rerollPressDuration, 0.001f)
            );

            buttonTransform.localScale = Vector3.Lerp(
                startScale,
                pressedScale,
                EaseOutCubic(progress)
            );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < rerollReleaseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(
                elapsed / Mathf.Max(rerollReleaseDuration, 0.001f)
            );

            buttonTransform.localScale = Vector3.Lerp(
                pressedScale,
                rerollButtonOriginalScale,
                EaseOutCubic(progress)
            );

            yield return null;
        }

        buttonTransform.localScale = rerollButtonOriginalScale;
        rerollButtonCoroutine = null;
    }

    /// <summary>
    /// 모든 카드를 아래로 내린 뒤 슬롯 또는 카드 데이터를 교체하고 다시 올립니다.
    /// 방향이 0이면 슬롯은 유지하고 카드 데이터만 다시 배정하므로 리롤에서도 같은 효과를 사용합니다.
    /// </summary>
    private IEnumerator PlayCardTransition(int direction, bool redealCards)
    {
        isMoving = true;
        UpdateRerollUI();
        ResetCarouselForTransition();

        int count = orderedCards.Length;
        Vector3[] startPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
            startPositions[i] = orderedCards[i].Rect.anchoredPosition3D;

        // 1단계: 투명도 변화 없이 모든 카드를 아래로 내립니다.
        float elapsed = 0f;

        while (elapsed < transitionDownDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(
                elapsed / Mathf.Max(transitionDownDuration, 0.001f)
            );
            float easedProgress = EaseInCubic(progress);

            for (int i = 0; i < count; i++)
            {
                orderedCards[i].Rect.anchoredPosition3D = Vector3.Lerp(
                    startPositions[i],
                    startPositions[i] + Vector3.down * transitionDropDistance,
                    easedProgress
                );
            }

            yield return null;
        }

        // 2단계: 슬롯 이동은 즉시 적용하며, 리롤이면 이 시점에 새 카드 데이터를 넣습니다.
        for (int i = 0; i < count; i++)
        {
            int targetSlot = direction == 0
                ? i
                : WrapIndex(i + direction, count);

            SlotPose pose = slotPoses[targetSlot];
            RuntimeCard card = orderedCards[i];

            card.Rect.anchoredPosition3D =
                pose.Position + Vector3.down * transitionDropDistance;
            card.Rect.localRotation = pose.Rotation;
            card.Rect.localScale = pose.Scale;
            card.Rect.sizeDelta = pose.SizeDelta;
        }

        if (direction != 0)
            ReorderCards(direction);

        if (redealCards)
            DealRandomCards(true);

        // 3단계: 교체된 슬롯에서 다시 위로 올립니다.
        elapsed = 0f;

        while (elapsed < transitionUpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(
                elapsed / Mathf.Max(transitionUpDuration, 0.001f)
            );
            float easedProgress = EaseOutCubic(progress);

            for (int slot = 0; slot < count; slot++)
            {
                SlotPose pose = slotPoses[slot];

                orderedCards[slot].Rect.anchoredPosition3D = Vector3.Lerp(
                    pose.Position + Vector3.down * transitionDropDistance,
                    pose.Position,
                    easedProgress
                );
            }

            yield return null;
        }

        for (int slot = 0; slot < count; slot++)
            slotPoses[slot].Apply(orderedCards[slot].Rect);

        isMoving = false;
        UpdateRerollUI();
    }

    /// <summary>
    /// 카드 전환 또는 리롤 전에 선택·상세·호버 상태를 슬롯 기본 상태로 초기화합니다.
    /// </summary>
    private void ResetCarouselForTransition()
    {
        StopCardHover();
        StopConfirmHover();

        if (selectionCoroutine != null)
        {
            StopCoroutine(selectionCoroutine);
            selectionCoroutine = null;
        }

        CloseAllDetailsInstantly();

        selectedCard = null;
        isSelecting = false;

        ResetHoverVisualsInstantly();
        RestoreCardAlphas();
        SetBackShadowInteractive(false);
    }

    /// <summary>
    /// 중앙 카드를 약하게 흔들고 확대하며 다른 카드의 투명도를 낮춥니다.
    /// </summary>
    private IEnumerator PlayCardHover(
        RuntimeCard card
    )
    {
        SlotPose basePose =
            GetNearestSlotPose(
                card.Rect
            );

        float[] angles =
        {
            -cardHoverAngle,
            cardHoverAngle,
            -cardHoverEndAngle
        };

        float[] durations =
        {
            0.1f,
            0.13f,
            0.2f
        };

        float totalDuration =
            durations[0] +
            durations[1] +
            durations[2];

        float passedDuration = 0f;

        float startAngle =
            GetRelativeAngle(
                card.Rect,
                basePose.Rotation
            );

        float[] startAlphas =
            CaptureCardAlphas();

        for (int step = 0;
             step < angles.Length;
             step++)
        {
            float elapsed = 0f;
            float duration = durations[step];
            float targetAngle = angles[step];

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed /
                        Mathf.Max(
                            duration,
                            0.001f
                        )
                    );

                float rotationProgress =
                    step == angles.Length - 1
                        ? EaseOutCubic(progress)
                        : SmoothStep(progress);

                float angle =
                    Mathf.Lerp(
                        startAngle,
                        targetAngle,
                        rotationProgress
                    );

                card.Rect.localRotation =
                    basePose.Rotation *
                    Quaternion.Euler(
                        0f,
                        0f,
                        angle
                    );

                float totalProgress =
                    Mathf.Clamp01(
                        (passedDuration + elapsed) /
                        totalDuration
                    );

                card.Rect.localScale =
                    Vector3.Lerp(
                        basePose.Scale,
                        basePose.Scale *
                        cardHoverScale,
                        SmoothStep(totalProgress)
                    );

                for (int i = 0;
                     i < orderedCards.Length;
                     i++)
                {
                    RuntimeCard other =
                        orderedCards[i];

                    if (other == card)
                    {
                        continue;
                    }

                    other.CanvasGroup.alpha =
                        Mathf.Lerp(
                            startAlphas[i],
                            cardHoverOtherAlpha,
                            SmoothStep(totalProgress)
                        );
                }

                yield return null;
            }

            passedDuration += duration;
            startAngle = targetAngle;
        }

        card.Rect.localRotation =
            basePose.Rotation *
            Quaternion.Euler(
                0f,
                0f,
                -cardHoverEndAngle
            );

        card.Rect.localScale =
            basePose.Scale *
            cardHoverScale;

        SetOtherCardAlpha(
            card,
            cardHoverOtherAlpha
        );

        cardHoverCoroutine = null;
    }

    /// <summary>
    /// 호버 중인 카드의 회전·크기·주변 카드 투명도를 원래대로 되돌립니다.
    /// </summary>
    private IEnumerator ReturnCardFromHover(
        RuntimeCard card
    )
    {
        SlotPose pose =
            GetNearestSlotPose(
                card.Rect
            );

        Quaternion startRotation =
            card.Rect.localRotation;

        Vector3 startScale =
            card.Rect.localScale;

        float[] startAlphas =
            CaptureCardAlphas();

        float elapsed = 0f;

        while (elapsed <
               cardHoverReturnDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        cardHoverReturnDuration,
                        0.001f
                    )
                );

            float easedProgress =
                EaseOutCubic(progress);

            card.Rect.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    pose.Rotation,
                    easedProgress
                );

            card.Rect.localScale =
                Vector3.Lerp(
                    startScale,
                    pose.Scale,
                    easedProgress
                );

            for (int i = 0;
                 i < orderedCards.Length;
                 i++)
            {
                orderedCards[i]
                    .CanvasGroup
                    .alpha =
                    Mathf.Lerp(
                        startAlphas[i],
                        orderedCards[i]
                            .OriginalAlpha,
                        easedProgress
                    );
            }

            yield return null;
        }

        card.Rect.localRotation =
            pose.Rotation;

        card.Rect.localScale =
            pose.Scale;

        RestoreCardAlphas();

        hoveredCard = null;
        cardHoverCoroutine = null;
    }

    /// <summary>
    /// 선택 카드를 지정 위치로 이동시키고 BackShadow와 주변 카드 표시를 조절합니다.
    /// </summary>
    private IEnumerator PlayCardSelection(
        RuntimeCard card
    )
    {
        isSelecting = true;

        Vector3 startPosition =
            card.Rect.anchoredPosition3D;

        Quaternion startRotation =
            card.Rect.localRotation;

        Vector3 startScale =
            card.Rect.localScale;

        SlotPose frontPose =
            slotPoses[1];

        float shadowStartAlpha =
            backShadow != null
                ? backShadow.color.a
                : 0f;

        float[] startAlphas =
            CaptureCardAlphas();

        float elapsed = 0f;

        while (elapsed <
               selectDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        selectDuration,
                        0.001f
                    )
                );

            float easedProgress =
                EaseOutCubic(progress);

            card.Rect.anchoredPosition3D =
                Vector3.Lerp(
                    startPosition,
                    selectedAnchoredPosition,
                    easedProgress
                );

            card.Rect.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    frontPose.Rotation,
                    easedProgress
                );

            card.Rect.localScale =
                Vector3.Lerp(
                    startScale,
                    frontPose.Scale,
                    easedProgress
                );

            if (backShadow != null)
            {
                SetGraphicAlpha(
                    backShadow,
                    Mathf.Lerp(
                        shadowStartAlpha,
                        selectedShadowAlpha,
                        easedProgress
                    )
                );
            }

            for (int i = 0;
                 i < orderedCards.Length;
                 i++)
            {
                RuntimeCard other =
                    orderedCards[i];

                if (other == card)
                {
                    continue;
                }

                other.CanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlphas[i],
                        selectedOtherCardAlpha,
                        easedProgress
                    );
            }

            yield return null;
        }

        card.Rect.anchoredPosition3D =
            selectedAnchoredPosition;

        card.Rect.localRotation =
            frontPose.Rotation;

        card.Rect.localScale =
            frontPose.Scale;

        card.Rect.sizeDelta =
            frontPose.SizeDelta;

        if (backShadow != null)
        {
            SetGraphicAlpha(
                backShadow,
                selectedShadowAlpha
            );
        }

        SetOtherCardAlpha(
            card,
            selectedOtherCardAlpha
        );

        isSelecting = false;
        selectionCoroutine = null;
    }

    /// <summary>
    /// 카드가 아직 선택되지 않았다면 먼저 선택한 뒤 상세 패널을 엽니다.
    /// </summary>
    private IEnumerator SelectThenOpenDetail(
        RuntimeCard card,
        DetailKind kind
    )
    {
        SelectCard(card);

        while (isSelecting)
        {
            yield return null;
        }

        if (selectedCard == card)
        {
            StartDetailAnimation(
                card,
                kind
            );
        }
    }

    /// <summary>
    /// 카드를 상세 위치로 이동하고 패널을 90도에서 -31도를 거쳐 0도로 회전시킵니다.
    /// </summary>
    private IEnumerator PlayDetailOpen(
        RuntimeCard card,
        DetailState state,
        DetailKind kind
    )
    {
        isDetailAnimating = true;

        DisableOtherDetails(
            card,
            state
        );

        activeDetailState =
            state;

        state.Rect.gameObject.SetActive(true);

        state.Rect.localRotation =
            Quaternion.Euler(
                detailStartRotation
            );

        // 비활성 상태에서 계산된 폭과 높이는 부정확할 수 있으므로
        // 상세 패널을 활성화한 뒤 레이아웃을 다시 계산합니다.
        card.View.RefreshDetailLayout(
            kind == DetailKind.Passive
        );

        yield return null;

        card.View.RefreshDetailLayout(
            kind == DetailKind.Passive
        );

        if (state.ScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            state.ScrollRect.StopMovement();
            state.ScrollRect.verticalNormalizedPosition = 1f;
        }

        automaticScrollResumeTime =
            Time.unscaledTime + automaticScrollResumeDelay;

        StartSelectorAnimation(
            card,
            kind
        );

        SetBackShadowInteractive(true);

        Vector3 cardStartPosition =
            card.Rect.anchoredPosition3D;

        float shadowStartAlpha =
            backShadow != null
                ? backShadow.color.a
                : 0f;

        Quaternion startRotation =
            Quaternion.Euler(
                detailStartRotation
            );

        Quaternion overshootRotation =
            Quaternion.Euler(
                detailOvershootRotation
            );

        Quaternion endRotation =
            Quaternion.Euler(
                detailEndRotation
            );

        float elapsed = 0f;

        while (elapsed <
               detailOvershootDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        detailOvershootDuration,
                        0.001f
                    )
                );

            float easedProgress =
                EaseOutCubic(progress);

            card.Rect.anchoredPosition3D =
                Vector3.Lerp(
                    cardStartPosition,
                    detailCardPosition,
                    easedProgress
                );

            state.Rect.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    overshootRotation,
                    easedProgress
                );

            if (backShadow != null)
            {
                SetGraphicAlpha(
                    backShadow,
                    Mathf.Lerp(
                        shadowStartAlpha,
                        detailShadowAlpha,
                        easedProgress
                    )
                );
            }

            yield return null;
        }

        elapsed = 0f;

        while (elapsed <
               detailSettleDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        detailSettleDuration,
                        0.001f
                    )
                );

            state.Rect.localRotation =
                Quaternion.Slerp(
                    overshootRotation,
                    endRotation,
                    EaseOutCubic(progress)
                );

            yield return null;
        }

        card.Rect.anchoredPosition3D =
            detailCardPosition;

        state.Rect.localRotation =
            endRotation;

        if (backShadow != null)
        {
            SetGraphicAlpha(
                backShadow,
                detailShadowAlpha
            );
        }

        isDetailAnimating = false;
        detailCoroutine = null;
    }

    /// <summary>
    /// 선택된 텍스트와 관련 아이콘의 알파를 165에서 255로 올리고 크기를 확대합니다.
    /// </summary>
    private IEnumerator PlaySelectorAnimation(
        RuntimeCard card,
        DetailKind kind
    )
    {
        RectTransform targetRect;
        Graphic targetGraphic;
        Graphic targetIcon;
        Vector3 originalScale;

        if (kind == DetailKind.Effect)
        {
            targetRect =
                card.OnUseRect;

            targetGraphic =
                card.OnUseGraphic;

            targetIcon =
                card.SkillEffectIcon;

            originalScale =
                card.OnUseOriginalScale;
        }
        else
        {
            targetRect =
                card.PassiveTextRect;

            targetGraphic =
                card.PassiveTextGraphic;

            targetIcon = null;

            originalScale =
                card.PassiveTextOriginalScale;
        }

        if (targetRect == null ||
            targetGraphic == null)
        {
            yield break;
        }

        SetGraphicAlpha(
            targetGraphic,
            selectorIdleAlpha
        );

        if (targetIcon != null)
        {
            SetGraphicAlpha(
                targetIcon,
                selectorIdleAlpha
            );
        }

        targetRect.localScale =
            originalScale;

        float elapsed = 0f;

        while (elapsed <
               selectorAnimationDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        selectorAnimationDuration,
                        0.001f
                    )
                );

            float easedProgress =
                EaseOutCubic(progress);

            SetGraphicAlpha(
                targetGraphic,
                Mathf.Lerp(
                    selectorIdleAlpha,
                    1f,
                    easedProgress
                )
            );

            if (targetIcon != null)
            {
                SetGraphicAlpha(
                    targetIcon,
                    Mathf.Lerp(
                        selectorIdleAlpha,
                        1f,
                        easedProgress
                    )
                );
            }

            targetRect.localScale =
                Vector3.Lerp(
                    originalScale,
                    originalScale *
                    selectorActiveScale,
                    easedProgress
                );

            yield return null;
        }

        SetGraphicAlpha(
            targetGraphic,
            1f
        );

        if (targetIcon != null)
        {
            SetGraphicAlpha(
                targetIcon,
                1f
            );
        }

        targetRect.localScale =
            originalScale *
            selectorActiveScale;

        selectorCoroutine = null;
    }

    /// <summary>
    /// 상세 설명을 자동으로 아래로 이동합니다.
    /// 사용자가 마우스 휠 또는 드래그를 사용하면 잠시 자동 이동을 멈춥니다.
    /// </summary>
    private void UpdateActiveDetailAutoScroll()
    {
        if (activeDetailState == null ||
            activeDetailState.Rect == null ||
            activeDetailState.ScrollRect == null ||
            !activeDetailState.Rect.gameObject.activeInHierarchy ||
            isDetailAnimating)
        {
            return;
        }

        ScrollRect scroll = activeDetailState.ScrollRect;

        if (scroll.content == null ||
            scroll.viewport == null)
        {
            return;
        }

        bool pointerInside = RectTransformUtility.RectangleContainsScreenPoint(
            scroll.viewport,
            Input.mousePosition,
            targetCanvas != null &&
            targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null
        );

        bool userScrolled =
            pointerInside &&
            (
                Mathf.Abs(Input.mouseScrollDelta.y) > 0.001f ||
                Input.GetMouseButton(0)
            );

        if (userScrolled)
        {
            automaticScrollResumeTime =
                Time.unscaledTime + automaticScrollResumeDelay;
            return;
        }

        if (Time.unscaledTime < automaticScrollResumeTime)
            return;

        if (scroll.content.rect.height <=
            scroll.viewport.rect.height + 0.5f)
        {
            scroll.verticalNormalizedPosition = 1f;
            return;
        }

        scroll.verticalNormalizedPosition = Mathf.MoveTowards(
            scroll.verticalNormalizedPosition,
            0f,
            automaticScrollSpeed * Time.unscaledDeltaTime
        );
    }

    /// <summary>
    /// Confirm 버튼의 크기와 알파를 천천히 반복 변화시킵니다.
    /// </summary>
    private IEnumerator PlayConfirmHover(
        RuntimeCard card
    )
    {
        float elapsed = 0f;

        while (selectedCard == card &&
               !isConfirmAnimating)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float phase =
                Mathf.PingPong(
                    elapsed /
                    Mathf.Max(
                        confirmPulseDuration,
                        0.001f
                    ),
                    1f
                );

            float easedPhase =
                SmoothStep(phase);

            SetGraphicAlpha(
                card.ConfirmGraphic,
                Mathf.Lerp(
                    card.ConfirmOriginalAlpha,
                    confirmMinimumAlpha,
                    easedPhase
                )
            );

            card.ConfirmRect.localScale =
                Vector3.Lerp(
                    card.ConfirmOriginalScale,
                    card.ConfirmOriginalScale *
                    confirmHoverScale,
                    easedPhase
                );

            yield return null;
        }

        confirmHoverCoroutine = null;
    }

    /// <summary>
    /// Confirm 버튼의 크기와 알파를 원래 값으로 부드럽게 되돌립니다.
    /// </summary>
    private IEnumerator ReturnConfirmFromHover(
        RuntimeCard card
    )
    {
        Vector3 startScale =
            card.ConfirmRect.localScale;

        float startAlpha =
            card.ConfirmGraphic.color.a;

        float elapsed = 0f;

        while (elapsed <
               confirmReturnDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        confirmReturnDuration,
                        0.001f
                    )
                );

            float easedProgress =
                EaseOutCubic(progress);

            card.ConfirmRect.localScale =
                Vector3.Lerp(
                    startScale,
                    card.ConfirmOriginalScale,
                    easedProgress
                );

            SetGraphicAlpha(
                card.ConfirmGraphic,
                Mathf.Lerp(
                    startAlpha,
                    card.ConfirmOriginalAlpha,
                    easedProgress
                )
            );

            yield return null;
        }

        ResetConfirmVisual(card);

        confirmHoverOwner = null;
        confirmHoverCoroutine = null;
    }

    /// <summary>
    /// Confirm 버튼 눌림, 카드 상승·360도 회전·낙하·투명화 애니메이션을 실행합니다.
    /// </summary>
    private IEnumerator PlayConfirmClick(
        RuntimeCard card
    )
    {
        isConfirmAnimating = true;

        RectTransform confirm =
            card.ConfirmRect;

        Vector3 confirmStartScale =
            confirm.localScale;

        Vector3 pressedScale =
            card.ConfirmOriginalScale *
            confirmPressedScale;

        float elapsed = 0f;

        while (elapsed <
               confirmPressDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        confirmPressDuration,
                        0.001f
                    )
                );

            confirm.localScale =
                Vector3.Lerp(
                    confirmStartScale,
                    pressedScale,
                    EaseOutCubic(progress)
                );

            yield return null;
        }

        Vector3 startPosition =
            card.Rect.anchoredPosition3D;

        Vector3 topPosition =
            startPosition +
            Vector3.up *
            confirmedRiseDistance;

        Vector3 bottomPosition =
            startPosition +
            Vector3.down *
            confirmedDropDistance;

        Quaternion startRotation =
            card.Rect.localRotation;

        float startAlpha =
            card.CanvasGroup.alpha;

        float shadowStartAlpha =
            backShadow != null
                ? backShadow.color.a
                : 0f;

        elapsed = 0f;

        while (elapsed <
               confirmedRiseDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        confirmedRiseDuration,
                        0.001f
                    )
                );

            card.Rect.anchoredPosition3D =
                Vector3.Lerp(
                    startPosition,
                    topPosition,
                    EaseOutCubic(progress)
                );

            ApplyConfirmedSpin(
                card.Rect,
                startRotation,
                progress * 0.35f
            );

            card.CanvasGroup.alpha =
                startAlpha;

            float releaseProgress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        confirmReleaseDuration,
                        0.001f
                    )
                );

            confirm.localScale =
                Vector3.Lerp(
                    pressedScale,
                    card.ConfirmOriginalScale,
                    EaseOutCubic(releaseProgress)
                );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed <
               confirmedFallDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        confirmedFallDuration,
                        0.001f
                    )
                );

            card.Rect.anchoredPosition3D =
                Vector3.Lerp(
                    topPosition,
                    bottomPosition,
                    EaseInCubic(progress)
                );

            ApplyConfirmedSpin(
                card.Rect,
                startRotation,
                Mathf.Lerp(
                    0.35f,
                    1f,
                    progress
                )
            );

            card.CanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    confirmedEndAlpha,
                    SmoothStep(progress)
                );

            if (backShadow != null)
            {
                SetGraphicAlpha(
                    backShadow,
                    Mathf.Lerp(
                        shadowStartAlpha,
                        0f,
                        SmoothStep(progress)
                    )
                );
            }

            yield return null;
        }

        card.Rect.anchoredPosition3D =
            bottomPosition;

        card.Rect.localRotation =
            startRotation;

        card.CanvasGroup.alpha =
            confirmedEndAlpha;

        card.CanvasGroup.interactable =
            false;

        card.CanvasGroup.blocksRaycasts =
            false;

        confirm.localScale =
            card.ConfirmOriginalScale;

        SetGraphicAlpha(
            card.ConfirmGraphic,
            card.ConfirmOriginalAlpha
        );

        SetBackShadowInteractive(false);

        confirmHoverOwner = null;
        confirmClickCoroutine = null;
        isConfirmAnimating = false;
    }

    /// <summary>
    /// 확정 애니메이션 진행률에 따라 카드의 3D 회전을 계산하여 적용합니다.
    /// </summary>
    private void ApplyConfirmedSpin(
        RectTransform rect,
        Quaternion baseRotation,
        float progress
    )
    {
        progress =
            Mathf.Clamp01(progress);

        float yRotation =
            confirmedSpinDegrees *
            progress;

        float xRotation =
            Mathf.Sin(
                progress *
                Mathf.PI
            ) *
            confirmedSpinXTilt;

        float zRotation =
            Mathf.Sin(
                progress *
                Mathf.PI *
                2f
            ) *
            confirmedSpinZTilt;

        rect.localRotation =
            baseRotation *
            Quaternion.Euler(
                xRotation,
                yRotation,
                zRotation
            );
    }

    /// <summary>
    /// 선택 또는 상세 위치의 카드를 중앙 슬롯으로 되돌리고 관련 UI 상태를 복구합니다.
    /// </summary>
    private IEnumerator ReturnSelectedCard(
        RuntimeCard card
    )
    {
        isSelecting = true;

        StopConfirmHover();
        CloseAllDetailsInstantly();
        ResetConfirmVisual(card);

        Vector3 startPosition =
            card.Rect.anchoredPosition3D;

        Quaternion startRotation =
            card.Rect.localRotation;

        Vector3 startScale =
            card.Rect.localScale;

        float startAlpha =
            card.CanvasGroup.alpha;

        float shadowStartAlpha =
            backShadow != null
                ? backShadow.color.a
                : 0f;

        float[] startAlphas =
            CaptureCardAlphas();

        SlotPose frontPose =
            slotPoses[1];

        card.CanvasGroup.interactable =
            false;

        card.CanvasGroup.blocksRaycasts =
            false;

        float elapsed = 0f;

        while (elapsed <
               selectDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        selectDuration,
                        0.001f
                    )
                );

            float easedProgress =
                EaseOutCubic(progress);

            card.Rect.anchoredPosition3D =
                Vector3.Lerp(
                    startPosition,
                    frontPose.Position,
                    easedProgress
                );

            card.Rect.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    frontPose.Rotation,
                    easedProgress
                );

            card.Rect.localScale =
                Vector3.Lerp(
                    startScale,
                    frontPose.Scale,
                    easedProgress
                );

            card.CanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    card.OriginalAlpha,
                    easedProgress
                );

            if (backShadow != null)
            {
                SetGraphicAlpha(
                    backShadow,
                    Mathf.Lerp(
                        shadowStartAlpha,
                        0f,
                        easedProgress
                    )
                );
            }

            for (int i = 0;
                 i < orderedCards.Length;
                 i++)
            {
                RuntimeCard other =
                    orderedCards[i];

                if (other == card)
                {
                    continue;
                }

                other.CanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlphas[i],
                        other.OriginalAlpha,
                        easedProgress
                    );
            }

            yield return null;
        }

        frontPose.Apply(
            card.Rect
        );

        card.CanvasGroup.alpha =
            card.OriginalAlpha;

        card.CanvasGroup.interactable =
            true;

        card.CanvasGroup.blocksRaycasts =
            true;

        if (backShadow != null)
        {
            SetGraphicAlpha(
                backShadow,
                0f
            );
        }

        SetBackShadowInteractive(false);
        RestoreCardAlphas();

        selectedCard = null;
        isSelecting = false;
    }

    /// <summary>
    /// 현재 열어야 할 상세 패널을 제외한 모든 상세 패널을 비활성화합니다.
    /// </summary>
    private void DisableOtherDetails(
        RuntimeCard owner,
        DetailState except
    )
    {
        for (int i = 0;
             i < orderedCards.Length;
             i++)
        {
            RuntimeCard card =
                orderedCards[i];

            SetDetailActive(
                card.EffectDetail,
                card == owner &&
                card.EffectDetail == except
            );

            SetDetailActive(
                card.PassiveDetail,
                card == owner &&
                card.PassiveDetail == except
            );
        }
    }

    /// <summary>
    /// 상세 패널 활성 상태를 변경하고 남아 있는 ScrollRect 속도를 제거합니다.
    /// </summary>
    private static void SetDetailActive(
        DetailState state,
        bool active
    )
    {
        if (state == null ||
            state.Rect == null)
        {
            return;
        }

        if (state.ScrollRect != null)
        {
            state.ScrollRect.velocity =
                Vector2.zero;
        }

        state.Rect.gameObject.SetActive(
            active
        );
    }

    /// <summary>
    /// 진행 중인 상세 애니메이션을 중지하고 모든 상세 패널을 즉시 닫습니다.
    /// </summary>
    private void CloseAllDetailsInstantly()
    {
        if (detailCoroutine != null)
        {
            StopCoroutine(
                detailCoroutine
            );

            detailCoroutine = null;
        }

        if (selectorCoroutine != null)
        {
            StopCoroutine(
                selectorCoroutine
            );

            selectorCoroutine = null;
        }

        for (int i = 0;
             i < orderedCards.Length;
             i++)
        {
            RuntimeCard card =
                orderedCards[i];

            SetDetailActive(
                card.EffectDetail,
                false
            );

            SetDetailActive(
                card.PassiveDetail,
                false
            );

            ResetSelectorVisuals(card);
        }

        activeDetailState = null;
        isDetailAnimating = false;
    }

    /// <summary>
    /// OnUseText, PassiveText, 아이콘의 알파와 크기를 기본값으로 복구합니다.
    /// </summary>
    private void ResetSelectorVisuals(
        RuntimeCard card
    )
    {
        if (card.OnUseGraphic != null)
        {
            SetGraphicAlpha(
                card.OnUseGraphic,
                selectorIdleAlpha
            );
        }

        if (card.SkillEffectIcon != null)
        {
            SetGraphicAlpha(
                card.SkillEffectIcon,
                selectorIdleAlpha
            );
        }

        if (card.OnUseRect != null)
        {
            card.OnUseRect.localScale =
                card.OnUseOriginalScale;
        }

        if (card.PassiveTextGraphic != null)
        {
            SetGraphicAlpha(
                card.PassiveTextGraphic,
                selectorIdleAlpha
            );
        }

        if (card.PassiveTextRect != null)
        {
            card.PassiveTextRect.localScale =
                card.PassiveTextOriginalScale;
        }
    }

    /// <summary>
    /// 실행 중인 카드 호버 코루틴을 안전하게 중지합니다.
    /// </summary>
    private void StopCardHover()
    {
        if (cardHoverCoroutine != null)
        {
            StopCoroutine(
                cardHoverCoroutine
            );

            cardHoverCoroutine = null;
        }

        hoveredCard = null;
    }

    /// <summary>
    /// 실행 중인 Confirm 호버 코루틴을 안전하게 중지합니다.
    /// </summary>
    private void StopConfirmHover()
    {
        if (confirmHoverCoroutine != null)
        {
            StopCoroutine(
                confirmHoverCoroutine
            );

            confirmHoverCoroutine = null;
        }

        confirmHoverOwner = null;
    }

    /// <summary>
    /// 모든 카드를 현재 슬롯의 위치·회전·크기·알파 상태로 즉시 복구합니다.
    /// </summary>
    private void ResetHoverVisualsInstantly()
    {
        for (int slot = 0;
             slot < orderedCards.Length;
             slot++)
        {
            RuntimeCard card =
                orderedCards[slot];

            slotPoses[slot].Apply(
                card.Rect
            );

            card.CanvasGroup.alpha =
                card.OriginalAlpha;

            card.CanvasGroup.interactable =
                true;

            card.CanvasGroup.blocksRaycasts =
                true;

            ResetConfirmVisual(card);
        }

        if (backShadow != null)
        {
            SetGraphicAlpha(
                backShadow,
                0f
            );
        }
    }

    /// <summary>
    /// 지정한 카드의 Confirm 버튼을 원래 크기와 알파로 복구합니다.
    /// </summary>
    private void ResetConfirmVisual(
        RuntimeCard card
    )
    {
        if (card == null ||
            card.ConfirmRect == null ||
            card.ConfirmGraphic == null)
        {
            return;
        }

        card.ConfirmRect.localScale =
            card.ConfirmOriginalScale;

        SetGraphicAlpha(
            card.ConfirmGraphic,
            card.ConfirmOriginalAlpha
        );
    }

    /// <summary>
    /// 선택 상태일 때만 BackShadow가 카드 외부 클릭을 받을 수 있도록 설정합니다.
    /// </summary>
    private void SetBackShadowInteractive(
        bool interactive
    )
    {
        if (backShadow != null)
        {
            backShadow.raycastTarget =
                interactive;
        }
    }

    /// <summary>
    /// 현재 카드들의 CanvasGroup 알파를 배열로 저장합니다.
    /// </summary>
    private float[] CaptureCardAlphas()
    {
        float[] result =
            new float[orderedCards.Length];

        for (int i = 0;
             i < orderedCards.Length;
             i++)
        {
            result[i] =
                orderedCards[i]
                    .CanvasGroup
                    .alpha;
        }

        return result;
    }

    /// <summary>
    /// 지정한 카드를 제외한 나머지 카드의 알파를 한 번에 변경합니다.
    /// </summary>
    private void SetOtherCardAlpha(
        RuntimeCard excludedCard,
        float alpha
    )
    {
        for (int i = 0;
             i < orderedCards.Length;
             i++)
        {
            RuntimeCard card =
                orderedCards[i];

            if (card != excludedCard)
            {
                card.CanvasGroup.alpha =
                    alpha;
            }
        }
    }

    /// <summary>
    /// 모든 카드의 알파를 최초 값으로 복구합니다.
    /// </summary>
    private void RestoreCardAlphas()
    {
        for (int i = 0;
             i < orderedCards.Length;
             i++)
        {
            orderedCards[i]
                .CanvasGroup
                .alpha =
                orderedCards[i]
                    .OriginalAlpha;
        }
    }

    /// <summary>
    /// 현재 카드 위치와 가장 가까운 슬롯 상태를 반환합니다.
    /// </summary>
    private SlotPose GetNearestSlotPose(
        RectTransform rect
    )
    {
        int nearestSlot = 0;
        float nearestDistance =
            float.MaxValue;

        for (int i = 0;
             i < slotPoses.Length;
             i++)
        {
            float distance =
                Vector3.Distance(
                    rect.anchoredPosition3D,
                    slotPoses[i].Position
                );

            if (distance <
                nearestDistance)
            {
                nearestDistance =
                    distance;

                nearestSlot = i;
            }
        }

        return slotPoses[nearestSlot];
    }

    /// <summary>
    /// 기준 회전에 대한 카드의 상대 Z축 각도를 계산합니다.
    /// </summary>
    private static float GetRelativeAngle(
        RectTransform rect,
        Quaternion baseRotation
    )
    {
        Quaternion relativeRotation =
            Quaternion.Inverse(
                baseRotation
            ) *
            rect.localRotation;

        return Mathf.DeltaAngle(
            0f,
            relativeRotation
                .eulerAngles.z
        );
    }

    /// <summary>
    /// 카드 전환 후 내부 배열 순서를 실제 슬롯 순서와 일치하도록 재배치합니다.
    /// </summary>
    private void ReorderCards(
        int direction
    )
    {
        RuntimeCard[] reordered =
            new RuntimeCard[
                orderedCards.Length
            ];

        for (int i = 0;
             i < orderedCards.Length;
             i++)
        {
            int targetSlot =
                WrapIndex(
                    i + direction,
                    orderedCards.Length
                );

            reordered[targetSlot] =
                orderedCards[i];
        }

        orderedCards = reordered;

        // 전환 후 중앙 카드가 바뀌었으므로 새 중앙 카드를 가장 앞에 표시합니다.
        BringFrontCardToTop();
    }

    /// <summary>
    /// Graphic의 기존 RGB 값은 유지하고 알파만 안전한 범위로 변경합니다.
    /// </summary>
    private static void SetGraphicAlpha(
        Graphic graphic,
        float alpha
    )
    {
        if (graphic == null)
        {
            return;
        }

        Color color =
            graphic.color;

        color.a =
            Mathf.Clamp01(alpha);

        graphic.color =
            color;
    }

    /// <summary>
    /// 배열 인덱스가 범위를 벗어나도 반대쪽으로 순환하도록 보정합니다.
    /// </summary>
    private static int WrapIndex(
        int index,
        int length
    )
    {
        return
            (index % length + length) %
            length;
    }

    /// <summary>
    /// 시작과 끝이 부드러운 보간 곡선을 계산합니다.
    /// </summary>
    private static float SmoothStep(
        float value
    )
    {
        return value *
               value *
               (3f - 2f * value);
    }

    /// <summary>
    /// 빠르게 시작한 뒤 천천히 멈추는 3차 감속 곡선을 계산합니다.
    /// </summary>
    private static float EaseOutCubic(
        float value
    )
    {
        float inverse =
            1f - value;

        return
            1f -
            inverse *
            inverse *
            inverse;
    }

    /// <summary>
    /// 천천히 시작한 뒤 빠르게 진행되는 3차 가속 곡선을 계산합니다.
    /// </summary>
    private static float EaseInCubic(
        float value
    )
    {
        return value *
               value *
               value;
    }
}
