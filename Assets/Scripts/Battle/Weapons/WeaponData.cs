using UnityEngine;

// [Strategy Pattern] 무기 전략 인터페이스
// 이 파일은 Scripts/Battle/Weapons/ 폴더에 두세요.
public interface IWeaponCalculator
{
    // UI 표시용 문자열 반환 (예: "10", "2x5")
    string GetDamageText(int baseDamage);

    // 실제 데미지 계산 (전투 로직용)
    int CalculateFinalDamage(int baseDamage);
}

// 유니티 인스펙터에 연결하기 위해 ScriptableObject로 래핑
public abstract class WeaponData : ScriptableObject, IWeaponCalculator
{
    public string weaponName;
    public abstract string GetDamageText(int baseDamage);
    public abstract int CalculateFinalDamage(int baseDamage);
}