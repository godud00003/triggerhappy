using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq; // 리스트 제어용 (Find 등)

public class BattleManager : MonoBehaviour
{
    public event System.Action OnCharacterChanged;

    #region [FSM : 상태 머신]
    public BattleState currentState;

    // 상태 인스턴스들
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
    public CharacterData mainCharacter;               // 시작 시 메인 캐릭터
    public List<CharacterData> startingSubCharacters; // 시작 시 서브 캐릭터 목록

    [Header("★ 적군 설정 (Enemy Squad)")]
    // 인스펙터에서 설정할 적 등장 목록 (웨이브/엔트리)
    public List<EnemyData> enemySpawnList;
    // 적들이 배치될 위치 목록 (화면 배치용)
    public List<Transform> enemySpawnPoints;
    // 적 생성에 사용할 프리팹
    public GameObject enemyPrefab;

    // [핵심] 실제 필드에 소환된 적 리스트 (Enemy.cs에서 접근함)
    public List<Enemy> spawnedEnemies = new List<Enemy>();

    // [호환성] 기존 코드(State 등)가 'currentEnemy'를 찾을 때, 자동으로 '타겟'을 반환해줌
    public Enemy currentEnemy
    {
        get
        {
            // 리스트에서 null이거나 파괴된 객체 제거 (청소)
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

    // [런타임 데이터]
    [HideInInspector] public Dictionary<CharacterData, int> characterHpMap = new Dictionary<CharacterData, int>();
    [HideInInspector] public List<CharacterData> currentParty = new List<CharacterData>();
    // 런타임 서브 캐릭터 리스트 (교체 시 여기 있는 애들과 바꿈)
    public List<CharacterData> subCharacters = new List<CharacterData>();

    [HideInInspector] public List<CardData> currentDeck = new List<CardData>();
    [HideInInspector] public List<CardData> currentDiscard = new List<CardData>();
    [HideInInspector] public CharacterData activeCharacter;

    #region [Bridge Properties] (UI 매니저 연결용)
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

        // 데이터 연결 상태 체크 (누락 시 경고 로그 출력)
        CheckDataConnections();

        InitializeStates();

        // 파티 및 적 초기화
        InitializeParty();
        SpawnEnemies();

        // 초기 자원 및 UI 갱신
        currentSP = maxSP;
        UpdateSPUI();
        UpdateAllHpUI();

        // 게임 시작 상태로 진입
        ChangeState(stateStart);
    }

    // [New] 적 소환 로직 (리스트 기반 + 안전 삭제 추가)
    void SpawnEnemies()
    {
        // 1. 기존 적 리스트 정리 (안전하게 삭제)
        if (spawnedEnemies != null)
        {
            // 역순으로 돌면서 삭제해야 인덱스 오류 안 남
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

        // 2. 리스트 기반 소환
        if (enemySpawnList != null && enemySpawnPoints != null)
        {
            for (int i = 0; i < enemySpawnList.Count; i++)
            {
                // 위치가 부족하면 중단
                if (i >= enemySpawnPoints.Count) break;
                if (enemySpawnList[i] == null) continue;

                // 프리팹 체크
                if (enemyPrefab == null)
                {
                    Debug.LogError("⛔ [Error] 적 프리팹(Enemy Prefab)이 연결되지 않았습니다!");
                    return;
                }

                // 3. 프리팹 생성
                GameObject obj = Instantiate(enemyPrefab, enemySpawnPoints[i]);

                // [안전장치] 생성 직후 스케일과 위치를 강제로 초기화 (UI 꼬임 방지)
                obj.SetActive(true);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;

                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero; // 부모 크기에 맞춤 (필요시 수정)
                }

                // 4. 데이터 주입 및 리스트 등록
                Enemy enemyScript = obj.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.Setup(enemySpawnList[i]);
                    spawnedEnemies.Add(enemyScript);
                }
                else
                {
                    Debug.LogError("⛔ [Error] 소환된 프리팹에 Enemy 스크립트가 없습니다!");
                    Destroy(obj); // 스크립트 없는 껍데기는 삭제
                }
            }
        }

        // 만약 인스펙터 설정 없이 씬에 미리 배치된 적이 있다면 리스트에 추가 (안전장치)
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
        // 메인 캐릭터 등록
        if (mainCharacter)
        {
            RegisterOne(mainCharacter);
            activeCharacter = mainCharacter;
        }

        // 서브 캐릭터 리스트 등록
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

    #region [Input Events] UI 버튼 연결용
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
        // 현재 타겟팅된(살아있는 첫번째) 적 정보 표시
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

    #region [Game Logic] 핵심 로직
    public void SwapCharacter(CharacterData newChar)
    {
        if (newChar == null || newChar == activeCharacter) return;

        CharacterData prevMain = activeCharacter;
        int index = subCharacters.IndexOf(newChar);

        if (index != -1)
        {
            // 자리 교체 (Swap)
            subCharacters[index] = prevMain;
            activeCharacter = newChar;
            mainCharacter = newChar;

            // UI 및 데이터 갱신
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

        UpdateAllHpUI();

        if (current <= 0)
        {
            stateEnd.SetResult(false);
            ChangeState(stateEnd);
        }
    }

    public void ApplyDamageToEnemy(int amount)
    {
        // [타겟팅] 현재 살아있는 첫 번째 적을 공격
        Enemy target = currentEnemy;

        if (target != null)
        {
            target.TakeDamage(amount);

            // 모든 적이 죽었는지 체크 (전멸 여부)
            bool allDead = true;

            // null 체크 강화
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
        // 적 재소환
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