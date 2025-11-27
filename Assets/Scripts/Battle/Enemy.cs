using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class EnemyState
{
    public EnemyData data;
    public int currentHp;
    public Dictionary<StatusEffectType, int> statusEffects = new Dictionary<StatusEffectType, int>();

    public EnemyState(EnemyData data)
    {
        this.data = data;
        this.currentHp = data.maxHp;
        this.statusEffects = new Dictionary<StatusEffectType, int>();
    }
}

public class Enemy : MonoBehaviour
{
    [Header("데이터 연결")]
    public EnemyState activeState;

    // [핵심] 군단 대기열 (Squad Pool)
    public List<EnemyState> reservePool = new List<EnemyState>();

    // 인스펙터 할당용
    public List<EnemyData> startingReserveList;

    [Header("UI 연결")]
    public Image hpBarFill;
    public TextMeshProUGUI hpText;
    public Image enemyImage;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI reserveCountText;

    [Header("★ 군단 기능 (Squad System)")]
    public GameObject minionPrefab;

    private BattleManager battleManager;

    public int currentHp => activeState != null ? activeState.currentHp : 0;
    public EnemyData data => activeState != null ? activeState.data : null;

    void Start()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        if (activeState == null && data != null)
        {
            Setup(data, startingReserveList);
        }

