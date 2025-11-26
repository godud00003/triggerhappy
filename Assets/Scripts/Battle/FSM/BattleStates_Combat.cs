using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ==========================================
// 실제 전투 로직을 담당하는 상태들 (Combat)
// PlayerTurn -> Resolution <-> QTE -> EnemyTurn
// ==========================================

// -------------------------------------------------------------------------
// 4. PlayerTurn: 플레이어의 입력 대기 및 턴 시작 처리
// -------------------------------------------------------------------------
public class State_PlayerTurn : BattleState
{
    public State_PlayerTurn(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log(">>> [State] PlayerTurn 시작");

        // 턴 시작 시 SP(교체 포인트) 회복
        manager.RecoverSP();

        // 손패 보충 시작
        manager.StartCoroutine(RefillHand());
    }

    // 재장전 버튼 클릭 시 호출
    public void OnReload()
    {
        manager.StartCoroutine(ReloadRoutine());
    }

    IEnumerator RefillHand()
    {
        // 손패가 5장 될 때까지 드로우 (하드코딩 대신 변수화 권장)
        while (manager.handArea.childCount < 5)
        {
            // 덱이 비었으면 버린 카드 섞어 넣기
            if (manager.currentDeck.Count == 0 && manager.currentDiscard.Count > 0)
            {
                manager.currentDeck.AddRange(manager.currentDiscard);
                manager.currentDiscard.Clear();
                ShuffleDeck();
            }

            // 드로우
            if (manager.currentDeck.Count > 0)
            {
                CardData card = manager.currentDeck[0];
                manager.currentDeck.RemoveAt(0);

                GameObject obj = Object.Instantiate(manager.cardPrefab, manager.handArea);
                obj.GetComponent<BulletCard>().Setup(card);
            }
            yield return new WaitForSeconds(0.1f);
        }
        manager.UpdateDeckUI();
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < manager.currentDeck.Count; i++)
        {
            CardData temp = manager.currentDeck[i];
            int r = Random.Range(i, manager.currentDeck.Count);
            manager.currentDeck[i] = manager.currentDeck[r];
            manager.currentDeck[r] = temp;
        }
    }

    IEnumerator ReloadRoutine()
    {
        // 실린더 슬롯 비우기
        foreach (var slot in manager.slots)
        {
            if (slot.isLoaded) manager.DiscardCard(slot.loadedCard);
            slot.ClearSlot();
        }

        // 현재 손패 비우기
        foreach (Transform child in manager.handArea)
        {
            BulletCard card = child.GetComponent<BulletCard>();
            if (card) manager.DiscardCard(card.cardData);
            Object.Destroy(child.gameObject);
        }

        yield return new WaitForSeconds(0.5f);
        yield return manager.StartCoroutine(RefillHand());
    }
}

// -------------------------------------------------------------------------
// 5. Resolution: 발사 시퀀스 및 결과 처리
// -------------------------------------------------------------------------
public class State_Resolution : BattleState
{
    public State_Resolution(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        manager.StartCoroutine(FireSequence());
    }

