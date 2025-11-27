using UnityEngine;

/// <summary>
/// 캐릭터 고유 스킬의 베이스 클래스
/// 각 캐릭터마다 다른 스킬을 ScriptableObject로 만들 수 있음
/// </summary>
public abstract class CharacterSkill : ScriptableObject
{
    [Header("스킬 정보")]
    public string skillName;
    [TextArea] public string description;
    public Sprite skillIcon;

    /// <summary>
    /// 슬롯에 카드가 장전될 때마다 호출
    /// </summary>
    public virtual void OnCardLoaded(BattleManager manager, int slotIndex, CardData loadedCard) { }

    /// <summary>
    /// 슬롯에서 카드가 해제될 때마다 호출
    /// </summary>
    public virtual void OnCardUnloaded(BattleManager manager, int slotIndex) { }

    /// <summary>
    /// 발사 시퀀스 시작 시 호출
    /// </summary>
    public virtual void OnFireStart(BattleManager manager) { }

    /// <summary>
    /// 턴 시작 시 호출
    /// </summary>
    public virtual void OnTurnStart(BattleManager manager) { }
}
