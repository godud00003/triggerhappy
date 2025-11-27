using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("설정")]
    public float floatSpeed = 100f;      // 위로 올라가는 속도
    public float lifetime = 0.8f;        // 지속 시간
    public float fadeStartTime = 0.4f;   // 페이드 시작 시점

    [Header("연출")]
    public float scaleUpAmount = 1.3f;   // 초기 확대 크기
    public float scaleDownSpeed = 2f;    // 축소 속도

    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    private Color originalColor;
    private float elapsed = 0f;
    private Vector3 moveDirection;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();

        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void Setup(int damage, Color color, bool isCritical = false)
    {
        if (textMesh == null) return;

        // 텍스트 설정
        textMesh.text = damage.ToString();
        textMesh.color = color;
        originalColor = color;

        // 크리티컬이면 더 크고 강조
        if (isCritical)
        {
            textMesh.text = damage.ToString() + "!";
            transform.localScale = Vector3.one * scaleUpAmount * 1.3f;
            textMesh.fontStyle = FontStyles.Bold;
        }
        else
        {
            transform.localScale = Vector3.one * scaleUpAmount;
        }

        // 랜덤한 방향으로 튀어오르기 (약간의 좌우 흔들림)
        float randomX = Random.Range(-30f, 30f);
        moveDirection = new Vector3(randomX, floatSpeed, 0);
    }

    // 힐용 오버로드
    public void SetupHeal(int amount)
    {
        Setup(amount, Color.green, false);
        if (textMesh != null)
        {
            textMesh.text = "+" + amount.ToString();
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // 1. 위로 이동 (점점 느려짐)
        float speedMultiplier = Mathf.Lerp(1f, 0.3f, elapsed / lifetime);
        rectTransform.anchoredPosition += (Vector2)moveDirection * speedMultiplier * Time.deltaTime;

        // 2. 크기 축소 (팡! -> 원래 크기)
        float targetScale = Mathf.Lerp(scaleUpAmount, 1f, elapsed * scaleDownSpeed);
        transform.localScale = Vector3.one * Mathf.Max(targetScale, 1f);

        // 3. 페이드 아웃
        if (elapsed > fadeStartTime && textMesh != null)
        {
            float fadeProgress = (elapsed - fadeStartTime) / (lifetime - fadeStartTime);
            Color c = originalColor;
            c.a = Mathf.Lerp(1f, 0f, fadeProgress);
            textMesh.color = c;
        }

        // 4. 수명 종료
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}