using UnityEngine;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Slay the Spire 스타일 손패 관리 시스템 (Fix Version 2.1)
/// - [Fix] 외부에서 생성된 카드(실린더 반환 등) 자동 감지 로직 추가 (중앙 쌓임 해결)
/// - [Fix] 드래그 좌표계 및 슬롯 간섭 완벽 차단
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("손패 설정")]
    public RectTransform handArea;
    public float cardSpacing = 120f;
    public float maxHandWidth = 800f;

    [Header("부채꼴 배치")]
    public bool useFanLayout = true;
    public float fanAngle = 5f;
    public float fanVerticalOffset = 10f;

    [Header("호버 효과")]
    public float hoverRaiseAmount = 30f;
    public float hoverScale = 1.15f;
    public float neighborPushAmount = 30f;

    [Header("애니메이션")]
    public float moveSpeed = 15f;
    public float rotateSpeed = 15f;
    public float scaleSpeed = 15f;

    [Header("드래그 연출")]
    public float dragMoveSpeed = 40f;
    public float dragTiltStrength = 1.5f;

    [Header("드로우 애니메이션")]
    public float drawCurveHeight = 80f;
    public float drawDuration = 0.3f;

    private List<CardState> cards = new List<CardState>();
    private CardState hoveredCard = null;
    private CardState draggedCard = null;
    private Canvas rootCanvas;

    private class CardState
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public BulletCard bulletCard;
        public CanvasGroup canvasGroup;

        public Vector2 targetPosition;
        public float targetRotation;
        public float targetScale;

        public Vector2 currentPosition;
        public float currentRotation;
        public float currentScale;

        public bool isDrawing;
        public float drawProgress;
        public Vector2 drawStartPos;

        public bool isHovered;
        public bool isDragging;

        public Vector2 dragLocalOffset;

        public int handIndex;
    }

    void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) Debug.LogError("⛔ [HandManager] 부모에 Canvas가 없습니다!");
    }

    void Update()
    {
        CleanupDestroyedCards();

        // [New] 자동 카드 감지 (외부에서 생성된 카드 등록)
        AutoRegisterCards();

        if (cards.Count == 0) return;

        EnforceCardHierarchy();
        CalculateTargetPositions();
        UpdateCardTransforms();
        UpdateSiblingIndices();
    }

    /// <summary>
    /// [New] HandArea 자식으로 들어왔지만 아직 리스트에 없는 카드를 찾아 등록합니다.
    /// 실린더에서 카드를 뺄 때 발생하는 문제를 해결합니다.
    /// </summary>
    void AutoRegisterCards()
    {
        foreach (Transform child in handArea)
        {
            GameObject obj = child.gameObject;

            // BulletCard 컴포넌트가 있고, 비활성화 상태가 아닌지 확인
            BulletCard bullet = obj.GetComponent<BulletCard>();
            if (bullet == null || !obj.activeInHierarchy) continue;

            // 이미 등록된 카드인지 확인
            if (HasCard(obj)) continue;

            // 등록되지 않은 새 카드 발견 -> 등록 절차 진행
            // (드로우 애니메이션 없이 즉시 등록)
            RegisterNewCard(obj);
        }
    }

    void RegisterNewCard(GameObject cardObj)
    {
        CardState state = new CardState
        {
            gameObject = cardObj,
            rectTransform = cardObj.GetComponent<RectTransform>(),
            bulletCard = cardObj.GetComponent<BulletCard>(),
            canvasGroup = cardObj.GetComponent<CanvasGroup>(),
            targetScale = 1f,
            currentScale = 1f, // 이미 생성된 상태이므로 1로 시작
            currentPosition = cardObj.GetComponent<RectTransform>().anchoredPosition, // 현재 위치 유지
            isDrawing = false, // 드로우 애니메이션 스킵
            handIndex = cards.Count
        };

        if (state.canvasGroup == null) state.canvasGroup = cardObj.AddComponent<CanvasGroup>();

        // 이벤트 핸들러 부착
        SetupCardEvents(state);

        cards.Add(state);
    }

    void CleanupDestroyedCards()
    {
        bool needsReindex = false;
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            if (cards[i] == null || cards[i].gameObject == null || cards[i].rectTransform == null)
            {
                if (cards[i] == hoveredCard) hoveredCard = null;
                if (cards[i] == draggedCard) draggedCard = null;
                cards.RemoveAt(i);
                needsReindex = true;
            }
        }

        if (needsReindex)
        {
            for (int i = 0; i < cards.Count; i++) cards[i].handIndex = i;
        }
    }

    void EnforceCardHierarchy()
    {
        foreach (var card in cards)
        {
            if (!card.isDragging && card.gameObject != null && card.rectTransform.parent != handArea)
            {
                card.rectTransform.SetParent(handArea, true);
            }
        }
    }

    public void AddCard(GameObject cardObj, Vector3 fromWorldPos)
    {
        // 중복 등록 방지
        if (HasCard(cardObj)) return;

        CardState state = new CardState
        {
            gameObject = cardObj,
            rectTransform = cardObj.GetComponent<RectTransform>(),
            bulletCard = cardObj.GetComponent<BulletCard>(),
            canvasGroup = cardObj.GetComponent<CanvasGroup>(),
            targetScale = 1f,
            currentScale = 0.5f,
            isDrawing = true,
            drawProgress = 0f,
            handIndex = cards.Count
        };

        if (state.canvasGroup == null) state.canvasGroup = cardObj.AddComponent<CanvasGroup>();

        cardObj.transform.SetParent(handArea, false);
        state.drawStartPos = handArea.InverseTransformPoint(fromWorldPos);
        state.currentPosition = state.drawStartPos;

        state.canvasGroup.alpha = 0.8f;
        state.rectTransform.anchoredPosition = state.drawStartPos;
        state.rectTransform.localScale = Vector3.one * 0.5f;
        state.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
        state.currentRotation = state.rectTransform.localRotation.eulerAngles.z;

        cards.Add(state);
        SetupCardEvents(state);
    }

    public void RemoveCard(GameObject cardObj)
    {
        CardState state = cards.Find(c => c.gameObject == cardObj);
        if (state != null)
        {
            if (state == hoveredCard) hoveredCard = null;
            if (state == draggedCard) draggedCard = null;

            cards.Remove(state);

            for (int i = 0; i < cards.Count; i++) cards[i].handIndex = i;
        }
    }

    void CalculateTargetPositions()
    {
        int cardCount = cards.Count;
        if (cardCount == 0) return;

        float totalWidth = (cardCount - 1) * cardSpacing;
        if (totalWidth > maxHandWidth && cardCount > 1) totalWidth = maxHandWidth;
        float startX = -totalWidth / 2f;
        float actualSpacing = (cardCount > 1) ? totalWidth / (cardCount - 1) : 0;

        for (int i = 0; i < cardCount; i++)
        {
            CardState card = cards[i];
            if (card.gameObject == null) continue;

            float x = startX + i * actualSpacing;
            float y = 0f;
            float rotation = 0f;
            float scale = 1f;

            if (useFanLayout && cardCount > 1)
            {
                float centerIndex = (cardCount - 1) / 2f;
                float offsetFromCenter = i - centerIndex;
                rotation = -offsetFromCenter * fanAngle;
                y = -Mathf.Abs(offsetFromCenter) * fanVerticalOffset;
            }

            if (card.isDragging)
            {
                Vector2 mouseLocalPos = GetMouseLocalPosition(handArea);
                card.targetPosition = mouseLocalPos - card.dragLocalOffset;

                float moveX = (card.targetPosition.x - card.currentPosition.x);
                float tilt = -moveX * dragTiltStrength;
                card.targetRotation = Mathf.Clamp(tilt, -30f, 30f);
                card.targetScale = 1.1f;
            }
            else
            {
                card.targetPosition = new Vector2(x, y);
                card.targetRotation = rotation;
                card.targetScale = scale;
            }
        }

        for (int i = 0; i < cardCount; i++)
        {
            CardState card = cards[i];
            if (card == null || card.isDragging) continue;

            if (card.isHovered)
            {
                card.targetPosition.y += hoverRaiseAmount;
                card.targetScale = hoverScale;
                card.targetRotation = 0f;

                for (int j = 0; j < cardCount; j++)
                {
                    if (j != i && cards[j] != null && !cards[j].isDragging)
                    {
                        float distance = j - i;
                        float pushFactor = 1f / Mathf.Max(Mathf.Abs(distance), 1f);
                        float push = neighborPushAmount * pushFactor * Mathf.Sign(distance);
                        cards[j].targetPosition.x += push;
                    }
                }
            }
        }
    }

    void UpdateCardTransforms()
    {
        float dt = Time.deltaTime;

        foreach (CardState card in cards)
        {
            if (card.rectTransform == null) continue;

            if (card.isDrawing)
            {
                card.drawProgress += dt / drawDuration;
                if (card.drawProgress >= 1f)
                {
                    card.isDrawing = false;
                    card.drawProgress = 1f;
                    if (card.canvasGroup != null) card.canvasGroup.alpha = 1f;
                }

                float t = Mathf.SmoothStep(0, 1, card.drawProgress);
                card.currentPosition = Vector2.Lerp(card.drawStartPos, card.targetPosition, t);
                card.currentPosition.y += Mathf.Sin(card.drawProgress * Mathf.PI) * drawCurveHeight;
                card.currentScale = Mathf.Lerp(0.5f, card.targetScale, t);
                card.currentRotation = Mathf.LerpAngle(card.currentRotation, card.targetRotation, t);

                if (card.canvasGroup != null) card.canvasGroup.alpha = Mathf.Lerp(0.8f, 1f, t);
            }
            else if (card.isDragging)
            {
                card.currentPosition = Vector2.Lerp(card.currentPosition, card.targetPosition, dt * dragMoveSpeed);
                card.currentRotation = Mathf.LerpAngle(card.currentRotation, card.targetRotation, dt * rotateSpeed);
                card.currentScale = Mathf.Lerp(card.currentScale, card.targetScale, dt * scaleSpeed);
            }
            else
            {
                card.currentPosition = Vector2.Lerp(card.currentPosition, card.targetPosition, dt * moveSpeed);
                card.currentRotation = Mathf.LerpAngle(card.currentRotation, card.targetRotation, dt * rotateSpeed);
                card.currentScale = Mathf.Lerp(card.currentScale, card.targetScale, dt * scaleSpeed);
            }

            card.rectTransform.anchoredPosition = card.currentPosition;
            card.rectTransform.localRotation = Quaternion.Euler(0, 0, card.currentRotation);
            card.rectTransform.localScale = Vector3.one * card.currentScale;
        }
    }

    void UpdateSiblingIndices()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            CardState card = cards[i];
            if (card == null || card.rectTransform == null) continue;

            if (!card.isDragging && !card.isHovered)
            {
                card.rectTransform.SetSiblingIndex(card.handIndex);
            }
        }

        if (hoveredCard != null && hoveredCard.rectTransform != null && !hoveredCard.isDragging)
        {
            hoveredCard.rectTransform.SetAsLastSibling();
        }

        if (draggedCard != null && draggedCard.rectTransform != null)
        {
            draggedCard.rectTransform.SetAsLastSibling();
        }
    }

    private Vector2 GetMouseLocalPosition(RectTransform targetRect)
    {
        Vector2 mouseScreenPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Pointer.current != null) mouseScreenPos = Pointer.current.position.ReadValue();
        else if (Mouse.current != null) mouseScreenPos = Mouse.current.position.ReadValue();
