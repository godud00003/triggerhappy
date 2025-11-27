using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// ==========================================
// 실제 전투 로직을 담당하는 상태들 (Combat)
// ==========================================

public class State_PlayerTurn : BattleState
{
    public State_PlayerTurn(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        manager.RecoverSP();
        manager.StartCoroutine(RefillHand());
    }

    public void OnReload()
    {
        manager.StartCoroutine(ReloadRoutine());
    }

    IEnumerator RefillHand()
    {
        int maxHandSize = 5;
        HandManager handManager = manager.handArea.GetComponent<HandManager>();
        int currentHandCount = (handManager != null) ? handManager.CardCount : manager.handArea.childCount;
        int cardsToDraw = maxHandSize - currentHandCount;

        for (int i = 0; i < cardsToDraw; i++)
        {
            if (manager.currentDeck.Count == 0 && manager.currentDiscard.Count > 0)
            {
                yield return manager.StartCoroutine(ShuffleDeckAnimation());
            }

            if (manager.currentDeck.Count > 0)
            {
                CardData card = manager.currentDeck[0];
                manager.currentDeck.RemoveAt(0);

                GameObject obj = Object.Instantiate(manager.cardPrefab, manager.handArea);
                BulletCard bulletCard = obj.GetComponent<BulletCard>();
                bulletCard.Setup(card);

                Vector3 deckWorldPos = manager.handArea.position + Vector3.left * 400f;
                if (manager.uiManager != null && manager.uiManager.deckCountText != null)
                    deckWorldPos = manager.uiManager.deckCountText.transform.position;

                if (handManager != null) handManager.AddCard(obj, deckWorldPos);
                else
                {
                    CardAnimator animator = obj.GetComponent<CardAnimator>();
                    if (animator == null) animator = obj.AddComponent<CardAnimator>();
                    animator.PlayDrawAnimation(deckWorldPos);
                }
            }
            yield return new WaitForSeconds(0.12f);
        }
        manager.UpdateDeckUI();
    }

