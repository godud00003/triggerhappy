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
    public bool isSpecialCard = false;  // SP카드인지 여부

    [Header("UI 연결")]
    public Image iconImage;
    public Text nameText;

    [Header("SP카드 시각 효과")]
    public Color specialCardTint = new Color(1f, 0.85f, 0.4f); // 금색 틴트
    public GameObject specialEffectObj; // 반짝이 이펙트 (선택)

    private Vector3 initialScale;
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;
    private Canvas rootCanvas;
    private GameObject dragVisualObj;
    private Color originalIconColor = Color.white;

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
                LoadBullet(card.cardData);
                Destroy(droppedObj);

                // ★ 스킬 시스템에 알림
                NotifyCardLoaded();
            }
        }
    }

    /// <summary>
    /// 카드 장전 (일반/SP카드 구분)
    /// </summary>
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
                // SP카드면 특별한 색상
                iconImage.color = isSpecial ? specialCardTint : Color.white;
            }
            else
            {
                iconImage.color = isSpecial ? specialCardTint : data.themeColor;
            }
        }
        if (nameText) nameText.text = data.cardName;

        // SP카드 이펙트
        if (specialEffectObj) specialEffectObj.SetActive(isSpecial);

        StartCoroutine(ShakeRoutine());
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // SP카드는 드래그로 해제 불가 (자동 소멸만 가능)
        if (!isLoaded || isSpecialCard) return;

        if (rootCanvas == null) rootCanvas = FindFirstObjectByType<Canvas>();
        if (rootCanvas == null || iconImage == null) return;

        dragVisualObj = new GameObject("DragIcon");
        dragVisualObj.transform.SetParent(rootCanvas.transform);
        dragVisualObj.transform.localScale = Vector3.one;

        Image img = dragVisualObj.AddComponent<Image>();
        img.sprite = iconImage.sprite;
        img.color = iconImage.color;
        img.raycastTarget = false;

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

        BattleManager gm = FindFirstObjectByType<BattleManager>();
        if (gm != null) gm.ReturnCardToHand(loadedCard);

        // ★ 스킬 시스템에 알림 (해제 전에)
        int myIndex = slotIndex;

        ClearSlot(sendToDiscard: false);

        // ★ 해제 후 스킬 체크
        if (gm != null) NotifyCardUnloaded(gm, myIndex);

        if (dragVisualObj != null) Destroy(dragVisualObj);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // SP카드는 우클릭 해제 불가
        if (isLoaded && !isSpecialCard && eventData.button == PointerEventData.InputButton.Right)
        {
            BattleManager gm = FindFirstObjectByType<BattleManager>();
            int myIndex = slotIndex;

            if (gm != null) gm.ReturnCardToHand(loadedCard);
            ClearSlot(sendToDiscard: false);

            // ★ 해제 후 스킬 체크
            if (gm != null) NotifyCardUnloaded(gm, myIndex);
        }
    }

    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    /// <param name="sendToDiscard">true면 묘지로, false면 그냥 소멸</param>
    public void ClearSlot(bool sendToDiscard = true)
    {
        // 묘지로 보내기 (SP카드가 아니고, sendToDiscard가 true일 때만)
        if (sendToDiscard && loadedCard != null && !isSpecialCard)
        {
            BattleManager gm = FindFirstObjectByType<BattleManager>();
            if (gm != null)
            {
                gm.DiscardCard(loadedCard);
                Debug.Log($"🗑️ [Slot] '{loadedCard.cardName}' → 묘지");
            }
        }
        else if (loadedCard != null && isSpecialCard)
        {
            Debug.Log($"💨 [Slot] SP카드 '{loadedCard.cardName}' 소멸!");
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

    /// <summary>
    /// 스킬 시스템에 카드 장전 알림
    /// </summary>
    void NotifyCardLoaded()
    {
        BattleManager gm = FindFirstObjectByType<BattleManager>();
        if (gm != null && gm.activeCharacter != null && gm.activeCharacter.characterSkills != null)
        {
            foreach (var skill in gm.activeCharacter.characterSkills)
            {
                if (skill != null)
                {
                    skill.OnCardLoaded(gm, slotIndex, loadedCard);
                }
            }
        }
    }

    /// <summary>
    /// 스킬 시스템에 카드 해제 알림
    /// </summary>
    void NotifyCardUnloaded(BattleManager gm, int index)
    {
        if (gm != null && gm.activeCharacter != null && gm.activeCharacter.characterSkills != null)
        {
            foreach (var skill in gm.activeCharacter.characterSkills)
            {
                if (skill != null)
                {
                    skill.OnCardUnloaded(gm, index);
                }
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
        rectTransform.anchoredPosition = originalAnchoredPos + new Vector2(0, 50f);
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