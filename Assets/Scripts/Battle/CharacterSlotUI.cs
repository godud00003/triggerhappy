using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSlotUI : MonoBehaviour
{
    [Header("UI 연결")]
    public Image portraitImage;      // 초상화
    public TextMeshProUGUI nameText; // 이름
    public TextMeshProUGUI hpText;   // HP 숫자 (100/100)
    public Image hpBarFill;          // HP 게이지 (빨간색)

    // 이 슬롯에 데이터를 채워넣는 함수
    public void Setup(CharacterData data, int currentHp)
    {
        if (data == null)
        {
            // 데이터가 없으면 숨김 (혹은 빈칸 처리)
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (portraitImage) portraitImage.sprite = data.portrait;
        if (nameText) nameText.text = data.characterName;

        UpdateHp(currentHp, data.maxHp);
    }

    // 체력만 따로 갱신하는 함수
    public void UpdateHp(int current, int max)
    {
        if (hpText) hpText.text = $"{current}/{max}";

        if (hpBarFill)
        {
            // Simple 타입이면 스케일 조절, Filled 타입이면 fillAmount 조절
            // (범용성을 위해 두 방식 모두 지원하도록 작성)
            if (hpBarFill.type == Image.Type.Filled)
            {
                hpBarFill.fillAmount = (float)current / max;
            }
            else
            {
                hpBarFill.rectTransform.localScale = new Vector3((float)current / max, 1, 1);
            }
        }
    }
}