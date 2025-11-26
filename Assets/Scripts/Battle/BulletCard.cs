using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // [필수] TextMeshPro 사용

public class BulletCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData cardData;
    public string bulletType = "fire";

    [Header("UI 연결 (프리팹 내부)")]
    public Image cardBackground;
    public Image cardIcon;
    public Text cardNameText;    // Legacy Text
    public TextMeshProUGUI cardNameTextTMP; // [추가] TMP Text

    // [추가] 보상 카드일 때 강조할 오브젝트 (선택)
    public GameObject rewardHighlight;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalPosition;
    private Canvas rootCanvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        if (cardData != null) Setup(cardData);
    }

    public void Setup(CardData data)
    {
        cardData = data;
        bulletType = data.cardName;

        // 1. 배경 색상 (테마)
        if (cardBackground != null)
        {
            cardBackground.color = data.themeColor;
        }

        // 2. 아이콘
        if (cardIcon != null)
        {
            if (data.icon != null)
            {
                cardIcon.sprite = data.icon;
                cardIcon.enabled = true;
                cardIcon.color = Color.white; // 원본 색상 유지
            }
            else
            {
                cardIcon.enabled = false;
            }
        }

        // 3. 이름 텍스트 (Legacy)
        if (cardNameText != null)
        {
            cardNameText.text = data.cardName;
        }
        // 4. 이름 텍스트 (TMP)
        if (cardNameTextTMP != null)
        {
            cardNameTextTMP.text = data.cardName;
        }

        CheckIfRewardCard();
    }

    void CheckIfRewardCard()
    {
        if (GetComponentInParent<RewardManager>() != null)
        {
            // 보상 카드 스타일
            transform.localScale = Vector3.one * 1.2f;
            if (rewardHighlight != null) rewardHighlight.SetActive(true);
        }
        else
        {
            // 일반 패 스타일
            transform.localScale = Vector3.one;
            if (rewardHighlight != null) rewardHighlight.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GetComponentInParent<RewardManager>() != null) return;

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        // 드래그 시 최상단 렌더링
        if (rootCanvas != null) transform.SetParent(rootCanvas.transform, true);

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GetComponentInParent<RewardManager>() != null) return;
        if (rootCanvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (GetComponentInParent<RewardManager>() != null) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == rootCanvas.transform)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}