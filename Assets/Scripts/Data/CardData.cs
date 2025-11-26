using UnityEngine;
using System.Collections.Generic;

public enum CardType { Attack, Skill, Ability, Curse }
public enum CardRarity { Common, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "New Card", menuName = "TriggerHappy/Card Data")]
public class CardData : ScriptableObject
{
    [Header("📝 기본 정보")]
    public string cardName;
    [TextArea] public string description;
    public Sprite icon;
    public Color themeColor = Color.white;

    [Header("📊 분류 및 등급")]
    public CardType cardType;
    public CardRarity rarity;

    [Header("⚙️ 로직 (순서대로 실행됨)")]
    // 여러 개의 효과를 가질 수 있음 (예: 데미지 주고 + 힐 하고 + 드로우)
    public List<CardAction> actions;
}

[System.Serializable]
public class CardAction
{
    public string label;            // 에디터 식별용 (예: "기본 공격")
    public CardEffect effectLogic;  // 실제 기능을 수행하는 SO (Logic_Damage 등)
    public int value;               // 적용 수치 (데미지 10 등)
}