using UnityEngine;

[CreateAssetMenu(menuName = "TriggerHappy/Effects/Logic/Damage", fileName = "Logic_Damage")]
public class Effect_Damage : CardEffect
{
    public override void OnUse(BattleManager gm, int amount)
    {
        gm.ApplyDamageToEnemy(amount);
        Debug.Log($"⚔️ [Damage] {amount}");
    }
}