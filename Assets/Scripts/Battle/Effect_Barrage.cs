using UnityEngine;
using System.Collections;

// "연타 + 상태이상" 전용 효과
[CreateAssetMenu(menuName = "TriggerHappy/Effects/Logic/Barrage (Multi-Hit)", fileName = "Logic_Barrage")]
public class Effect_Barrage : CardEffect
{
    [Header("연타 설정")]
    public int hitCount = 5;          // 몇 번 때릴지
    public float delayPerHit = 0.15f; // 타격 간격

    [Header("상태이상 설정")]
    public StatusEffectType statusType = StatusEffectType.Wound; // 부여할 상태이상
    public int statusAmountPerHit = 1; // 타격당 부여할 수치

    public override void OnUse(BattleManager gm, int damagePerHit)
    {
        // CardEffect는 ScriptableObject라 코루틴을 직접 돌릴 수 없습니다.
        // 따라서 BattleManager에게 코루틴 실행을 부탁(Delegate)합니다.
        gm.StartCoroutine(BarrageRoutine(gm, damagePerHit));
    }

    IEnumerator BarrageRoutine(BattleManager gm, int damage)
    {
        // 타격 횟수만큼 반복
        for (int i = 0; i < hitCount; i++)
        {
            // 1. 적이 살아있는지 확인
            if (gm.currentEnemy == null || gm.currentEnemy.currentHp <= 0) break;

            // 2. 상태이상 부여
            // (상처를 먼저 부여하고 때려야 첫 타부터 아플지, 때리고 부여할지는 순서 조절 가능)
            // 여기서는 "때리면서 상처가 벌어진다"는 느낌으로 [선 데미지 후 상태이상] 혹은 [동시] 처리

            // 데미지 처리 (Enemy.TakeDamage 내부에서 Wound 체크함)
            gm.ApplyDamageToEnemy(damage);

            // 상태이상 부여
            gm.currentEnemy.ApplyStatus(statusType, statusAmountPerHit);

            // 3. 연출 딜레이 (타-다-다-닥)
            yield return new WaitForSeconds(delayPerHit);
        }

        // ★ 효과 완료 알림
        gm.isEffectRunning = false;
        Debug.Log("✅ [Barrage] 다단히트 완료");
    }
}