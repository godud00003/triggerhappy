using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("데이터 연결")]
    public EnemyData data;

    [Header("실시간 상태")]
    public int currentHp;

    [Header("UI 연결")]
    public Image hpBarFill;
    public TextMeshProUGUI hpText;
    public Image enemyImage;

    private BattleManager battleManager;

    void Start()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        if (data != null) Setup(data);
        else
        {
            Debug.LogWarning($"[Enemy] {gameObject.name}에 EnemyData가 연결되지 않았습니다!");
            currentHp = 100;
            UpdateUI();
        }
    }

    public void Setup(EnemyData enemyData)
    {
        data = enemyData;
        currentHp = data.maxHp;

        if (enemyImage != null && data.sprite != null)
        {
            enemyImage.sprite = data.sprite;
        }

        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp < 0) currentHp = 0;

        UpdateUI();
        StartCoroutine(HitEffect());

        string targetName = (data != null) ? data.enemyName : "Unknown Enemy";
        Debug.Log($"{targetName} 체력: {currentHp}");

        if (currentHp <= 0) Die();
    }

    public void DoAttack()
    {
        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
            if (battleManager == null) return;
        }

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
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

        if (battleManager != null)
        {
            battleManager.EndEnemyTurn();
        }
    }

    void UpdateUI()
    {
        int maxHealth = (data != null) ? data.maxHp : 100;

        if (hpBarFill != null)
            hpBarFill.fillAmount = (float)currentHp / maxHealth;

        if (hpText != null)
            hpText.text = $"{currentHp} / {maxHealth}";
    }

    IEnumerator HitEffect()
    {
        if (enemyImage != null) enemyImage.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (enemyImage != null) enemyImage.color = Color.white;

        Vector3 originalPos = transform.position;
        transform.position = originalPos + (Vector3)Random.insideUnitCircle * 10f;
        yield return new WaitForSeconds(0.05f);
        transform.position = originalPos;
    }

    void Die()
    {
        string targetName = (data != null) ? data.enemyName : "Unknown Enemy";
        Debug.Log($"{targetName} 처치됨!");
        // gameObject.SetActive(false); // 승리 연출을 위해 잠시 주석 처리 (필요 시 해제)
    }
}