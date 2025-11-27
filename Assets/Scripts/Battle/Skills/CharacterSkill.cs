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

    [Header("QTE 설정")]
    public bool canTriggerQTE = false;  // 이 스킬이 QTE를 발동할 수 있는지
    public float qteTimeLimit = 3f;      // QTE 제한 시간

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

    /// <summary>
    /// ★ QTE 조건 체크 - 발사 중 매 슬롯마다 호출
    /// </summary>
    /// <param name="manager">배틀 매니저</param>
    /// <param name="slotIndex">현재 발사 중인 슬롯</param>
    /// <param name="firedCard">발사된 카드</param>
    /// <returns>true면 QTE 발동</returns>
    public virtual bool CheckQTECondition(BattleManager manager, int slotIndex, CardData firedCard)
    {
        return false;
    }

    /// <summary>
    /// ★ QTE 성공 시 호출 (캐릭터 교체 후)
    /// </summary>
    public virtual void OnQTESuccess(BattleManager manager, CharacterData newCharacter) { }

    /// <summary>
    /// ★ QTE 실패/시간초과 시 호출
    /// </summary>
    public virtual void OnQTEFailed(BattleManager manager) { }
}