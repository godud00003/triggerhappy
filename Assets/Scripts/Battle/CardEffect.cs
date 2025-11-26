using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    public abstract void OnUse(BattleManager targetManager, int amount);
}