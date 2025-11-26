using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

// [핵심] 클래스 이름이 파일명과 똑같아야 함!
public class CylinderSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    // ... (내용은 기존과 동일) ...
    public int slotIndex;
    public bool isLoaded = false;
    public CardData loadedCard;

    [Header("UI 연결")]
    public Image iconImage;
    public Text nameText;

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
            }
        }
    }

    public void LoadBullet(CardData data)
    {
        isLoaded = true;
        loadedCard = data;

        if (iconImage)
        {
            iconImage.enabled = true;
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.color = data.themeColor;
            }
        }
        if (nameText) nameText.text = data.cardName;

        StartCoroutine(ShakeRoutine());
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isLoaded) return;

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
        if (!isLoaded) return;

        BattleManager gm = FindFirstObjectByType<BattleManager>();
        if (gm != null) gm.ReturnCardToHand(loadedCard);

        ClearSlot();
        if (dragVisualObj != null) Destroy(dragVisualObj);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLoaded && eventData.button == PointerEventData.InputButton.Right)
        {
            BattleManager gm = FindFirstObjectByType<BattleManager>();
            if (gm != null) gm.ReturnCardToHand(loadedCard);
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        isLoaded = false;
        loadedCard = null;
        if (iconImage) iconImage.enabled = false;
        if (nameText) nameText.text = (slotIndex + 1).ToString();
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