    IEnumerator ShuffleDeckAnimation()
    {
        Vector3 discardPos = manager.handArea.position + Vector3.right * 400f;
        Vector3 deckPos = manager.handArea.position + Vector3.left * 400f;
        if (manager.uiManager != null)
        {
            if (manager.uiManager.discardCountText) discardPos = manager.uiManager.discardCountText.transform.position;
            if (manager.uiManager.deckCountText) deckPos = manager.uiManager.deckCountText.transform.position;
        }

        int visualCount = Mathf.Min(manager.currentDiscard.Count, 5);
        for (int i = 0; i < visualCount; i++)
        {
            GameObject dummy = Object.Instantiate(manager.cardPrefab, manager.uiManager.transform);
            dummy.transform.position = discardPos;
            dummy.transform.localScale = Vector3.one * 0.1f;
            Object.Destroy(dummy.GetComponent<BulletCard>());
            Object.Destroy(dummy.GetComponent<Button>());

            CardAnimator anim = dummy.GetComponent<CardAnimator>();
            if (anim == null) anim = dummy.AddComponent<CardAnimator>();
            anim.PlayShuffleAnimation(discardPos, deckPos, () => Object.Destroy(dummy));
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.5f);

        manager.currentDeck.AddRange(manager.currentDiscard);
        manager.currentDiscard.Clear();
        ShuffleDeck();
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
        HandManager handManager = manager.handArea.GetComponent<HandManager>();
        Vector3 discardPos = manager.handArea.position + Vector3.right * 400f;
        if (manager.uiManager != null && manager.uiManager.discardCountText != null)
            discardPos = manager.uiManager.discardCountText.transform.position;

        foreach (var slot in manager.slots)
        {
            if (slot.isLoaded) manager.DiscardCard(slot.loadedCard);
            slot.ClearSlot();
        }

        if (handManager != null)
        {
            List<GameObject> cardsToDiscard = new List<GameObject>();
            foreach (Transform child in manager.handArea) cardsToDiscard.Add(child.gameObject);
            handManager.ClearHand();

            foreach (GameObject cardObj in cardsToDiscard)
            {
                BulletCard bullet = cardObj.GetComponent<BulletCard>();
                if (bullet) manager.DiscardCard(bullet.cardData);

                CardAnimator animator = cardObj.GetComponent<CardAnimator>();
                if (animator == null) animator = cardObj.AddComponent<CardAnimator>();
                animator.PlayDiscardAnimation(discardPos, () => Object.Destroy(cardObj));
                yield return new WaitForSeconds(0.05f);
            }
        }
        else
        {
            foreach (Transform child in manager.handArea)
            {
                BulletCard card = child.GetComponent<BulletCard>();
                if (card) manager.DiscardCard(card.cardData);
                Object.Destroy(child.gameObject);
            }
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
        int capacity = manager.slots.Count;
        if (capacity == 0)
        {
            manager.ChangeState(manager.stateEnemyTurn);
            yield break;
        }

        float angleStep = 360f / capacity;

        for (int i = 0; i < capacity; i++)
        {
            CylinderSlot slot = manager.slots[i];

            if (slot.isLoaded)
            {
                CharacterSkill triggeringSkill = CheckQTEFromSkills(manager, i, slot.loadedCard);
                if (triggeringSkill != null)
                {
                    manager.currentQTESkill = triggeringSkill;
                    manager.ChangeState(manager.stateQTE);
                    yield break;
                }

                // 1. 발사 애니메이션 연출 (카드 생성 -> 상단으로 날림)
                if (manager.cardPrefab != null)
                {
                    GameObject visualCard = Object.Instantiate(manager.cardPrefab, slot.transform);
                    visualCard.transform.localPosition = Vector3.zero;
                    visualCard.transform.localScale = Vector3.one;

                    BulletCard bc = visualCard.GetComponent<BulletCard>();
                    if (bc) bc.Setup(slot.loadedCard);

                    Object.Destroy(visualCard.GetComponent<Button>());
                    Object.Destroy(visualCard.GetComponent<CylinderSlot>());

                    // [Fix] 발사 시 슬롯 안의 카드 이미지는 즉시 숨김 (VisualCard가 날아가므로)
                    if (slot.iconImage) slot.iconImage.enabled = false;
                    if (slot.nameText) slot.nameText.text = "";
                    if (slot.specialEffectObj) slot.specialEffectObj.SetActive(false);

                    CardAnimator animator = visualCard.GetComponent<CardAnimator>();
                    if (animator == null) animator = visualCard.AddComponent<CardAnimator>();

                    // 화면 위쪽으로 날아가도록 목표 설정 (1자 발사)
                    Vector3 upTargetPos = slot.transform.position + Vector3.up * 1500f;

                    // 애니메이션 실행 (완료 후 제거)
                    animator.PlayFireAnimation(upTargetPos, () => {
                        Object.Destroy(visualCard);
                    });

                    // 1-1. 발사 애니메이션이 끝날 때까지 대기
                    yield return new WaitForSeconds(animator.fireDuration);
                }

                slot.PlayFireEffect(); // 슬롯 자체의 반동 효과

                // 2. 데미지/효과 처리 및 팝업
                if (slot.loadedCard.actions != null)
                {
                    foreach (var act in slot.loadedCard.actions)
                    {
                        if (act.effectLogic != null)
                        {
                            yield return manager.StartCoroutine(ExecuteEffectAndWait(act.effectLogic, act.value));
                        }
                    }
                }

                // 2-1. 팝업이 뜨고 나서 유저가 인지할 시간 대기
                yield return new WaitForSeconds(0.4f);

                // 3. 카드 소멸/묘지행 처리
                if (slot.isSpecialCard)
                    slot.ClearSlot(sendToDiscard: false);
                else
                    slot.ClearSlot(sendToDiscard: true);

                if (manager.currentEnemy == null || manager.currentEnemy.currentHp <= 0)
                {
                    ResetCylinder();
                    yield break;
                }
            }
            else
            {
                // 빈 슬롯은 빠르게 지나감
                yield return new WaitForSeconds(0.1f);
            }

            // 4. 실린더 회전
            float nextTargetZ = (i + 1) * angleStep;
            if (manager.cylinderPivot)
            {
                manager.cylinderPivot.rotation = Quaternion.Euler(0, 0, nextTargetZ);
            }
            // 회전 연출 대기
            yield return new WaitForSeconds(0.15f);
        }

        ResetCylinder();
        manager.ChangeState(manager.stateEnemyTurn);
    }

    IEnumerator ExecuteEffectAndWait(CardEffect effect, int value)
    {
        manager.isEffectRunning = true;
        effect.OnUse(manager, value);
        while (manager.isEffectRunning) yield return null;
    }

    void ResetCylinder()
    {
        if (manager.cylinderPivot != null)
        {
            manager.cylinderPivot.rotation = Quaternion.identity;
        }
    }

    CharacterSkill CheckQTEFromSkills(BattleManager manager, int slotIndex, CardData firedCard)
    {
        if (manager.subCharacters == null) return null;
        foreach (var subChar in manager.subCharacters)
        {
            if (subChar == null || subChar.characterSkills == null) continue;
            if (!manager.characterHpMap.ContainsKey(subChar) || manager.characterHpMap[subChar] <= 0) continue;
            foreach (var skill in subChar.characterSkills)
            {
                if (skill != null && skill.canTriggerQTE)
                {
                    if (skill.CheckQTECondition(manager, slotIndex, firedCard)) return skill;
                }
            }
        }
        return null;
    }
}

public class State_QTE_Slow : BattleState
{
    private float qteDuration = 3.0f;
    private float currentTimer;
    public State_QTE_Slow(BattleManager manager) : base(manager) { }
    public override void Enter()
    {
        Debug.Log("⚡ [State] QTE Time Start (Slow Motion)");
        if (manager.currentQTESkill != null) qteDuration = manager.currentQTESkill.qteTimeLimit;
        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        currentTimer = qteDuration;
        if (manager.uiManager)
        {
            manager.uiManager.SetActiveQTE(true);
            CharacterData s1 = GetSubChar(0);
            CharacterData s2 = GetSubChar(1);
            manager.uiManager.SetupQTEPortraits(s1, IsAlive(s1), s2, IsAlive(s2));
        }
    }
    CharacterData GetSubChar(int index)
    {
        if (manager.subCharacters != null && manager.subCharacters.Count > index) return manager.subCharacters[index];
        return null;
    }
    bool IsAlive(CharacterData ch)
    {
        return ch != null && manager.characterHpMap.ContainsKey(ch) && manager.characterHpMap[ch] > 0;
    }
    public override void Execute()
    {
        currentTimer -= Time.unscaledDeltaTime;
        if (manager.uiManager) manager.uiManager.UpdateQTETimer(currentTimer / qteDuration);
        if (currentTimer <= 0)
        {
            if (manager.currentQTESkill != null) manager.currentQTESkill.OnQTEFailed(manager);
            manager.currentQTESkill = null;
            manager.ChangeState(manager.stateResolution);
            return;
        }
        if (UnityEngine.Input.GetMouseButtonDown(0)) TryTag(GetSubChar(0));
        else if (UnityEngine.Input.GetMouseButtonDown(1)) TryTag(GetSubChar(1));
    }
    void TryTag(CharacterData nextChar)
    {
        if (IsAlive(nextChar))
        {
            if (manager.currentQTESkill != null) manager.currentQTESkill.OnQTESuccess(manager, nextChar);
            manager.SwapCharacter(nextChar);
            manager.currentQTESkill = null;
            manager.ChangeState(manager.stateResolution);
        }
    }
    public override void Exit()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        if (manager.uiManager) manager.uiManager.SetActiveQTE(false);
    }
}

public class State_EnemyTurn : BattleState
{
    public State_EnemyTurn(BattleManager manager) : base(manager) { }
    public override void Enter() { manager.StartCoroutine(EnemyRoutine()); }
    IEnumerator EnemyRoutine()
    {
        Debug.Log(">>> [State] EnemyTurn 시작");
        yield return new WaitForSeconds(0.5f);
        if (manager.currentEnemy != null && manager.currentEnemy.gameObject.activeSelf) manager.currentEnemy.DoAttack();
        yield return new WaitForSeconds(1.0f);
        manager.ChangeState(manager.statePlayerTurn);
    }
}