    IEnumerator FireSequence()
    {
        // 슬롯 개수에 맞춰서 안전하게 순회
        int capacity = manager.slots.Count;

        if (capacity == 0)
        {
            Debug.LogError("⛔ [Error] 슬롯이 없습니다! BattleUIManager 설정을 확인하세요.");
            manager.ChangeState(manager.stateEnemyTurn);
            yield break;
        }

        float angleStep = 360f / capacity;

        for (int i = 0; i < capacity; i++)
        {
            // 1. 회전 연출
            float targetZ = i * angleStep;
            if (manager.cylinderPivot)
                manager.cylinderPivot.rotation = Quaternion.Euler(0, 0, targetZ);

            yield return new WaitForSeconds(0.15f);

            CylinderSlot slot = manager.slots[i];

            if (slot.isLoaded)
            {
                // 2. QTE 트리거 체크
                // (임시 조건: 카드 이름에 "QTE" 포함 시)
                if (slot.loadedCard.cardName.Contains("QTE"))
                {
                    Debug.Log("⚡ QTE Triggered!");
                    manager.ChangeState(manager.stateQTE);
                    yield break; // 발사 중단하고 QTE 상태로 전환
                }

                // 3. 카드 효과 발동
                if (slot.loadedCard.actions != null)
                {
                    foreach (var act in slot.loadedCard.actions)
                    {
                        if (act.effectLogic) act.effectLogic.OnUse(manager, act.value);
                    }
                }

                // 4. 발사 이펙트 및 정리
                slot.PlayFireEffect();
                manager.DiscardCard(slot.loadedCard);
                slot.ClearSlot();

                // 적 사망 체크
                if (manager.currentEnemy == null || manager.currentEnemy.currentHp <= 0)
                {
                    yield break; // 승리 처리는 BattleManager.ApplyDamageToEnemy에서 함
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

        // 모든 발사가 끝나면 적 턴으로 이동
        manager.ChangeState(manager.stateEnemyTurn);
    }
}

// -------------------------------------------------------------------------
// 6. QTE_Slow: 시간 감속 및 태그 (ZZZ 스타일)
// -------------------------------------------------------------------------
public class State_QTE_Slow : BattleState
{
    private float qteDuration = 3.0f; // 제한 시간
    private float currentTimer;

    public State_QTE_Slow(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("⚡ [State] QTE Time Start (Slow Motion)");

        // 1. 시간 감속 (매트릭스 효과)
        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        currentTimer = qteDuration;

        // 2. UI 활성화 및 초상화 세팅
        if (manager.uiManager)
        {
            manager.uiManager.SetActiveQTE(true);

            // [변경] 리스트 기반으로 서브 캐릭터 가져오기
            // 0번 인덱스 = 좌클릭 대상, 1번 인덱스 = 우클릭 대상
            CharacterData s1 = GetSubChar(0);
            CharacterData s2 = GetSubChar(1);

            manager.uiManager.SetupQTEPortraits(
                s1, IsAlive(s1),
                s2, IsAlive(s2)
            );
        }
    }

    // 헬퍼: 리스트 인덱스 안전 접근
    CharacterData GetSubChar(int index)
    {
        if (manager.subCharacters != null && manager.subCharacters.Count > index)
        {
            return manager.subCharacters[index];
        }
        return null;
    }

    // 헬퍼: 캐릭터 생존 여부 확인
    bool IsAlive(CharacterData ch)
    {
        return ch != null &&
               manager.characterHpMap.ContainsKey(ch) &&
               manager.characterHpMap[ch] > 0;
    }

    public override void Execute()
    {
        // 1. 타이머 감소 (UnscaledDeltaTime 사용 필수)
        currentTimer -= Time.unscaledDeltaTime;

        if (manager.uiManager)
            manager.uiManager.UpdateQTETimer(currentTimer / qteDuration);

        // 2. 시간 초과 체크
        if (currentTimer <= 0)
        {
            Debug.Log("⏰ QTE 시간 초과! (교체 없이 진행)");
            manager.ChangeState(manager.stateResolution);
            return;
        }

        // 3. 입력 감지 (좌/우 클릭)
        // [변경] 고정 변수 대신 리스트 인덱스로 접근
        if (Input.GetMouseButtonDown(0))
        {
            TryTag(GetSubChar(0)); // 좌클릭 -> 리스트 0번 서브
        }
        else if (Input.GetMouseButtonDown(1))
        {
            TryTag(GetSubChar(1)); // 우클릭 -> 리스트 1번 서브
        }
    }

    void TryTag(CharacterData nextChar)
    {
        if (IsAlive(nextChar))
        {
            manager.SwapCharacter(nextChar);
            // [연출] 여기에 카메라 줌인이나 특수 효과 추가 가능

            // 태그 후 다시 발사 시퀀스로 복귀
            manager.ChangeState(manager.stateResolution);
        }
    }

    public override void Exit()
    {
        // 시간 및 UI 원상 복구
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        if (manager.uiManager)
            manager.uiManager.SetActiveQTE(false);
    }
}

// -------------------------------------------------------------------------
// 7. EnemyTurn: 적 공격 턴
// -------------------------------------------------------------------------
public class State_EnemyTurn : BattleState
{
    public State_EnemyTurn(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        manager.StartCoroutine(EnemyRoutine());
    }

    IEnumerator EnemyRoutine()
    {
        Debug.Log(">>> [State] EnemyTurn 시작");
        yield return new WaitForSeconds(0.5f);

        if (manager.currentEnemy != null && manager.currentEnemy.gameObject.activeSelf)
        {
            manager.currentEnemy.DoAttack();
        }

        // 적 공격 연출 대기 (Enemy 스크립트의 AttackDelay와 맞추거나 콜백 사용 권장)
        yield return new WaitForSeconds(1.0f);

        // 플레이어 턴으로 복귀
        manager.ChangeState(manager.statePlayerTurn);
    }
}