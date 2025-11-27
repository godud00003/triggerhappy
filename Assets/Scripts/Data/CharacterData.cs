using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Character", menuName = "TriggerHappy/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("📝 기본 정보")]
    public string characterName;
    [TextArea] public string description;

    [Header("🎨 비주얼")]
    public Sprite portrait;
    public GameObject modelPrefab;

    [Header("⚔️ 전투 스탯")]
    public int maxHp = 100;
    public int defense = 0;

    [Header("🔫 무기 전략 (Strategy)")]
    public WeaponData weaponStrategy;

    [Header("⭐ 고유 스킬 (복수 가능)")]
    public List<CharacterSkill> characterSkills = new List<CharacterSkill>();

    [Header("🃏 초기 덱")]
    public List<CardData> startingDeck;
}