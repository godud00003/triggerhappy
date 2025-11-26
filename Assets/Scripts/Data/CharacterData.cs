using UnityEngine;
using System.Collections.Generic;

// [핵심] 우클릭 -> Create -> TriggerHappy -> Character Data로 캐릭터를 찍어낼 수 있음
[CreateAssetMenu(fileName = "New Character", menuName = "TriggerHappy/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string characterName;  // 캐릭터 이름 (예: LENA)
    public Sprite portrait;       // 전신 일러스트 혹은 초상화
    public int maxHp = 100;       // 최대 체력

    [Header("전용 덱 설정")]
    // 이 캐릭터가 게임 시작할 때 들고 나갈 카드들
    public List<CardData> startingDeck;
}