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
    public Color enemyDamageColor = new Color(1f, 0.4f, 0.4f); // 적이 때릴 때 (연한 빨강)

    [Header("카메라 설정 (자동 감지)")]
    public Camera worldCamera; // 3D/2D 월드 카메라
    public Camera uiCamera;    // UI 카메라 (Screen Space - Camera 모드일 때 필요)

    [Header("★ 오프셋 설정 (Inspector 조정)")]
    public Vector2 randomSpread = new Vector2(30f, 20f);       // 랜덤하게 흩뿌려질 범위

    [Tooltip("플레이어 머리 위로 얼마나 띄울지")]
    public Vector2 playerOffset = new Vector2(0f, 80f);

    [Tooltip("적 머리 위로 얼마나 띄울지")]
    public Vector2 enemyOffset = new Vector2(0f, 100f);

    private Canvas rootCanvas;
    private RectTransform canvasRect;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 1. 최상위 캔버스 찾기
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindFirstObjectByType<Canvas>();

        if (rootCanvas != null)
        {
            canvasRect = rootCanvas.transform as RectTransform;

            // 2. 카메라 자동 할당
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = null; // Overlay는 카메라 불필요
            }
            else
            {
                // Canvas Camera가 있으면 할당, 없으면 MainCamera를 UI카메라로 간주
                uiCamera = rootCanvas.worldCamera ? rootCanvas.worldCamera : Camera.main;
            }
        }

        if (worldCamera == null) worldCamera = Camera.main;
    }

    /// <summary>
    /// [메인 함수] 타겟(초상화 or 적 오브젝트) 위에 데미지 출력
    /// </summary>
    public void SpawnAtTransform(Transform targetAnchor, int damage, bool isCritical = false, bool isPlayerDamage = false)
    {
        if (targetAnchor == null || popupPrefab == null || rootCanvas == null) return;

        // 1. 캔버스 상의 정확한 좌표 계산
        Vector2 canvasPos = GetAccurateCanvasPosition(targetAnchor);

        // 2. 오프셋 적용 (플레이어/적 구분)
        Vector2 offset = isPlayerDamage ? playerOffset : enemyOffset;
        canvasPos += offset;
        canvasPos += GetRandomSpread();

        // 3. 팝업 생성 및 설정
        SpawnPopup(canvasPos, damage, isCritical, isPlayerDamage);
    }

    /// <summary>
    /// 힐 팝업 생성
    /// </summary>
    public void SpawnHeal(Transform targetAnchor, int amount)
    {
        if (targetAnchor == null || popupPrefab == null) return;

        Vector2 canvasPos = GetAccurateCanvasPosition(targetAnchor);

        // 힐은 보통 아군에게 쓰므로 playerOffset 사용 (필요시 healOffset 분리 가능)
        canvasPos += playerOffset;
        canvasPos += GetRandomSpread();

        GameObject popup = CreatePopupObject(canvasPos);
        DamagePopup popupScript = popup.GetComponent<DamagePopup>();

        if (popupScript != null)
        {
            popupScript.SetupHeal(amount);
        }
    }

    // ============ 내부 로직 ============

    /// <summary>
    /// 타겟이 UI인지 월드 객체인지 판단하여 캔버스 로컬 좌표로 변환
    /// </summary>
    Vector2 GetAccurateCanvasPosition(Transform target)
    {
        Vector3 screenPos = Vector3.zero;
        bool isUI = target.GetComponent<RectTransform>() != null;

        if (isUI)
        {
            // [CASE 1] UI 요소 (초상화 등)
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenPos = target.position;
            }
            else
            {
                // Screen Space - Camera 모드
                Camera cam = (uiCamera != null) ? uiCamera : Camera.main;
                if (cam != null) screenPos = cam.WorldToScreenPoint(target.position);
            }
        }
        else
        {
            // [CASE 2] 월드 오브젝트 (적 모델)
            if (worldCamera != null)
            {
                screenPos = worldCamera.WorldToScreenPoint(target.position);
            }
            else
            {
                screenPos = target.position;
            }
        }

        // 스크린 좌표 -> 캔버스 로컬 좌표 변환
        Vector2 localPos;
        Camera conversionCam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera;

        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && conversionCam == null)
            conversionCam = Camera.main;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            conversionCam,
            out localPos
        );

        return localPos;
    }

    GameObject CreatePopupObject(Vector2 canvasPos)
    {
        GameObject popup = Instantiate(popupPrefab, rootCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();

        // [Critical Fix] Z축 초기화, 스케일 초기화, 회전 초기화, 레이어 동기화
        popupRect.anchoredPosition3D = new Vector3(canvasPos.x, canvasPos.y, 0f);
        popup.transform.localScale = Vector3.one;
        popup.transform.localRotation = Quaternion.identity;
        popup.layer = rootCanvas.gameObject.layer;

        popup.transform.SetAsLastSibling();

        return popup;
    }

    void SpawnPopup(Vector2 canvasPos, int damage, bool isCritical, bool isPlayerDamage)
    {
        GameObject popup = CreatePopupObject(canvasPos);
        DamagePopup popupScript = popup.GetComponent<DamagePopup>();

        if (popupScript != null)
        {
            Color targetColor;
            if (isPlayerDamage) targetColor = enemyDamageColor;
            else targetColor = isCritical ? criticalDamageColor : normalDamageColor;

            popupScript.Setup(damage, targetColor, isCritical);
        }
    }

    Vector2 GetRandomSpread()
    {
        return new Vector2(
            Random.Range(-randomSpread.x, randomSpread.x),
            Random.Range(-randomSpread.y, randomSpread.y)
        );
    }
}