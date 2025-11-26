using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Card", menuName = "TriggerHappy/Card Data")]
public class CardData : ScriptableObject
{
    [Header("카드 비주얼")]
    public string cardName;
    public Sprite icon;
    public Color themeColor = Color.white;

    [Header("액션 리스트 (순서대로 실행됨)")]
    // [핵심] 이 부분이 빠져서 에러가 났었습니다.
    public List<CardAction> actions;

    [TextArea] public string description;
}

// [핵심] 액션 하나를 정의하는 클래스
[System.Serializable]
public class CardAction
{
    public string label;           // 메모용 (예: 공격)
    public CardEffect effectLogic; // 기능 파일 (예: Logic_Damage)
    public int value;              // 수치 (예: 30)
}