using UnityEngine;
using TMPro;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("프리팹 연결")]
    public GameObject popupPrefab;

    [Header("기본 색상")]
    public Color normalDamageColor = Color.white;
    public Color criticalDamageColor = Color.yellow;
    public Color healColor = Color.green;
    public Color enemyDamageColor = Color.red;  // 적이 플레이어 때릴 때

    [Header("설정")]
    public Vector2 randomOffset = new Vector2(50f, 30f);   // 랜덤 위치 오프셋
    public Vector2 enemyPopupOffset = new Vector2(0f, 100f);  // ★ 적 데미지 오프셋 (머리 위)
    public Vector2 playerPopupOffset = new Vector2(0f, 50f);  // ★ 플레이어 데미지 오프셋 (초상화 위)

    private Canvas rootCanvas;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = FindFirstObjectByType<Canvas>();
        }
    }

    /// <summary>
    /// 월드 좌표 기준으로 데미지 팝업 생성
    /// </summary>
    public void SpawnAtWorldPosition(Vector3 worldPos, int damage, bool isCritical = false)
    {
        if (popupPrefab == null || rootCanvas == null) return;

        // 월드 -> 스크린 -> 캔버스 좌표 변환
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            screenPos,
            rootCanvas.worldCamera,
            out Vector2 canvasPos
        );

        SpawnPopup(canvasPos, damage, isCritical);
    }

    /// <summary>
    /// UI 요소(RectTransform) 위치에 데미지 팝업 생성
    /// </summary>
    public void SpawnAtUIPosition(RectTransform targetRect, int damage, bool isCritical = false)
    {
        if (popupPrefab == null || rootCanvas == null || targetRect == null) return;

        // UI 요소의 월드 좌표를 캔버스 로컬 좌표로 변환
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
            rootCanvas.worldCamera,
            targetRect.position
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            screenPos,
            rootCanvas.worldCamera,
            out Vector2 canvasPos
        );

        SpawnPopup(canvasPos, damage, isCritical);
    }

    /// <summary>
    /// Transform 위치에 데미지 팝업 생성 (3D/2D 호환)
    /// </summary>
    public void SpawnAtTransform(Transform target, int damage, bool isCritical = false, bool isPlayer = false)
    {
        if (target == null || popupPrefab == null || rootCanvas == null) return;

        // 팝업 생성
        GameObject popup = Instantiate(popupPrefab, rootCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();

        // ★ 핵심: Screen 좌표로 변환 후 사용
        Vector3 screenPos;

        // UI 카메라 확인 (Overlay면 null)
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        // 타겟의 월드 좌표를 스크린 좌표로
        if (uiCamera != null)
        {
            screenPos = uiCamera.WorldToScreenPoint(target.position);
        }
        else
        {
            // Overlay Canvas: 월드 좌표가 곧 스크린 좌표 (UI 요소의 경우)
            screenPos = target.position;
        }

        // 스크린 좌표를 캔버스 로컬 좌표로 변환
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            uiCamera,
            out localPos
        );

        // ★ 적/플레이어에 따라 다른 오프셋 적용
        Vector2 offset = isPlayer ? playerPopupOffset : enemyPopupOffset;
        popupRect.anchoredPosition = localPos + offset + GetRandomOffset();

        // 맨 앞으로
        popup.transform.SetAsLastSibling();

        // 데미지 표시 설정
        DamagePopup popupScript = popup.GetComponent<DamagePopup>();
        if (popupScript != null)
        {
            // ★ 플레이어 피격은 다른 색상 사용
            Color color;
            if (isPlayer)
                color = enemyDamageColor; // 플레이어가 맞은 데미지는 빨간색
            else
                color = isCritical ? criticalDamageColor : normalDamageColor;

            popupScript.Setup(damage, color, isCritical);
        }

        Debug.Log($"💥 [Popup] 타겟: {target.name}, isPlayer: {isPlayer}, 최종: {popupRect.anchoredPosition}");
    }

    /// <summary>
    /// 힐 팝업 생성
    /// </summary>
    public void SpawnHeal(Transform target, int amount)
    {
        if (popupPrefab == null || rootCanvas == null || target == null) return;

        Vector2 canvasPos = GetCanvasPosition(target);
        canvasPos += GetRandomOffset();

        GameObject popup = Instantiate(popupPrefab, rootCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchoredPosition = canvasPos;

        DamagePopup popupScript = popup.GetComponent<DamagePopup>();
        if (popupScript != null)
        {
            popupScript.SetupHeal(amount);
        }
    }

    // ============ Private Methods ============

    void SpawnPopup(Vector2 canvasPos, int damage, bool isCritical)
    {
        // ★ 디버그: 호출 확인
        Debug.Log($"💥 [DamagePopup] SpawnPopup 호출됨! 데미지: {damage}, 위치: {canvasPos}");

        if (popupPrefab == null)
        {
            Debug.LogError("⛔ [DamagePopup] popupPrefab이 연결되지 않았습니다!");
            return;
        }

        if (rootCanvas == null)
        {
            Debug.LogError("⛔ [DamagePopup] rootCanvas가 null입니다!");
            return;
        }

        // 랜덤 오프셋 추가 (같은 위치에 여러 개 생겨도 겹치지 않게)
        canvasPos += GetRandomOffset();

        GameObject popup = Instantiate(popupPrefab, rootCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchoredPosition = canvasPos;

        Debug.Log($"✅ [DamagePopup] 팝업 생성됨! 오브젝트: {popup.name}");

        DamagePopup popupScript = popup.GetComponent<DamagePopup>();
        if (popupScript != null)
        {
            Color color = isCritical ? criticalDamageColor : normalDamageColor;
            popupScript.Setup(damage, color, isCritical);
        }
        else
        {
            Debug.LogError("⛔ [DamagePopup] 프리팹에 DamagePopup 스크립트가 없습니다!");
        }
    }

    Vector2 GetCanvasPosition(Transform target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();

        if (rect != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                rootCanvas.worldCamera,
                rect.position
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                screenPos,
                rootCanvas.worldCamera,
                out Vector2 canvasPos
            );

            return canvasPos;
        }
        else
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(target.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                screenPos,
                rootCanvas.worldCamera,
                out Vector2 canvasPos
            );

            return canvasPos;
        }
    }

    Vector2 GetRandomOffset()
    {
        return new Vector2(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y)
        );
    }
}