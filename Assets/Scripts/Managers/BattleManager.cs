using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    public event System.Action OnCharacterChanged;

    #region [FSM : 상태 머신]
    public BattleState currentState;

    public State_Start stateStart;
    public State_PlayerTurn statePlayerTurn;
    public State_EnemyTurn stateEnemyTurn;
    public State_Resolution stateResolution;
    public State_QTE_Slow stateQTE;
    public State_LevelUp stateLevelUp;
    public State_End stateEnd;
    #endregion

    [Header("★ 핵심 매니저 연결")]
    public BattleUIManager uiManager;
    public RewardManager rewardManager;

    [Header("★ 아군 데이터 (Party)")]
    public CharacterData mainCharacter;
    public List<CharacterData> startingSubCharacters;

    [Header("★ 적군 설정 (Enemy Squad)")]
    public List<EnemyData> enemySpawnList;
    public List<Transform> enemySpawnPoints;
    public GameObject enemyPrefab;

    public List<Enemy> spawnedEnemies = new List<Enemy>();

    public Enemy currentEnemy
    {
        get
        {
            if (spawnedEnemies != null)
            {
                spawnedEnemies.RemoveAll(e => e == null);
                return spawnedEnemies.Find(e => e.gameObject.activeSelf && e.currentHp > 0);
            }
            return null;
        }
    }

    [Header("★ 자원 설정")]
    public int maxSP = 3;
    public int currentSP = 3;
    public int swapCost = 1;
    public int spRecoverPerTurn = 1;

    [HideInInspector] public Dictionary<CharacterData, int> characterHpMap = new Dictionary<CharacterData, int>();
    [HideInInspector] public List<CharacterData> currentParty = new List<CharacterData>();
    public List<CharacterData> subCharacters = new List<CharacterData>();

    [HideInInspector] public List<CardData> currentDeck = new List<CardData>();
    [HideInInspector] public CharacterSkill currentQTESkill = null;
    [HideInInspector] public List<CardData> currentDiscard = new List<CardData>();
    [HideInInspector] public CharacterData activeCharacter;
    [HideInInspector] public bool isEffectRunning = false;

    #region [Bridge Properties]
    public Transform cylinderPivot => uiManager.cylinderPivot;
    public Transform handArea => uiManager.handArea;
    public GameObject cardPrefab => uiManager.cardPrefab;
    public List<CylinderSlot> slots => uiManager.slots;

    public GameObject qtePanel => uiManager.qtePanel;
    public UnityEngine.UI.Image qteDimmedBG => uiManager.qteDimmedBG;
    public UnityEngine.UI.Image qteTimerBar => uiManager.qteTimerBar;

    public GameObject resultPanel => uiManager.resultPanel;
    public TMPro.TextMeshProUGUI resultText => uiManager.resultText;
    #endregion

    void Start()
    {
        InitializeManagers();
        CheckDataConnections();
        InitializeStates();
        InitializeParty();
        SpawnEnemies();

        currentSP = maxSP;
        UpdateSPUI();
        UpdateAllHpUI();

        ChangeState(stateStart);
    }

    void SpawnEnemies()
    {
        if (spawnedEnemies != null)
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] != null && spawnedEnemies[i].gameObject != null)
                {
                    Destroy(spawnedEnemies[i].gameObject);
                }
            }
            spawnedEnemies.Clear();
        }
        else
        {
            spawnedEnemies = new List<Enemy>();
        }

        if (enemySpawnList != null && enemySpawnPoints != null)
        {
            for (int i = 0; i < enemySpawnList.Count; i++)
            {
                if (i >= enemySpawnPoints.Count) break;
                if (enemySpawnList[i] == null) continue;

                if (enemyPrefab == null)
                {
                    Debug.LogError("⛔ [Error] 적 프리팹(Enemy Prefab)이 연결되지 않았습니다!");
                    return;
                }

                GameObject obj = Instantiate(enemyPrefab, enemySpawnPoints[i]);
                obj.SetActive(true);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;

                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero;
                }

                Enemy enemyScript = obj.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.Setup(enemySpawnList[i]);
                    spawnedEnemies.Add(enemyScript);
                }
                else
                {
                    Destroy(obj);
                }
            }
        }

        if (spawnedEnemies.Count == 0)
        {
            var existingEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            spawnedEnemies.AddRange(existingEnemies);
        }
    }

    void CheckDataConnections()
    {
        if (startingSubCharacters == null || startingSubCharacters.Count == 0)
            Debug.LogWarning("⚠️ [Check] 서브 캐릭터 리스트가 비어있습니다.");

        if (enemySpawnList == null || enemySpawnList.Count == 0)
            Debug.LogWarning("⚠️ [Check] 적 스폰 리스트(Enemy Spawn List)가 비어있습니다.");
    }

    void Update()
    {
        if (currentState != null) currentState.Execute();
    }

    void InitializeManagers()
    {
        if (uiManager == null) uiManager = GetComponent<BattleUIManager>();
        if (uiManager == null) uiManager = FindFirstObjectByType<BattleUIManager>();
    }

    void InitializeStates()
    {
        stateStart = new State_Start(this);
        statePlayerTurn = new State_PlayerTurn(this);
        stateEnemyTurn = new State_EnemyTurn(this);
        stateResolution = new State_Resolution(this);
        stateQTE = new State_QTE_Slow(this);
        stateLevelUp = new State_LevelUp(this);
        stateEnd = new State_End(this);
    }

    void InitializeParty()
    {
        if (mainCharacter)
        {
            RegisterOne(mainCharacter);
            activeCharacter = mainCharacter;
        }

        subCharacters.Clear();
        if (startingSubCharacters != null)
        {
            foreach (var sub in startingSubCharacters)
            {
                if (sub != null)
                {
                    subCharacters.Add(sub);
                    RegisterOne(sub);
                }
            }
        }
    }

    void RegisterOne(CharacterData d)
    {
        if (!currentParty.Contains(d)) currentParty.Add(d);
        if (!characterHpMap.ContainsKey(d)) characterHpMap[d] = d.maxHp;
    }

    public void ChangeState(BattleState newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;
        if (currentState != null) currentState.Enter();
    }

    #region [Input Events]
    public void OnClick_Fire()
    {
        if (currentState == statePlayerTurn) ChangeState(stateResolution);
    }

    public void OnClick_Reload()
    {
        if (currentState == statePlayerTurn) statePlayerTurn.OnReload();
    }

    public void OnClick_Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClick_ShowGraveyard()
    {
        if (uiManager) uiManager.ShowGraveyardPopup(currentDiscard);
    }

    public void OnClick_ShowPlayerStatus()
    {
        if (uiManager) uiManager.ShowPlayerStatusPopup(activeCharacter);
    }

    public void OnClick_ShowEnemyStatus()
    {
        if (uiManager) uiManager.ShowEnemyStatusPopup(currentEnemy);
    }

    public void OnClick_SubCharacter(CharacterData targetChar)
    {
        if (targetChar == activeCharacter) return;
        if (currentState == stateQTE) return;

        if (currentSP >= swapCost)
        {
            currentSP -= swapCost;
            SwapCharacter(targetChar);
        }
        else
        {
            Debug.Log($"❌ SP 부족 ({currentSP}/{swapCost})");
        }
    }
    #endregion

    #region [Game Logic]
    public void SwapCharacter(CharacterData newChar)
    {
        if (newChar == null || newChar == activeCharacter) return;

        CharacterData prevMain = activeCharacter;
        int index = subCharacters.IndexOf(newChar);

        if (index != -1)
        {
            subCharacters[index] = prevMain;
            activeCharacter = newChar;
            mainCharacter = newChar;

            UpdateAllHpUI();
            UpdateSPUI();
            OnCharacterChanged?.Invoke();

            Debug.Log($"✅ 태그 완료: {prevMain.characterName} ↔ {activeCharacter.characterName}");
        }
        else
        {
            Debug.LogError($"⛔ 데이터 불일치: {newChar.characterName}가 서브 리스트에 없습니다.");
        }
    }

    public void RecoverSP()
    {
        currentSP = Mathf.Min(currentSP + spRecoverPerTurn, maxSP);
        UpdateSPUI();
    }

    public void PlayerTakeDamage(int amount)
    {
        if (activeCharacter == null) return;

        int current = characterHpMap[activeCharacter];
        current = Mathf.Max(current - amount, 0);
        characterHpMap[activeCharacter] = current;

        // ★ [Fix] 플레이어 데미지 팝업 연동 수정
        if (DamagePopupManager.Instance != null && uiManager != null && uiManager.mainCharUI != null)
        {
            // 초상화 이미지가 있으면 거기를 타겟으로, 없으면 전체 UI를 타겟으로
            Transform targetTransform = (uiManager.mainCharUI.portraitImage != null)
                                      ? uiManager.mainCharUI.portraitImage.transform
                                      : uiManager.mainCharUI.transform;

            DamagePopupManager.Instance.SpawnAtTransform(targetTransform, amount, false, true);
        }

        UpdateAllHpUI();

        if (current <= 0)
        {
            stateEnd.SetResult(false);
            ChangeState(stateEnd);
        }
    }

    public void ApplyDamageToEnemy(int amount)
    {
        Enemy target = currentEnemy;

        if (target != null)
        {
            target.TakeDamage(amount);

            bool allDead = true;
            foreach (var e in spawnedEnemies)
            {
                if (e != null && e.gameObject != null && e.currentHp > 0)
                {
                    allDead = false;
                    break;
                }
            }

            if (allDead)
            {
                stateEnd.SetResult(true);
                ChangeState(stateEnd);
            }
        }
    }

    public void HealPlayer(int amount)
    {
        int max = activeCharacter.maxHp;
        int current = characterHpMap[activeCharacter];
        current = Mathf.Min(current + amount, max);
        characterHpMap[activeCharacter] = current;

        // ★ [Fix] 힐 팝업 연동 수정
        if (DamagePopupManager.Instance != null && uiManager != null && uiManager.mainCharUI != null)
        {
            // 초상화 이미지가 있으면 거기를 타겟으로 (데미지와 동일 로직)
            Transform targetTransform = (uiManager.mainCharUI.portraitImage != null)
                                      ? uiManager.mainCharUI.portraitImage.transform
                                      : uiManager.mainCharUI.transform;

            DamagePopupManager.Instance.SpawnHeal(targetTransform, amount);
        }

        UpdateAllHpUI();
    }

    public void AddCardToDeck(CardData newCard)
    {
        currentDeck.Add(newCard);
        UpdateDeckUI();
        if (activeCharacter != null) activeCharacter.startingDeck.Add(newCard);
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
        if (uiManager) StartCoroutine(uiManager.AnimateReturnCard(card));
    }

    public void StartNextBattle()
    {
        StartCoroutine(NextBattleRoutine());
    }

    IEnumerator NextBattleRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        SpawnEnemies();
        ChangeState(stateStart);
    }
    #endregion

    #region [UI Updates]
    public void UpdateSPUI()
    {
        if (uiManager)
        {
            uiManager.UpdateSP(currentSP, maxSP);
            uiManager.UpdateSlotInteractability(currentSP, swapCost, activeCharacter);
        }
    }

    public void UpdateAllHpUI()
    {
        if (uiManager)
            uiManager.UpdateAllHpUI(activeCharacter, subCharacters, characterHpMap);
    }

    public void UpdateDeckUI()
    {
        if (uiManager)
            uiManager.UpdateDeckCount(currentDeck.Count, currentDiscard.Count);
    }
    #endregion
}