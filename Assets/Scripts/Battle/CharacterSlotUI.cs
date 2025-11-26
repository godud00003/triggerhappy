using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSlotUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image hpBarFill;

    [Header("상호작용")]
    // 인스펙터에서 Add Component -> Button 후 연결하세요
    public Button slotButton;

    public CharacterData CurrentData { get; private set; }
    private BattleManager battleManager;

    void Start()
    {
        battleManager = FindFirstObjectByType<BattleManager>();

        if (slotButton == null) slotButton = GetComponent<Button>();

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnClickSlot);
        }
    }

    public void Setup(CharacterData data, int currentHp)
    {
        CurrentData = data;

        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        if (portraitImage) portraitImage.sprite = data.portrait;
        if (nameText) nameText.text = data.characterName;

        UpdateHp(currentHp, data.maxHp);
    }

    public void UpdateHp(int current, int max)
    {
        if (hpText) hpText.text = $"{current}/{max}";

        if (hpBarFill)
        {
            if (hpBarFill.type == Image.Type.Filled)
                hpBarFill.fillAmount = (float)current / max;
            else
                hpBarFill.rectTransform.localScale = new Vector3((float)current / max, 1, 1);
        }
    }

    public void SetInteractable(bool isInteractable)
    {
        if (slotButton)
        {
            slotButton.interactable = isInteractable;

            // 시각적 피드백 (회색 처리)
            if (portraitImage)
            {
                portraitImage.color = isInteractable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
    }

    // 클릭 이벤트
    void OnClickSlot()
    {
        // [디버깅] 클릭 시 이 로그가 안 뜨면 Raycast Target 문제입니다.
        Debug.Log($"[UI] 슬롯 클릭됨: {CurrentData?.characterName}");

        if (battleManager != null && CurrentData != null)
        {
            battleManager.OnClick_SubCharacter(CurrentData);
        }
    }
}