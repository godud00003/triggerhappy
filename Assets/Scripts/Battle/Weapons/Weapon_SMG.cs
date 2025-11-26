using UnityEngine;

// SMG 구현체
// 경로: Scripts/Battle/Weapons/Weapon_SMG.cs
[CreateAssetMenu(fileName = "Weapon_SMG", menuName = "TriggerHappy/Weapons/SMG")]
public class Weapon_SMG : WeaponData
{
    [Header("SMG 설정")]
    public int hitCount = 5;      // 5연발
    public float damageMultiplier = 0.2f; // 발당 20% 데미지

    public override string GetDamageText(int baseDamage)
    {
        int damagePerHit = Mathf.CeilToInt(baseDamage * damageMultiplier);
        return $"{damagePerHit}x{hitCount}";
    }

    public override int CalculateFinalDamage(int baseDamage)
    {
        int damagePerHit = Mathf.CeilToInt(baseDamage * damageMultiplier);
        return damagePerHit * hitCount;
    }
}