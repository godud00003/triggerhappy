using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BattleUIManager : MonoBehaviour
{
    [Header("★ 오브젝트 연결")]
    public Transform cylinderPivot;
    public Transform handArea;
    public GameObject cardPrefab;
    public List<CylinderSlot> slots;

    [Header("★ UI 연결")]
    public CharacterSlotUI mainCharUI;
    public List<CharacterSlotUI> subCharSlots;

    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI discardCountText;
    public TextMeshProUGUI spText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("★ 팝업 UI")]
    public GameObject graveyardPanel;
    public Transform graveyardContent;
    public GameObject statusPanel;
    public TextMeshProUGUI statusText;

    [Header("★ QTE UI")]
    public GameObject qtePanel;
    public Image qteDimmedBG;
    public Image qteTimerBar;
    public Image qteLeftPortrait;
    public Image qteRightPortrait;
    public GameObject qteLeftGroup;
    public GameObject qteRightGroup;

    public Transform CanvasTransform { get; private set; }

    void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) CanvasTransform = canvas.transform;

        if (graveyardPanel) graveyardPanel.SetActive(false);
        if (statusPanel) statusPanel.SetActive(false);
    }

    public void UpdateAllHpUI(CharacterData active, List<CharacterData> subs, Dictionary<CharacterData, int> hpMap)
    {
        if (mainCharUI) mainCharUI.Setup(active, hpMap.ContainsKey(active) ? hpMap[active] : 0);

        for (int i = 0; i < subCharSlots.Count; i++)
        {
            if (i < subs.Count)
            {
                CharacterData subData = subs[i];
                int hp = (subData && hpMap.ContainsKey(subData)) ? hpMap[subData] : 0;
                subCharSlots[i].Setup(subData, hp);
            }
            else
            {
                subCharSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void UpdateSlotInteractability(int currentSP, int swapCost, CharacterData activeChar)
    {
        foreach (var slot in subCharSlots)
        {
            if (slot != null && slot.CurrentData != null)
            {
                bool interactable = (slot.CurrentData != activeChar) && (currentSP >= swapCost);
                slot.SetInteractable(interactable);
            }
        }
    }

    public void UpdateDeckCount(int deckCount, int discardCount)
    {
        if (deckCountText) deckCountText.text = deckCount.ToString();
        if (discardCountText) discardCountText.text = discardCount.ToString();
    }

    public void UpdateSP(int current, int max)
    {
        if (spText) spText.text = $"SP: {current}/{max}";
    }

    public void ShowGraveyardPopup(List<CardData> discardPile)
    {
        if (!graveyardPanel) return;
        graveyardPanel.SetActive(true);
        foreach (Transform child in graveyardContent) Destroy(child.gameObject);

        foreach (var data in discardPile)
        {
            GameObject obj = Instantiate(cardPrefab, graveyardContent);
            BulletCard cardScript = obj.GetComponent<BulletCard>();
            cardScript.Setup(data);
            Destroy(cardScript);
            Destroy(obj.GetComponent<Button>());
            obj.transform.localScale = Vector3.one;
        }
    }

    // [변경] 플레이어 정보만 표시
    public void ShowPlayerStatusPopup(CharacterData player)
    {
        if (!statusPanel) return;
        statusPanel.SetActive(true);

        if (player == null)
        {
            if (statusText) statusText.text = "No Player Data";
            return;
        }

        string weaponName = (player.weaponStrategy != null) ? player.weaponStrategy.name : "Unarmed";

        string info = $"<size=150%><b>[PLAYER STATUS]</b></size>\n\n" +
                      $"Name: {player.characterName}\n" +
                      $"HP: {player.maxHp}\n" + // 현재 HP는 매니저에서 받아와야 정확하지만, 일단 데이터 기준 표시
                      $"Weapon: {weaponName}\n" +
                      $"Desc: {player.description}";

        if (statusText) statusText.text = info;
    }

    // [변경] 적 정보만 표시
    public void ShowEnemyStatusPopup(Enemy enemy)
    {
        if (!statusPanel) return;
        statusPanel.SetActive(true);

        string info = $"<size=150%><b>[ENEMY STATUS]</b></size>\n\n";

        if (enemy != null && enemy.data != null)
        {
            info += $"Name: {enemy.data.enemyName}\n" +
                    $"HP: {enemy.currentHp}/{enemy.data.maxHp}\n" +
                    $"ATK: {enemy.data.attackPower}\n" +
                    $"Desc: {enemy.data.description}";
        }
        else
        {
            info += "No Enemy Target";
        }

        if (statusText) statusText.text = info;
    }

    public void ClosePopup(GameObject popup)
    {
        popup.SetActive(false);
    }

    public void SetActiveQTE(bool isActive)
    {
        if (qtePanel) qtePanel.SetActive(isActive);
        if (qteDimmedBG) qteDimmedBG.gameObject.SetActive(isActive);
    }

    public void UpdateQTETimer(float ratio)
    {
        if (qteTimerBar) qteTimerBar.fillAmount = ratio;
    }

    public void SetupQTEPortraits(CharacterData sub1, bool sub1Alive, CharacterData sub2, bool sub2Alive)
    {
        if (qteLeftGroup) qteLeftGroup.SetActive(sub1Alive);
        if (qteLeftPortrait && sub1 != null) qteLeftPortrait.sprite = sub1.portrait;

        if (qteRightGroup) qteRightGroup.SetActive(sub2Alive);
        if (qteRightPortrait && sub2 != null) qteRightPortrait.sprite = sub2.portrait;
    }

    public IEnumerator AnimateReturnCard(CardData card)
    {
        GameObject realCardObj = Instantiate(cardPrefab, handArea);
        BulletCard realCard = realCardObj.GetComponent<BulletCard>();
        realCard.Setup(card);
        realCardObj.transform.localScale = Vector3.one;
        realCardObj.transform.localPosition = Vector3.zero;

        CanvasGroup cg = realCardObj.GetComponent<CanvasGroup>();
        if (!cg) cg = realCardObj.AddComponent<CanvasGroup>();
        cg.alpha = 0;

        yield return null;

        Transform p = CanvasTransform ? CanvasTransform : transform.root;
        GameObject fly = Instantiate(cardPrefab, p);
        fly.transform.position = cylinderPivot.position;
        fly.transform.localScale = Vector3.one;
        fly.GetComponent<BulletCard>().Setup(card);

        CanvasGroup fcg = fly.GetComponent<CanvasGroup>();
        if (!fcg) fcg = fly.AddComponent<CanvasGroup>();
        fcg.blocksRaycasts = false;

        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startPos = fly.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (realCardObj != null)
                fly.transform.position = Vector3.Lerp(startPos, realCardObj.transform.position, elapsed / duration);
            yield return null;
        }

        Destroy(fly);
        if (cg) cg.alpha = 1;
        LayoutRebuilder.ForceRebuildLayoutImmediate(handArea.GetComponent<RectTransform>());
    }
}