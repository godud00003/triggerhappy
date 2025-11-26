using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RewardManager : MonoBehaviour
{
    [Header("데이터 연결")]
    public List<CardData> allCardPool;

    [Header("UI 연결")]
    public GameObject rewardPanel;
    public Transform cardContainer;
    public GameObject cardSelectPrefab;
    public TextMeshProUGUI remainingText;

    public int teamLevel = 1;
    public int currentXp = 0;
    public int maxXp = 100;

    public int pendingRewardCount = 0;

    void Start()
    {
        if (rewardPanel != null) rewardPanel.SetActive(false);
        pendingRewardCount = 0;
    }

    public void AddXp(int amount)
    {
        currentXp += amount;
        CheckLevelUp();
    }

    void CheckLevelUp()
    {
        while (currentXp >= maxXp)
        {
            currentXp -= maxXp;
            teamLevel++;
            maxXp = Mathf.RoundToInt(maxXp * 1.2f);

            pendingRewardCount++;
            Debug.Log($"🎉 LEVEL UP! Lv.{teamLevel} (보상 대기: {pendingRewardCount})");
        }
    }

    public void ShowRewardPopup()
    {
        if (pendingRewardCount <= 0) return;

        rewardPanel.SetActive(true);
        UpdateRemainingText();
        GenerateRandomCards();
    }

    void UpdateRemainingText()
    {
        if (remainingText) remainingText.text = $"남은 선택 기회: {pendingRewardCount}";
    }

    void GenerateRandomCards()
    {
        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        for (int i = 0; i < 3; i++)
        {
            if (allCardPool.Count == 0) break;

            CardData randomCard = allCardPool[Random.Range(0, allCardPool.Count)];

            GameObject cardObj = Instantiate(cardSelectPrefab, cardContainer);

            BulletCard uiScript = cardObj.GetComponent<BulletCard>();
            if (uiScript) uiScript.Setup(randomCard);

            Button btn = cardObj.GetComponent<Button>();
            if (btn == null) btn = cardObj.AddComponent<Button>();

            btn.onClick.AddListener(() => OnSelectCard(randomCard));
        }
    }

    void OnSelectCard(CardData selected)
    {
        Debug.Log($"선택함: {selected.cardName}");

        // 덱에 카드 추가 (BattleManager를 통해)
        FindAnyObjectByType<BattleManager>().AddCardToDeck(selected);

        pendingRewardCount--;

        if (pendingRewardCount > 0)
        {
            UpdateRemainingText();
            GenerateRandomCards();
        }
        else
        {
            rewardPanel.SetActive(false);
            Debug.Log("모든 보상 수령 완료. 다음 전투로 이동합니다.");

            // [추가] 다음 전투 시작 요청
            FindAnyObjectByType<BattleManager>().StartNextBattle();
        }
    }
}