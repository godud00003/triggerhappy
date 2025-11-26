using UnityEngine;

// [기반 클래스] 모든 전투 상태의 부모입니다.
// "설계도" 역할을 하며, 실제 기능은 없습니다.
public abstract class BattleState
{
    protected BattleManager manager;

    public BattleState(BattleManager manager)
    {
        this.manager = manager;
    }

    // 상태 진입 시 1회 실행 (초기화)
    public virtual void Enter() { }

    // 매 프레임 실행 (Update)
    public virtual void Execute() { }

    // 상태 종료 시 1회 실행 (정리)
    public virtual void Exit() { }
}