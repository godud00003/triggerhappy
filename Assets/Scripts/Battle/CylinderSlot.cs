using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class CylinderSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public int slotIndex;
    public bool isLoaded = false;
    public CardData loadedCard;

    [Header("★ SP카드 여부")]
    public bool isSpecialCard = false;

    [Header("UI 연결")]
    public Image iconImage;
    public Text nameText;

    [Header("SP카드 시각 효과")]
    public Color specialCardTint = new Color(1f, 0.85f, 0.4f);
    public GameObject specialEffectObj;

    private Vector3 initialScale;
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;
    private Canvas rootCanvas;
    private GameObject dragVisualObj;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        initialScale = transform.localScale;
        originalAnchoredPos = rectTransform.anchoredPosition;
        if (rootCanvas == null) rootCanvas = FindFirstObjectByType<Canvas>();
    }

    void LateUpdate()
    {
        // 슬롯이 회전하더라도 아이콘 등은 정방향 유지 (필요시)
        transform.rotation = Quaternion.identity;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isLoaded) return;

        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj != null)
        {
            BulletCard card = droppedObj.GetComponent<BulletCard>();
            if (card != null && card.cardData != null)
            {
                // [Fix] 장전 애니메이션 제거 및 즉시 처리

                // 1. HandManager에서 제거
                HandManager handManager = FindFirstObjectByType<HandManager>();
                if (handManager != null) handManager.RemoveCard(droppedObj);

                // 2. 데이터 장전
                LoadBullet(card.cardData);

                // 3. 드롭된 카드 오브젝트는 즉시 파괴 (슬롯 아이콘으로 대체됨)
                Destroy(droppedObj);

                // 4. 스킬 시스템 알림
                NotifyCardLoaded();
            }
        }
    }

    public void LoadBullet(CardData data, bool isSpecial = false)
    {
        isLoaded = true;
        loadedCard = data;
        isSpecialCard = isSpecial;

        if (iconImage)
        {
            iconImage.enabled = true;
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.color = isSpecial ? specialCardTint : Color.white;
            }
            else
            {
                iconImage.color = isSpecial ? specialCardTint : data.themeColor;
            }
        }
        if (nameText) nameText.text = data.cardName;

        if (specialEffectObj) specialEffectObj.SetActive(isSpecial);

        // 장전 시 가볍게 흔들림 효과
        StartCoroutine(ShakeRoutine());
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isLoaded || isSpecialCard) return;

        if (rootCanvas == null) rootCanvas = FindFirstObjectByType<Canvas>();
        if (rootCanvas == null || iconImage == null) return;

        // 드래그 시작 시 시각적 아이콘 생성
        dragVisualObj = new GameObject("DragIcon");
        dragVisualObj.transform.SetParent(rootCanvas.transform);
        dragVisualObj.transform.localScale = Vector3.one;

        Image img = dragVisualObj.AddComponent<Image>();
        img.sprite = iconImage.sprite;
        img.color = iconImage.color;
        img.raycastTarget = false;

        // 원본 아이콘 숨김
        iconImage.enabled = false;
        if (nameText) nameText.text = "";
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisualObj != null) dragVisualObj.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isLoaded || isSpecialCard) return;

        // 드래그 종료 시 카드를 손패로 반환
        BattleManager gm = FindFirstObjectByType<BattleManager>();
        if (gm != null) gm.ReturnCardToHand(loadedCard);

        int myIndex = slotIndex;
        ClearSlot(sendToDiscard: false); // 슬롯 비우기 (묘지로 안 보냄)

        if (gm != null) NotifyCardUnloaded(gm, myIndex);

        if (dragVisualObj != null) Destroy(dragVisualObj);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 우클릭 시 해제
        if (isLoaded && !isSpecialCard && eventData.button == PointerEventData.InputButton.Right)
        {
            BattleManager gm = FindFirstObjectByType<BattleManager>();
            int myIndex = slotIndex;

            if (gm != null) gm.ReturnCardToHand(loadedCard);
            ClearSlot(sendToDiscard: false);
            if (gm != null) NotifyCardUnloaded(gm, myIndex);
        }
    }

    public void ClearSlot(bool sendToDiscard = true)
    {
        if (sendToDiscard && loadedCard != null && !isSpecialCard)
        {
            BattleManager gm = FindFirstObjectByType<BattleManager>();
            if (gm != null) gm.DiscardCard(loadedCard);
        }

        isLoaded = false;
        loadedCard = null;
        isSpecialCard = false;

        if (iconImage)
        {
            iconImage.enabled = false;
            iconImage.color = Color.white;
        }
        if (nameText) nameText.text = (slotIndex + 1).ToString();
        if (specialEffectObj) specialEffectObj.SetActive(false);
    }

    void NotifyCardLoaded()
    {
        BattleManager gm = FindFirstObjectByType<BattleManager>();
        if (gm != null && gm.activeCharacter != null && gm.activeCharacter.characterSkills != null)
        {
            foreach (var skill in gm.activeCharacter.characterSkills)
            {
                if (skill != null) skill.OnCardLoaded(gm, slotIndex, loadedCard);
            }
        }
    }

    void NotifyCardUnloaded(BattleManager gm, int index)
    {
        if (gm != null && gm.activeCharacter != null && gm.activeCharacter.characterSkills != null)
        {
            foreach (var skill in gm.activeCharacter.characterSkills)
            {
                if (skill != null) skill.OnCardUnloaded(gm, index);
            }
        }
    }

    public void PlayFireEffect()
    {
        StartCoroutine(FireAnim());
    }

    IEnumerator FireAnim()
    {
        transform.localScale = initialScale * 1.2f;
        rectTransform.anchoredPosition = originalAnchoredPos - new Vector2(0, 10f);
        yield return new WaitForSeconds(0.1f);
        transform.localScale = initialScale;
        rectTransform.anchoredPosition = originalAnchoredPos;
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        float duration = 0.2f;
        float magnitude = 10f;
        while (elapsed < duration)
        {
            rectTransform.anchoredPosition = originalAnchoredPos + new Vector2(Random.Range(-1f, 1f) * magnitude, Random.Range(-1f, 1f) * magnitude);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = originalAnchoredPos;
    }
}