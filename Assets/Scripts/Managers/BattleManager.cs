using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    // ... (기존 변수 선언 동일) ...
    public enum BattleState { Ready, PlayerTurn, EnemyTurn, Win, Lose }
    public BattleState state;

    [Header("★ 캐릭터 설정")]
    public CharacterData mainCharacter;
    public CharacterData subCharacter1;
    public CharacterData subCharacter2;

    [Header("★ 현재 적")]
    public Enemy currentEnemy;

    [Header("오브젝트 연결")]
    public Transform cylinderPivot;
    public Transform handArea;
    public GameObject cardPrefab;
    public List<CylinderSlot> slots;
    private Transform canvasTransform;

    [Header("UI 연결")]
    public CharacterSlotUI mainCharUI;
    public CharacterSlotUI subChar1UI;
    public CharacterSlotUI subChar2UI;
    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI discardCountText;

    public Image characterPortraitImage;
    public TextMeshProUGUI characterNameText;

    [Header("결과창 UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("보상 시스템")]
    public RewardManager rewardManager;
    public int xpReward = 100;

    [Header("설정")]
    public float smoothTime = 0.05f;

    // 데이터 변수들
    private Dictionary<CharacterData, int> characterHpMap = new Dictionary<CharacterData, int>();
    private List<CharacterData> currentParty = new List<CharacterData>();
    private List<CardData> currentDeck = new List<CardData>();
    private List<CardData> currentDiscard = new List<CardData>();

    private CharacterData activeCharacter;
    private float targetAngle = 0f;
    private float currentVelocity;

    private bool isFiring = false;
    private bool isReloading = false;

    void Start()
    {
        state = BattleState.Ready;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) canvasTransform = canvas.transform;

        if (resultPanel != null) resultPanel.SetActive(false);

        if (mainCharacter != null)
        {
            RegisterCharacter(mainCharacter);
            if (subCharacter1 != null) RegisterCharacter(subCharacter1);
            if (subCharacter2 != null) RegisterCharacter(subCharacter2);

            activeCharacter = mainCharacter;
            InitializeGame();
        }
        else
        {
            Debug.LogError("Main Character가 설정되지 않았습니다!");
        }
    }

    void RegisterCharacter(CharacterData charData)
    {
        if (charData == null) return;
        currentParty.Add(charData);
        if (!characterHpMap.ContainsKey(charData))
        {
            characterHpMap[charData] = charData.maxHp;
        }
    }

    void Update()
    {
        float z = Mathf.SmoothDampAngle(cylinderPivot.eulerAngles.z, targetAngle, ref currentVelocity, smoothTime);
        cylinderPivot.rotation = Quaternion.Euler(0, 0, z);
    }

    public void SwapCharacter(CharacterData newCharacter)
    {
        if (state != BattleState.PlayerTurn && state != BattleState.Ready) return;
        if (newCharacter == null || newCharacter == activeCharacter) return;

        Debug.Log($"🔄 태그! {activeCharacter.characterName} -> {newCharacter.characterName}");
        activeCharacter = newCharacter;

        UpdateAllHpUI();

        if (characterPortraitImage) characterPortraitImage.sprite = activeCharacter.portrait;
        if (characterNameText) characterNameText.text = activeCharacter.characterName;
    }

    void InitializeGame()
    {
        UpdateAllHpUI();

        // [수정] 전투 시작 시 상태 완벽 초기화 (Clean Slate)
        // 1. 슬롯 비우기
        foreach (var slot in slots) slot.ClearSlot();

        // 2. 손패 비우기 (즉시 파괴하여 childCount를 0으로 만듦)
        // Destroy는 프레임 끝에 실행되므로, 루프를 돌며 DestroyImmediate를 쓰거나
        // 리스트에 담아두고 처리하는 것이 안전하지만, 여기서는 역순 루프로 처리
        for (int i = handArea.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(handArea.GetChild(i).gameObject);
        }

        // 3. 각도 초기화 (0도로 정렬)
        targetAngle = 0;
        cylinderPivot.rotation = Quaternion.Euler(0, 0, 0);

        // 4. 덱 재생성 (업데이트된 startingDeck 반영)
        currentDeck.Clear();
        currentDiscard.Clear();

        foreach (CharacterData member in currentParty)
        {
            if (member.startingDeck != null)
            {
                foreach (CardData card in member.startingDeck) currentDeck.Add(card);
            }
        }

        ShuffleDeck();
        UpdateDeckUI();

        // 첫 턴 시작
        state = BattleState.PlayerTurn;
        StartCoroutine(RefillHandRoutine());
    }

    // --- [보상 시스템] 카드 영구 획득 ---
    public void AddCardToDeck(CardData newCard)
    {
        // 1. 현재 전투 덱에 추가 (즉시 반영)
        currentDeck.Add(newCard);
        UpdateDeckUI();

        // [핵심 수정] 2. 캐릭터의 원본 덱(Starting Deck)에도 추가 (영구 저장)
        // (현재 활성화된 캐릭터에게 주거나, 공용 덱이 있다면 그곳에 추가)
        if (activeCharacter != null)
        {
            activeCharacter.startingDeck.Add(newCard);
            Debug.Log($"[System] {newCard.cardName} 카드가 {activeCharacter.characterName}의 덱에 영구 추가되었습니다.");
        }
        // 주의: 에디터 상의 CharacterData 파일이 실시간으로 수정됩니다. (게임 종료 후에도 유지됨)
    }

    // ... (ApplyDamageToEnemy, PlayerTakeDamage, HealPlayer, WinSequence, LoseSequence 등 기존 로직 동일) ...

    public void ApplyDamageToEnemy(int amount)
    {
        if (currentEnemy != null)
        {
            currentEnemy.TakeDamage(amount);
            if (currentEnemy.currentHp <= 0)
            {
                StartCoroutine(WinSequence());
            }
        }
    }

    public void PlayerTakeDamage(int amount)
    {
        int current = characterHpMap[activeCharacter];
        current = Mathf.Max(current - amount, 0);
        characterHpMap[activeCharacter] = current;

        UpdateAllHpUI();
        Debug.Log($"<color=red>{activeCharacter.characterName} -{amount} 피해!</color>");

        if (current <= 0)
        {
            Debug.Log("캐릭터 사망!");
            StartCoroutine(LoseSequence());
        }
    }

    public void HealPlayer(int amount)
    {
        int max = activeCharacter.maxHp;
        int current = characterHpMap[activeCharacter];
        current = Mathf.Min(current + amount, max);
        characterHpMap[activeCharacter] = current;
        UpdateAllHpUI();
        Debug.Log($"<color=green>{activeCharacter.characterName} +{amount} 회복!</color>");
    }

    IEnumerator WinSequence()
    {
        if (state == BattleState.Win) yield break;
        state = BattleState.Win;

        Debug.Log("🎉 VICTORY!");
        yield return new WaitForSeconds(1.0f);

        if (resultPanel)
        {
            resultPanel.SetActive(true);
            if (resultText) resultText.text = "<color=#00FF00>VICTORY</color>";
        }

        if (rewardManager != null)
        {
            rewardManager.AddXp(xpReward);
            yield return new WaitForSeconds(2.0f);

            if (rewardManager.pendingRewardCount > 0)
            {
                if (resultPanel) resultPanel.SetActive(false);
                rewardManager.ShowRewardPopup();
            }
            else
            {
                if (resultPanel) resultPanel.SetActive(false);
                StartNextBattle();
            }
        }
    }

    IEnumerator LoseSequence()
    {
        if (state == BattleState.Lose) yield break;
        state = BattleState.Lose;

        Debug.Log("💀 DEFEAT...");
        yield return new WaitForSeconds(1.0f);

        if (resultPanel)
        {
            resultPanel.SetActive(true);
            if (resultText) resultText.text = "<color=#FF0000>DEFEAT</color>";
        }
    }

    public void StartNextBattle()
    {
        StartCoroutine(NextBattleRoutine());
    }

    IEnumerator NextBattleRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        // 적 리셋
        if (currentEnemy != null)
        {
            currentEnemy.gameObject.SetActive(true);
            if (currentEnemy.data != null) currentEnemy.Setup(currentEnemy.data);
        }

        // 게임 상태 리셋 및 재시작
        state = BattleState.PlayerTurn;
        InitializeGame(); // 여기서 초기화(탄창 비우기, 덱 리필 등) 수행됨

        Debug.Log("=== 다음 전투 시작 ===");
    }

    public void OnClick_Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateAllHpUI()
    {
        if (mainCharUI != null) mainCharUI.Setup(activeCharacter, characterHpMap[activeCharacter]);

        int sub1Hp = (subCharacter1 != null && characterHpMap.ContainsKey(subCharacter1)) ? characterHpMap[subCharacter1] : 0;
        if (subChar1UI != null) subChar1UI.Setup(subCharacter1, sub1Hp);

        int sub2Hp = (subCharacter2 != null && characterHpMap.ContainsKey(subCharacter2)) ? characterHpMap[subCharacter2] : 0;
        if (subChar2UI != null) subChar2UI.Setup(subCharacter2, sub2Hp);
    }

    void UpdateDeckUI()
    {
        if (deckCountText) deckCountText.text = currentDeck.Count.ToString();
        if (discardCountText) discardCountText.text = currentDiscard.Count.ToString();
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < currentDeck.Count; i++)
        {
            CardData temp = currentDeck[i];
            int randomIndex = Random.Range(i, currentDeck.Count);
            currentDeck[i] = currentDeck[randomIndex];
            currentDeck[randomIndex] = temp;
        }
    }

    CardData DrawCard()
    {
        if (currentDeck.Count <= 0)
        {
            if (currentDiscard.Count <= 0) return null;
            currentDeck.AddRange(currentDiscard);
            currentDiscard.Clear();
            ShuffleDeck();
        }
        CardData card = currentDeck[0];
        currentDeck.RemoveAt(0);
        return card;
    }

    public void DiscardCard(CardData card)
    {
        if (card != null)
        {
            currentDiscard.Add(card);
            UpdateDeckUI();
        }
    }

    public void ReturnCardToHand(CardData card)
    {
        StartCoroutine(AnimateReturnCard(card));
    }

    public void OnClick_Fire()
    {
        if (state != BattleState.PlayerTurn) return;
        if (!isFiring && !isReloading) StartCoroutine(FireRoutine());
    }

    public void OnClick_Reload()
    {
        if (state != BattleState.PlayerTurn) return;
        if (isFiring || isReloading) return;
        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        foreach (var slot in slots)
        {
            if (slot.isLoaded)
            {
                DiscardCard(slot.loadedCard);
                slot.ClearSlot();
            }
        }

        foreach (Transform child in handArea)
        {
            BulletCard card = child.GetComponent<BulletCard>();
            if (card) DiscardCard(card.cardData);
            Destroy(child.gameObject);
        }

        yield return StartCoroutine(AlignCylinderRoutine());

        yield return StartCoroutine(RefillHandRoutine());
        isReloading = false;
    }

    IEnumerator FireRoutine()
    {
        isFiring = true;

        if (state == BattleState.Win || state == BattleState.Lose) yield break;

        for (int i = 0; i < 6; i++)
        {
            targetAngle = i * 60f;
            while (Mathf.Abs(Mathf.DeltaAngle(cylinderPivot.eulerAngles.z, targetAngle)) > 1.0f) yield return null;
            yield return new WaitForSeconds(0.02f);

            CylinderSlot currentSlot = slots[i];
            if (currentSlot.isLoaded)
            {
                Debug.Log($"=== [발사] {currentSlot.loadedCard.cardName} (By {activeCharacter.characterName}) ===");

                if (currentSlot.loadedCard.actions != null)
                {
                    foreach (var action in currentSlot.loadedCard.actions)
                    {
                        if (action.effectLogic != null) action.effectLogic.OnUse(this, action.value);
                    }
                }

                currentSlot.PlayFireEffect();
                DiscardCard(currentSlot.loadedCard);
                currentSlot.ClearSlot();

                if (state == BattleState.Win) break;
                yield return new WaitForSeconds(0.15f);
            }
            else
            {
                yield return new WaitForSeconds(0.05f);
            }
        }

        isFiring = false;

        if (state != BattleState.Win && state != BattleState.Lose)
        {
            yield return StartCoroutine(AlignCylinderRoutine());
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    IEnumerator AlignCylinderRoutine()
    {
        float startAngle = targetAngle;
        float endAngle = Mathf.Ceil(startAngle / 360f) * 360f;
        if (endAngle <= startAngle) endAngle += 360f;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentZ = Mathf.Lerp(startAngle, endAngle, 1 - Mathf.Pow(1 - t, 4));

            cylinderPivot.rotation = Quaternion.Euler(0, 0, currentZ);
            yield return null;
        }

        cylinderPivot.rotation = Quaternion.Euler(0, 0, 0);
        targetAngle = 0;
    }

    IEnumerator EnemyTurnRoutine()
    {
        state = BattleState.EnemyTurn;
        Debug.Log("=== 적 턴 시작 ===");
        yield return new WaitForSeconds(0.5f);

        if (currentEnemy != null && currentEnemy.gameObject.activeSelf)
        {
            currentEnemy.DoAttack();
        }
        else
        {
            EndEnemyTurn();
        }
    }

    public void EndEnemyTurn()
    {
        if (state == BattleState.Lose || state == BattleState.Win) return;

        state = BattleState.PlayerTurn;
        Debug.Log("=== 플레이어 턴 시작 ===");
        StartCoroutine(RefillHandRoutine());
    }

    IEnumerator RefillHandRoutine()
    {
        int currentHandCount = handArea.childCount;
        int cardsToDraw = 5 - currentHandCount;

        for (int i = 0; i < cardsToDraw; i++)
        {
            CardData drawnCard = DrawCard();
            if (drawnCard != null)
            {
                StartCoroutine(AnimateDrawCard(drawnCard));
                yield return new WaitForSeconds(0.2f);
            }
        }
        UpdateDeckUI();
    }

    IEnumerator AnimateDrawCard(CardData card)
    {
        GameObject realCardObj = Instantiate(cardPrefab, handArea);
        BulletCard realCard = realCardObj.GetComponent<BulletCard>();
        realCard.Setup(card);
        realCardObj.transform.localScale = Vector3.one;
        realCardObj.transform.localPosition = Vector3.zero;

        CanvasGroup realCG = realCardObj.GetComponent<CanvasGroup>();
        if (realCG == null) realCG = realCardObj.AddComponent<CanvasGroup>();
        realCG.alpha = 0;
        yield return null;

        Transform p = canvasTransform ? canvasTransform : transform.root;
        GameObject flyingCard = Instantiate(cardPrefab, p);
        Vector3 spawnPos = deckCountText != null ? deckCountText.transform.position : new Vector3(-800, -400, 0);
        flyingCard.transform.position = spawnPos;
        flyingCard.transform.localScale = Vector3.one;

        BulletCard flyingScript = flyingCard.GetComponent<BulletCard>();
        flyingScript.Setup(card);
        CanvasGroup flyingCG = flyingCard.GetComponent<CanvasGroup>();
        if (flyingCG == null) flyingCG = flyingCard.AddComponent<CanvasGroup>();
        flyingCG.blocksRaycasts = false;

        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startPos = flyingCard.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (realCardObj != null)
            {
                flyingCard.transform.position = Vector3.Lerp(startPos, realCardObj.transform.position, elapsed / duration);
            }
            yield return null;
        }

        Destroy(flyingCard);
        if (realCG != null) realCG.alpha = 1;
        LayoutRebuilder.ForceRebuildLayoutImmediate(handArea.GetComponent<RectTransform>());
    }

    IEnumerator AnimateReturnCard(CardData card)
    {
        GameObject realCardObj = Instantiate(cardPrefab, handArea);
        BulletCard realCard = realCardObj.GetComponent<BulletCard>();
        realCard.Setup(card);
        realCardObj.transform.localScale = Vector3.one;
        realCardObj.transform.localPosition = Vector3.zero;

        CanvasGroup cg = realCardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = realCardObj.AddComponent<CanvasGroup>();
        cg.alpha = 0;

        yield return null;

        Transform p = canvasTransform ? canvasTransform : transform.root;
        GameObject fly = Instantiate(cardPrefab, p);
        fly.transform.position = cylinderPivot.position;
        fly.transform.localScale = Vector3.one;
        fly.GetComponent<BulletCard>().Setup(card);
        CanvasGroup fcg = fly.GetComponent<CanvasGroup>();
        if (!fcg) fcg = fly.AddComponent<CanvasGroup>(); fcg.blocksRaycasts = false;

        float d = 0.2f, e = 0f;
        Vector3 s = fly.transform.position;
        while (e < d) { e += Time.deltaTime; if (realCardObj) fly.transform.position = Vector3.Lerp(s, realCardObj.transform.position, e / d); yield return null; }
        Destroy(fly); if (cg) cg.alpha = 1;
        LayoutRebuilder.ForceRebuildLayoutImmediate(handArea.GetComponent<RectTransform>());
    }
}