using UnityEngine;

/// <summary>
/// 적 고유 스킬의 베이스 클래스
/// </summary>
public abstract class EnemySkill : ScriptableObject
{
    [Header("스킬 정보")]
    public string skillName;
    [TextArea] public string description;
    public Sprite skillIcon;

    /// <summary>
    /// 적 턴 시작 시 호출
    /// </summary>
    public virtual void OnTurnStart(Enemy enemy, BattleManager manager) { }

    /// <summary>
    /// 적 공격 전 호출
    /// </summary>
    public virtual void OnBeforeAttack(Enemy enemy, BattleManager manager) { }

    /// <summary>
    /// 적 공격 후 호출
    /// </summary>
    public virtual void OnAfterAttack(Enemy enemy, BattleManager manager) { }

    /// <summary>
    /// 적이 피해를 받을 때 호출
    /// </summary>
    public virtual void OnTakeDamage(Enemy enemy, BattleManager manager, int damage) { }

    /// <summary>
    /// 적이 사망할 때 호출
    /// </summary>
    public virtual void OnDeath(Enemy enemy, BattleManager manager) { }

    /// <summary>
    /// 매 프레임 호출 (패시브 효과용)
    /// </summary>
    public virtual void OnUpdate(Enemy enemy, BattleManager manager) { }
}
