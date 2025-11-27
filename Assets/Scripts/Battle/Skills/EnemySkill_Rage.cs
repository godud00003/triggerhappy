using UnityEngine;

/// <summary>
/// 분노 스킬 - 체력이 낮아지면 공격력 증가
/// </summary>
[CreateAssetMenu(menuName = "TriggerHappy/Skills/Enemy/Rage", fileName = "EnemySkill_Rage")]
public class EnemySkill_Rage : EnemySkill
{
    [Header("분노 설정")]
    [Range(0f, 1f)]
    public float hpThreshold = 0.5f;    // 체력 50% 이하일 때 발동
    public int bonusDamage = 10;         // 추가 데미지

    private bool isRaging = false;

    public override void OnBeforeAttack(Enemy enemy, BattleManager manager)
    {
        if (enemy.data == null) return;

        float hpRatio = (float)enemy.currentHp / enemy.data.maxHp;

        if (hpRatio <= hpThreshold && !isRaging)
        {
            isRaging = true;
            Debug.Log($"🔥 [Rage] {enemy.data.enemyName} 분노! 공격력 +{bonusDamage}!");
        }

        // 분노 상태면 추가 데미지 (Enemy의 attackPower를 직접 수정하지 않고 별도 처리)
        if (isRaging && manager != null)
        {
            // 추가 데미지를 별도로 적용
            manager.PlayerTakeDamage(bonusDamage);
            Debug.Log($"💥 [Rage] 분노 추가 데미지 {bonusDamage}!");
        }
    }

    public override void OnTurnStart(Enemy enemy, BattleManager manager)
    {
        // 체력 회복 등으로 분노 해제될 수 있음
        if (enemy.data == null) return;

        float hpRatio = (float)enemy.currentHp / enemy.data.maxHp;
        if (hpRatio > hpThreshold)
        {
            isRaging = false;
        }
    }
}
