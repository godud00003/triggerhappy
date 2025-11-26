using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // [필수] TextMeshPro 사용

public class BulletCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData cardData;
    public string bulletType = "fire";

    [Header("UI 연결")]
    public Image cardBackground;
    public Image cardIcon;
    public Text cardNameText;            // Legacy Text
    public TextMeshProUGUI cardNameTextTMP; // TMP 제목
    public TextMeshProUGUI cardDescriptionTMP; // [New] 동적 텍스트용 설명

    [Header("하이라이트")]
    public GameObject rewardHighlight;

    // 드래그 관련 변수
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

        // [Observer] 매니저의 캐릭터 교체 이벤트 구독
        var bm = FindFirstObjectByType<BattleManager>();
        if (bm != null)
        {
            bm.OnCharacterChanged += UpdateDynamicText;
        }

        // 초기화 시 텍스트 갱신
        UpdateDynamicText();
    }

    void OnDestroy()
    {
        // [중요] 구독 해제
        var bm = FindFirstObjectByType<BattleManager>();
        if (bm != null)
        {
            bm.OnCharacterChanged -= UpdateDynamicText;
        }
    }

    public void Setup(CardData data)
    {
        cardData = data;
        bulletType = data.cardName;

        if (cardBackground != null) cardBackground.color = data.themeColor;

        if (cardIcon != null)
        {
            if (data.icon != null)
            {
                cardIcon.sprite = data.icon;
                cardIcon.enabled = true;
                cardIcon.color = Color.white;
            }
            else
            {
                cardIcon.enabled = false;
            }
        }

        if (cardNameText != null) cardNameText.text = data.cardName;
        if (cardNameTextTMP != null) cardNameTextTMP.text = data.cardName;

        CheckIfRewardCard();
        UpdateDynamicText();
    }

    // [New] 동적 텍스트 갱신 로직
    void UpdateDynamicText()
    {
        if (cardData == null) return;

        var bm = FindFirstObjectByType<BattleManager>();
        // 예외 처리: 매니저나 캐릭터 정보가 없으면 리턴
        if (bm == null || bm.activeCharacter == null || bm.activeCharacter.weaponStrategy == null) return;

        WeaponData currentWeapon = bm.activeCharacter.weaponStrategy;

        if (cardDescriptionTMP != null)
        {
            string originalDesc = cardData.description;

            int baseVal = 0;
            if (cardData.actions != null && cardData.actions.Count > 0)
            {
                baseVal = cardData.actions[0].value;
            }

            string dynamicValue = currentWeapon.GetDamageText(baseVal);

            // {D} 치환 로직
            string finalDesc = originalDesc.Replace("{D}", dynamicValue);

            if (!originalDesc.Contains("{D}"))
            {
                // 플레이스홀더가 없으면 괄호로 덧붙임
                finalDesc = $"{originalDesc} ({dynamicValue})";
            }

            cardDescriptionTMP.text = finalDesc;
        }
    }

    void CheckIfRewardCard()
    {
        if (GetComponentInParent<RewardManager>() != null)
        {
            transform.localScale = Vector3.one * 1.2f;
            if (rewardHighlight != null) rewardHighlight.SetActive(true);
        }
        else
        {
            transform.localScale = Vector3.one;
            if (rewardHighlight != null) rewardHighlight.SetActive(false);
        }
    }

    // [복구됨] 드래그 인터페이스 구현
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GetComponentInParent<RewardManager>() != null) return;

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

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