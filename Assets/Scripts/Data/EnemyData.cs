using UnityEngine;
using System.Collections.Generic;

public enum EnemyIntent { Attack, Defend, Buff, Debuff, Special }

[CreateAssetMenu(fileName = "New Enemy", menuName = "TriggerHappy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("📝 기본 정보")]
    public string enemyName;
    [TextArea] public string description;

    [Header("🎨 비주얼")]
    public Sprite sprite;
    public GameObject prefab; // 3D/Spine 사용 시

    [Header("⚔️ 전투 스탯")]
    public int maxHp = 100;
    public int attackPower = 10;
    public float attackDelay = 1.0f; // 공격 애니메이션 속도 제어용

    [Header("⭐ 고유 스킬 (복수 가능)")]
    public List<EnemySkill> enemySkills = new List<EnemySkill>();

    [Header("🤖 행동 패턴 (AI)")]
    // 적이 사용할 수 있는 스킬 목록 (확률 혹은 순서대로 사용)
    public List<EnemyPattern> patterns;
}

[System.Serializable]
public class EnemyPattern
{
    public string patternName;
    public EnemyIntent intent;
    public int value; // 데미지 혹은 쉴드량
    public float chance = 1.0f; // 발동 확률 가중치
}