#else
        mouseScreenPos = Input.mousePosition;
#endif

        Vector2 localPos;
        Camera cam = null;
        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, mouseScreenPos, cam, out localPos);
        return localPos;
    }

    void SetupCardEvents(CardState state)
    {
        BulletCard card = state.bulletCard;
        if (card == null) return;

        CardHoverHandler handler = state.gameObject.GetComponent<CardHoverHandler>();
        if (handler == null) handler = state.gameObject.AddComponent<CardHoverHandler>();
        handler.Initialize(this, state.gameObject);
    }

    public void SetCardHovered(GameObject cardObj, bool isHovered)
    {
        CardState state = cards.Find(c => c.gameObject == cardObj);
        if (state != null)
        {
            state.isHovered = isHovered;
            hoveredCard = isHovered ? state : null;
        }
    }

    public void SetCardDragging(GameObject cardObj, bool isDragging)
    {
        CardState state = cards.Find(c => c.gameObject == cardObj);
        if (state != null)
        {
            state.isDragging = isDragging;

            if (isDragging)
            {
                draggedCard = state;
                Vector2 mouseLocalPos = GetMouseLocalPosition(handArea);
                state.dragLocalOffset = mouseLocalPos - state.rectTransform.anchoredPosition;
            }
            else
            {
                if (draggedCard != null) ReorderCardByPosition(draggedCard);
                draggedCard = null;
            }
        }
    }

    void ReorderCardByPosition(CardState draggedState)
    {
        if (cards.Count <= 1) return;

        float draggedX = draggedState.currentPosition.x;
        int newIndex = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == draggedState) continue;
            if (cards[i].targetPosition.x < draggedX) newIndex++;
        }

        cards.Remove(draggedState);
        newIndex = Mathf.Clamp(newIndex, 0, cards.Count);
        cards.Insert(newIndex, draggedState);

        for (int i = 0; i < cards.Count; i++) cards[i].handIndex = i;
    }

    public bool HasCard(GameObject cardObj) => cards.Exists(c => c.gameObject == cardObj);
    public int CardCount => cards.Count;
    public void ClearHand()
    {
        cards.Clear();
        hoveredCard = null;
        draggedCard = null;
    }
}