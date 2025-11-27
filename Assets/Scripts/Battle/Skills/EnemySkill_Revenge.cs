using UnityEngine;

/// <summary>
/// 복수 스킬 - 피해를 받으면 일정 확률로 즉시 반격
/// </summary>
[CreateAssetMenu(menuName = "TriggerHappy/Skills/Enemy/Revenge", fileName = "EnemySkill_Revenge")]
public class EnemySkill_Revenge : EnemySkill
{
    [Header("복수 설정")]
    [Range(0f, 1f)]
    public float triggerChance = 0.3f;  // 발동 확률 (30%)
    public int revengeDamage = 5;        // 반격 데미지

    public override void OnTakeDamage(Enemy enemy, BattleManager manager, int damage)
    {
        // 확률 체크
        if (Random.value > triggerChance) return;

        // 반격!
        Debug.Log($"💢 [Revenge] {enemy.data.enemyName}의 반격! {revengeDamage} 데미지!");
        
        if (manager != null)
        {
            manager.PlayerTakeDamage(revengeDamage);
        }
    }
}