        if (minionPrefab == null) minionPrefab = gameObject;
    }

    public void Setup(EnemyData mainData, List<EnemyData> subDataList = null)
    {
        activeState = new EnemyState(mainData);

        reservePool.Clear();
        if (subDataList != null)
        {
            foreach (var subData in subDataList)
                if (subData != null) reservePool.Add(new EnemyState(subData));
        }
        else if (startingReserveList != null)
        {
            foreach (var subData in startingReserveList)
                if (subData != null) reservePool.Add(new EnemyState(subData));
        }

        UpdateVisuals();
        UpdateUI();
    }

    public void ApplyStatus(StatusEffectType type, int amount)
    {
        if (type == StatusEffectType.None || activeState == null) return;

        if (activeState.statusEffects.ContainsKey(type))
            activeState.statusEffects[type] += amount;
        else
            activeState.statusEffects.Add(type, amount);

        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        if (activeState == null) return;

        int finalDamage = amount;
        bool hasWound = false;

        if (activeState.statusEffects.ContainsKey(StatusEffectType.Wound))
        {
            int woundStack = activeState.statusEffects[StatusEffectType.Wound];
            if (woundStack > 0)
            {
                finalDamage += woundStack;
                hasWound = true;
            }
        }

        activeState.currentHp -= finalDamage;
        if (activeState.currentHp < 0) activeState.currentHp = 0;

        // ★ 데미지 팝업 표시
        if (DamagePopupManager.Instance != null)
        {
            // enemyImage가 있으면 그 위치 사용, 없으면 자기 자신
            Transform popupTarget = (enemyImage != null) ? enemyImage.transform : transform;
            DamagePopupManager.Instance.SpawnAtTransform(popupTarget, finalDamage, hasWound);
        }

        // ★ 적 스킬: 피해 받을 때
        TriggerSkills_OnTakeDamage(finalDamage);

        UpdateUI();
        StartCoroutine(HitEffect());

        if (activeState.currentHp <= 0) Die();
    }

    // AI 행동 결정
    public void DoAttack()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        if (battleManager) StartCoroutine(EnemyTurnRoutine());
    }

    IEnumerator EnemyTurnRoutine()
    {
        bool shouldSwap = false;
        bool hitAndRun = false; // 때리고 튀기 (게릴라)

        // [전술 AI] 예시 패턴
        if (reservePool.Count > 0)
        {
            float hpRatio = (float)activeState.currentHp / activeState.data.maxHp;

            // 1. 위기 상황: 체력 30% 이하면 교체해서 도망감
            if (hpRatio <= 0.3f && Random.value < 0.5f)
            {
                shouldSwap = true;
            }
            // 2. 게릴라 전술: 체력 많을 때 20% 확률로 때리고 교체 (Hit & Run)
            else if (hpRatio > 0.7f && Random.value < 0.2f)
            {
                hitAndRun = true;
            }
        }

        if (shouldSwap)
        {
            // 공격 없이 바로 교체 (도망/정비)
            yield return StartCoroutine(SwapRoutine());
        }
        else if (hitAndRun)
        {
            // 공격 후 교체 (치고 빠지기)
            yield return StartCoroutine(AttackRoutine());
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(SwapRoutine());
        }
        else
        {
            // 일반 공격
            yield return StartCoroutine(AttackRoutine());
        }
    }

    // 1:1 순환 교체 (Rotation)
    IEnumerator SwapRoutine()
    {
        if (reservePool.Count == 0) yield break;

        EnemyState nextEnemy = reservePool[0];
        Debug.Log($"🔄 [Enemy] 태그! {activeState.data.enemyName} -> {nextEnemy.data.enemyName}");

        // 사라짐 연출
        if (enemyImage) enemyImage.color = new Color(1, 1, 1, 0.5f);
        yield return new WaitForSeconds(0.3f);

        // 데이터 스왑 (현재 적은 대기열 맨 뒤로)
        reservePool.RemoveAt(0);
        reservePool.Add(activeState);
        activeState = nextEnemy;

        // 등장 연출
        UpdateVisuals();
        UpdateUI();

        if (enemyImage) enemyImage.color = Color.white;
        yield return new WaitForSeconds(0.5f);
    }

    // 1:N 소환 (Deploy)
    public void DeployReserveMember(Transform spawnLocation)
    {
        if (reservePool.Count == 0) return;

        EnemyState deployState = reservePool[0];
        reservePool.RemoveAt(0);

        GameObject minionObj = Instantiate(minionPrefab, spawnLocation.position, Quaternion.identity, spawnLocation.parent);
        Enemy minionScript = minionObj.GetComponent<Enemy>();

        minionScript.activeState = deployState;
        minionScript.Setup(deployState.data);

        if (battleManager != null)
        {
            battleManager.spawnedEnemies.Add(minionScript);
        }

        UpdateVisuals();
        Debug.Log($"📢 [Enemy] 지원군 소환! {deployState.data.enemyName} 등장!");
    }

    // N:1 흡수 (Absorb)
    public void AbsorbAlly(Enemy targetAlly)
    {
        if (targetAlly == null || targetAlly == this) return;

        Debug.Log($"🌪️ [Enemy] {activeState.data.enemyName}가 {targetAlly.data.enemyName}를 흡수(합류)합니다!");

        if (targetAlly.activeState != null)
        {
            reservePool.Add(targetAlly.activeState);
            if (targetAlly.reservePool.Count > 0)
                reservePool.AddRange(targetAlly.reservePool);
        }

        if (battleManager != null)
            battleManager.spawnedEnemies.Remove(targetAlly);

        Destroy(targetAlly.gameObject);
        UpdateVisuals();
    }

    IEnumerator AttackRoutine()
    {
        // ★ 적 스킬: 공격 전
        TriggerSkills_OnBeforeAttack();

        float delay = (data != null) ? data.attackDelay : 0.5f;
        yield return new WaitForSeconds(delay);

        Vector3 originalPos = transform.position;
        transform.position += Vector3.down * 20f;

        if (battleManager != null)
        {
            int damage = (data != null) ? data.attackPower : 10;
            battleManager.PlayerTakeDamage(damage);
        }

        yield return new WaitForSeconds(0.2f);
        transform.position = originalPos;

        // ★ 적 스킬: 공격 후
        TriggerSkills_OnAfterAttack();
    }

    void UpdateVisuals()
    {
        if (enemyImage != null && data != null && data.sprite != null)
        {
            enemyImage.sprite = data.sprite;
        }

        if (reserveCountText != null)
        {
            reserveCountText.text = reservePool.Count > 0 ? $"+{reservePool.Count}" : "";
        }
    }

    void UpdateUI()
    {
        if (activeState == null || data == null) return;

        if (hpBarFill != null)
            hpBarFill.fillAmount = (float)activeState.currentHp / data.maxHp;

        if (hpText != null)
            hpText.text = $"{activeState.currentHp} / {data.maxHp}";

        if (statusText != null)
        {
            string statusStr = "";
            foreach (var pair in activeState.statusEffects)
            {
                if (pair.Value > 0) statusStr += $"{pair.Key}: {pair.Value}\n";
            }
            statusText.text = statusStr;
        }
    }

    IEnumerator HitEffect()
    {
        if (enemyImage != null) enemyImage.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (enemyImage != null) enemyImage.color = Color.white;
    }

    void Die()
    {
        // ★ 적 스킬: 사망 시
        TriggerSkills_OnDeath();

        // 사망 시: 대기열 있으면 증원, 없으면 전사
        if (reservePool.Count > 0)
        {
            Debug.Log($"💀 [Enemy] {activeState.data.enemyName} 사망! 다음 타자 등판!");
            StartCoroutine(ReinforceRoutine());
        }
        else
        {
            Debug.Log("💀 [Enemy] 전멸! 완전 처치됨!");
            if (battleManager != null) battleManager.spawnedEnemies.Remove(this);
            gameObject.SetActive(false);
        }
    }

    IEnumerator ReinforceRoutine()
    {
        if (enemyImage) enemyImage.color = Color.clear;
        yield return new WaitForSeconds(0.5f);

        activeState = reservePool[0];
        reservePool.RemoveAt(0);

        UpdateVisuals();
        UpdateUI();
        if (enemyImage) enemyImage.color = Color.white;

        Debug.Log($"👹 [Enemy] {activeState.data.enemyName} 난입!");
    }

    #region [스킬 시스템]

    /// <summary>
    /// 턴 시작 시 모든 스킬 호출
    /// </summary>
    public void TriggerSkills_OnTurnStart()
    {
        if (data == null || data.enemySkills == null) return;
        foreach (var skill in data.enemySkills)
        {
            if (skill != null) skill.OnTurnStart(this, battleManager);
        }
    }

    /// <summary>
    /// 공격 전 모든 스킬 호출
    /// </summary>
    void TriggerSkills_OnBeforeAttack()
    {
        if (data == null || data.enemySkills == null) return;
        foreach (var skill in data.enemySkills)
        {
            if (skill != null) skill.OnBeforeAttack(this, battleManager);
        }
    }

    /// <summary>
    /// 공격 후 모든 스킬 호출
    /// </summary>
    void TriggerSkills_OnAfterAttack()
    {
        if (data == null || data.enemySkills == null) return;
        foreach (var skill in data.enemySkills)
        {
            if (skill != null) skill.OnAfterAttack(this, battleManager);
        }
    }

    /// <summary>
    /// 피해 받을 때 모든 스킬 호출
    /// </summary>
    void TriggerSkills_OnTakeDamage(int damage)
    {
        if (data == null || data.enemySkills == null) return;
        foreach (var skill in data.enemySkills)
        {
            if (skill != null) skill.OnTakeDamage(this, battleManager, damage);
        }
    }

    /// <summary>
    /// 사망 시 모든 스킬 호출
    /// </summary>
    void TriggerSkills_OnDeath()
    {
        if (data == null || data.enemySkills == null) return;
        foreach (var skill in data.enemySkills)
        {
            if (skill != null) skill.OnDeath(this, battleManager);
        }
    }

    #endregion
}