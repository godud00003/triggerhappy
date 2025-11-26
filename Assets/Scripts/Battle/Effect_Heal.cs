using UnityEngine;

[CreateAssetMenu(menuName = "TriggerHappy/Effects/Logic/Heal", fileName = "Logic_Heal")]
public class Effect_Heal : CardEffect
{
    public override void OnUse(BattleManager gm, int amount)
    {
        gm.HealPlayer(amount);
        Debug.Log($"💚 [Heal] {amount}");
    }
}