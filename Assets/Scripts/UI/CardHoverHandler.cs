using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 카드 호버/드래그 이벤트를 HandManager에 전달
/// </summary>
public class CardHoverHandler : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private HandManager handManager;
    private GameObject cardObject;
    private bool isInitialized = false;
    
    public void Initialize(HandManager manager, GameObject card)
    {
        handManager = manager;
        cardObject = card;
        isInitialized = true;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInitialized || handManager == null) return;
        handManager.SetCardHovered(cardObject, true);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInitialized || handManager == null) return;
        handManager.SetCardHovered(cardObject, false);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isInitialized || handManager == null) return;
        handManager.SetCardDragging(cardObject, true);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        // 드래그 중 위치는 HandManager에서 처리
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isInitialized || handManager == null) return;
        handManager.SetCardDragging(cardObject, false);
    }
}
