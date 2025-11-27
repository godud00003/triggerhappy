using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 트리플 콤보 스킬
/// 같은 카드 3장 연속 장전 시 다음 빈 슬롯에 SP카드 생성
/// </summary>
[CreateAssetMenu(menuName = "TriggerHappy/Skills/Triple Combo", fileName = "Skill_TripleCombo")]
public class Skill_TripleCombo : CharacterSkill
{
    [Header("트리플 콤보 설정")]
    public CardData spCardToGenerate;  // 생성할 SP카드
    public int requiredCombo = 3;       // 필요한 연속 장수

    public override void OnCardLoaded(BattleManager manager, int slotIndex, CardData loadedCard)
    {
        RefreshSPCards(manager);
    }

    public override void OnCardUnloaded(BattleManager manager, int slotIndex)
    {
        RefreshSPCards(manager);
    }

    /// <summary>
    /// 전체 슬롯 상태를 확인하고 SP카드 생성/제거
    /// </summary>
    void RefreshSPCards(BattleManager manager)
    {
        List<CylinderSlot> slots = manager.slots;
        int slotCount = slots.Count;

        // 1. 먼저 기존 SP카드 위치 찾기
        int currentSPSlot = -1;
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i].isLoaded && slots[i].isSpecialCard)
            {
                currentSPSlot = i;
                break;
            }
        }

        // 2. 유효한 콤보 찾기
        int validSPSlot = FindValidSPSlot(slots, slotCount);

        // 3. 상태 비교 후 처리
        if (validSPSlot >= 0)
        {
            // 콤보 유효함
            if (currentSPSlot < 0)
            {
                // SP카드 없으면 생성
                GenerateSPCard(manager, validSPSlot);
            }
            else if (currentSPSlot != validSPSlot)
            {
                // 위치가 다르면 기존 거 제거하고 새로 생성
                slots[currentSPSlot].ClearSlot(sendToDiscard: false);
                GenerateSPCard(manager, validSPSlot);
            }
            // 위치가 같으면 유지
        }
        else
        {
            // 콤보 없음 - SP카드 있으면 제거
            if (currentSPSlot >= 0)
            {
                Debug.Log($"💨 [TripleCombo] 콤보 깨짐! SP카드 소멸");
                slots[currentSPSlot].ClearSlot(sendToDiscard: false);
            }
        }
    }

    /// <summary>
    /// 유효한 콤보를 찾아서 SP카드가 들어갈 슬롯 인덱스 반환
    /// </summary>
    int FindValidSPSlot(List<CylinderSlot> slots, int slotCount)
    {
        for (int startIdx = 0; startIdx <= slotCount - requiredCombo; startIdx++)
        {
            // 시작 슬롯이 비어있거나 SP카드면 스킵
            if (!slots[startIdx].isLoaded || slots[startIdx].isSpecialCard) continue;

            string targetCardName = slots[startIdx].loadedCard.cardName;
            bool isCombo = true;

            // 연속 체크
            for (int i = 1; i < requiredCombo; i++)
            {
                int checkIdx = startIdx + i;
                CylinderSlot checkSlot = slots[checkIdx];

                if (!checkSlot.isLoaded ||
                    checkSlot.isSpecialCard ||
                    checkSlot.loadedCard.cardName != targetCardName)
                {
                    isCombo = false;
                    break;
                }
            }

            // 콤보 성공!
            if (isCombo)
            {
                int nextSlotIdx = startIdx + requiredCombo;

                // 다음 슬롯이 존재하고 (비어있거나 이미 SP카드인 경우)
                if (nextSlotIdx < slotCount)
                {
                    CylinderSlot nextSlot = slots[nextSlotIdx];
                    if (!nextSlot.isLoaded || nextSlot.isSpecialCard)
                    {
                        return nextSlotIdx;
                    }
                }
            }
        }

        return -1; // 유효한 콤보 없음
    }

    /// <summary>
    /// SP카드 생성
    /// </summary>
    void GenerateSPCard(BattleManager manager, int slotIndex)
    {
        if (spCardToGenerate == null)
        {
            Debug.LogError("⛔ [TripleCombo] SP카드가 설정되지 않았습니다!");
            return;
        }

        CylinderSlot slot = manager.slots[slotIndex];

        // SP카드 장전
        slot.LoadBullet(spCardToGenerate, isSpecial: true);

        Debug.Log($"⭐ [TripleCombo] SP카드 '{spCardToGenerate.cardName}' 생성! (슬롯 {slotIndex + 1})");
    }
}