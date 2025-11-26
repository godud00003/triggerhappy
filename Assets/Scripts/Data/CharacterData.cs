using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Character", menuName = "TriggerHappy/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("📝 기본 정보")]
    public string characterName;
    [TextArea] public string description;

    [Header("🎨 비주얼")]
    public Sprite portrait;       // UI 표시용 초상화
    public GameObject modelPrefab; // 인게임 모델 (Spine/3D)

    [Header("⚔️ 전투 스탯")]
    public int maxHp = 100;
    public int defense = 0;

    [Header("🔫 무기 전략 (Strategy)")]
    // [중요] 캐릭터 교체 시 이 전략에 따라 카드 텍스트/효과가 변함
    public WeaponData weaponStrategy;

    [Header("🃏 초기 덱")]
    public List<CardData> startingDeck;
}