using UnityEngine;

// [핵심] 적 데이터 생성 메뉴 추가
[CreateAssetMenu(fileName = "New Enemy", menuName = "TriggerHappy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;    // 적 이름
    public Sprite sprite;       // 적 모습 (이미지)
    public int maxHp;           // 최대 체력
    public int attackPower;     // 공격력

    [Header("행동 패턴 (나중에 확장 가능)")]
    public float attackDelay = 0.5f; // 공격 전 대기 시간
}