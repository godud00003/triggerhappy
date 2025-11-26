using UnityEngine;
using System.Collections;

// ==========================================
// 게임의 전체 흐름을 담당하는 상태들
// (Start -> LevelUp -> End)
// ==========================================

// 1. Start: 전투 준비
public class State_Start : BattleState
{
    public State_Start(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        manager.StartCoroutine(SetupRoutine());
    }

    IEnumerator SetupRoutine()
    {
        if (manager.resultPanel) manager.resultPanel.SetActive(false);
        if (manager.qtePanel) manager.qtePanel.SetActive(false);

        // 덱 초기화
        manager.currentDeck.Clear();
        manager.currentDiscard.Clear();
        foreach (var member in manager.currentParty)
        {
            if (member.startingDeck != null) manager.currentDeck.AddRange(member.startingDeck);
        }
        ShuffleDeck();
        manager.UpdateDeckUI();

        // 필드 초기화
        foreach (var slot in manager.slots) slot.ClearSlot();
        foreach (Transform child in manager.handArea) Object.Destroy(child.gameObject);

        // 실린더 회전 초기화
        manager.cylinderPivot.rotation = Quaternion.identity;

        yield return new WaitForSeconds(0.5f);
        manager.ChangeState(manager.statePlayerTurn);
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < manager.currentDeck.Count; i++)
        {
            CardData temp = manager.currentDeck[i];
            int rand = Random.Range(i, manager.currentDeck.Count);
            manager.currentDeck[i] = manager.currentDeck[rand];
            manager.currentDeck[rand] = temp;
        }
    }
}

// 2. LevelUp: 뱀서식 보상 선택
public class State_LevelUp : BattleState
{
    public State_LevelUp(BattleManager manager) : base(manager) { }

    public override void Enter()
    {
        if (manager.rewardManager != null)
        {
            manager.rewardManager.ShowRewardPopup();
        }
    }

    // RewardManager가 선택 완료 후 manager.StartNextBattle() 등을 호출함
}

// 3. End: 승리/패배 결과
public class State_End : BattleState
{
    bool isVictory;

    public State_End(BattleManager manager) : base(manager) { }

    public void SetResult(bool victory) => this.isVictory = victory;

    public override void Enter()
    {
        manager.StartCoroutine(EndSequence());
    }

    IEnumerator EndSequence()
    {
        yield return new WaitForSeconds(1.0f);
        if (manager.resultPanel) manager.resultPanel.SetActive(true);

        if (isVictory)
        {
            if (manager.resultText) manager.resultText.text = "<color=green>VICTORY</color>";
            if (manager.rewardManager)
            {
                manager.rewardManager.AddXp(100);
                yield return new WaitForSeconds(1.0f);

                if (manager.rewardManager.pendingRewardCount > 0)
                {
                    manager.ChangeState(manager.stateLevelUp);
                }
                else
                {
                    manager.StartNextBattle();
                }
            }
        }
        else
        {
            if (manager.resultText) manager.resultText.text = "<color=red>DEFEAT</color>";
        }
    }
}