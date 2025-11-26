using UnityEngine;

// 리볼버 구현체
// 경로: Scripts/Battle/Weapons/Weapon_Revolver.cs
[CreateAssetMenu(fileName = "Weapon_Revolver", menuName = "TriggerHappy/Weapons/Revolver")]
public class Weapon_Revolver : WeaponData
{
    public override string GetDamageText(int baseDamage)
    {
        // 리볼버는 깡뎀 그대로 표시
        return baseDamage.ToString();
    }

    public override int CalculateFinalDamage(int baseDamage)
    {
        return baseDamage;
    }
}