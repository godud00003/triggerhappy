using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class CardAnimator : MonoBehaviour
{
    [Header("드로우 애니메이션")]
    public float drawDuration = 0.35f;
    public float startScale = 0.5f;
    public float overshootScale = 1.1f;

    [Header("묘지 이동 애니메이션")]
    public float discardDuration = 0.4f;
    public float discardEndScale = 0.1f;

    [Header("셔플 애니메이션 (묘지->덱)")]
    public float shuffleDuration = 0.5f;

    [Header("장전 애니메이션 (손->슬롯)")]
    public float loadDuration = 0.2f;
    public float loadScale = 1.0f;

    [Header("발사 애니메이션 (슬롯->적)")]
    public float fireDuration = 0.2f;  // 발사 속도
    public float fireScaleX = 0.6f;    // 날아갈 때 홀쭉해짐
    public float fireScaleY = 1.4f;    // 날아갈 때 길어짐

    [Header("곡선 설정")]
    public float curveHeight = 100f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private bool isAnimating = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        originalScale = Vector3.one;
    }

    #region [Draw, Discard, Shuffle, Load]
    // 기존 로직 유지
    public void PlayDrawAnimation(RectTransform deckPos, RectTransform handArea, Action onComplete = null)
    {
        if (isAnimating) return;
        StartCoroutine(DrawFromDeckRoutine(deckPos, handArea, onComplete));
    }
    public void PlayDrawAnimation(Vector3 startWorldPos, Action onComplete = null)
    {
        if (isAnimating) return;
        StartCoroutine(DrawSimpleRoutine(startWorldPos, onComplete));
    }
    public void PlayDiscardAnimation(Vector3 graveWorldPos, Action onComplete = null)
    {
        if (isAnimating) StopAllCoroutines();
        StartCoroutine(DiscardRoutine(graveWorldPos, onComplete));
    }
    public void PlayShuffleAnimation(Vector3 fromPos, Vector3 toPos, Action onComplete = null)
    {
        if (isAnimating) StopAllCoroutines();
        StartCoroutine(ShuffleRoutine(fromPos, toPos, onComplete));
    }
    public void PlayLoadAnimation(RectTransform slotTransform, Action onComplete = null)
    {
        if (isAnimating) StopAllCoroutines();
        StartCoroutine(LoadRoutine(slotTransform, onComplete));
    }
    #endregion

    #region [Fire Animation - Vanish Removed]
    public void PlayFireAnimation(Vector3 targetWorldPos, Action onComplete = null)
    {
        if (isAnimating) StopAllCoroutines();
        StartCoroutine(FireRoutine(targetWorldPos, onComplete));
    }

    IEnumerator FireRoutine(Vector3 targetPos, Action onComplete)
    {
        isAnimating = true;

        // 부모를 최상위 캔버스로 변경 (실린더 회전 영향 안 받게)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) transform.SetParent(canvas.rootCanvas.transform, true);

        Vector3 startPos = transform.position;

        // 방향 계산 (타겟을 향해 회전)
        Vector3 direction = (targetPos - startPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(0, 0, angle - 90);

        float elapsed = 0f;
        while (elapsed < fireDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fireDuration;
            float smoothT = t * t; // EaseIn (점점 빠르게)

            transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, t * 10f);

            // 속도감 연출 (길쭉해짐)
            Vector3 stretchScale = new Vector3(fireScaleX, fireScaleY, 1f);
            transform.localScale = Vector3.Lerp(Vector3.one, stretchScale, t);

            yield return null;
        }

        isAnimating = false;
        onComplete?.Invoke();
    }
    #endregion

    #region [Coroutines Implementation]
    // 기존 코루틴들 (Load, Shuffle, Discard, Draw...)

    IEnumerator LoadRoutine(RectTransform slotTransform, Action onComplete)
    {
        isAnimating = true;
        transform.SetParent(slotTransform, true);
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        Vector3 startScaleVec = transform.localScale;
        float elapsed = 0f;
        while (elapsed < loadDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / loadDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, smoothT);
            transform.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, smoothT);
            transform.localScale = Vector3.Lerp(startScaleVec, Vector3.one * loadScale, smoothT);
            yield return null;
        }
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * loadScale;
        isAnimating = false;
        onComplete?.Invoke();
    }

    IEnumerator ShuffleRoutine(Vector3 fromPos, Vector3 toPos, Action onComplete)
    {
        isAnimating = true;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) transform.SetParent(canvas.rootCanvas.transform, true);
        transform.position = fromPos;
        transform.localScale = Vector3.one * 0.5f;
        canvasGroup.alpha = 1f;
        Vector3 startPos = fromPos + new Vector3(UnityEngine.Random.Range(-50f, 50f), UnityEngine.Random.Range(-50f, 50f), 0);
        transform.position = startPos;
        float elapsed = 0f;
        while (elapsed < shuffleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shuffleDuration;
            float smoothT = t * t * (3f - 2f * t);
            Vector3 currentPos = Vector3.Lerp(startPos, toPos, smoothT);
            float arc = Mathf.Sin(t * Mathf.PI) * (curveHeight * 0.8f);
            currentPos.y += arc;
            transform.position = currentPos;
            transform.Rotate(0, 0, 720f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one * 0.2f, smoothT);
            if (t > 0.8f) canvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.8f) * 5f);
            yield return null;
        }
        isAnimating = false;
        onComplete?.Invoke();
    }

    IEnumerator DiscardRoutine(Vector3 gravePos, Action onComplete)
    {
        isAnimating = true;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) transform.SetParent(canvas.rootCanvas.transform, true);
        Vector3 startPos = transform.position;
        Vector3 startScaleVec = transform.localScale;
        float elapsed = 0f;
        while (elapsed < discardDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / discardDuration;
            float smoothT = t * t;
            Vector3 currentPos = Vector3.Lerp(startPos, gravePos, smoothT);
            float arc = Mathf.Sin(t * Mathf.PI) * (curveHeight * 0.5f);
            currentPos.y += arc;
            transform.position = currentPos;
            transform.Rotate(0, 0, -360f * Time.deltaTime * 2f);
            transform.localScale = Vector3.Lerp(startScaleVec, Vector3.one * discardEndScale, smoothT);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            yield return null;
        }
        isAnimating = false;
        onComplete?.Invoke();
    }

    IEnumerator DrawFromDeckRoutine(RectTransform deckPos, RectTransform handArea, Action onComplete)
    {
        isAnimating = true;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        Transform originalParent = transform.parent;
        int originalSiblingIndex = transform.GetSiblingIndex();
        if (canvas != null) transform.SetParent(canvas.rootCanvas.transform, true);
        Vector3 startPos = deckPos.position;

        // [Fix] endPos 변수 선언 추가
        Vector3 endPos = handArea.position;

        transform.position = startPos;
        transform.localScale = originalScale * startScale;
        transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-20f, 20f));
        canvasGroup.alpha = 0.8f;
        float elapsed = 0f;
        while (elapsed < drawDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / drawDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothT);
            float arc = Mathf.Sin(t * Mathf.PI) * curveHeight;
            currentPos.y += arc;
            transform.position = currentPos;
            float scale = Mathf.Lerp(startScale, 1f, smoothT);
            transform.localScale = originalScale * scale;
            float rotZ = Mathf.Lerp(transform.rotation.eulerAngles.z, 0, smoothT * 2f);
            transform.rotation = Quaternion.Euler(0, 0, rotZ > 180 ? rotZ - 360 : rotZ);
            canvasGroup.alpha = Mathf.Lerp(0.8f, 1f, smoothT);
            yield return null;
        }
        yield return StartCoroutine(BounceRoutine());
        if (canvas != null && transform.parent == canvas.rootCanvas.transform)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;
        canvasGroup.alpha = 1f;
        isAnimating = false;
        onComplete?.Invoke();
    }

    IEnumerator DrawSimpleRoutine(Vector3 startWorldPos, Action onComplete)
    {
        isAnimating = true;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        Transform originalParent = transform.parent;
        int originalSiblingIndex = transform.GetSiblingIndex();
        if (canvas != null) transform.SetParent(canvas.rootCanvas.transform, true);

        // [Fix] endPos 변수가 여기서도 필요할 수 있음 (현재 위치를 endPos로)
        Vector3 endPos = transform.position;

        transform.position = startWorldPos;
        transform.localScale = originalScale * startScale;
        transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-20f, 20f));
        canvasGroup.alpha = 0.8f;
        float elapsed = 0f;
        while (elapsed < drawDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / drawDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // [Fix] endPos 변수 사용
            Vector3 currentPos = Vector3.Lerp(startWorldPos, endPos, smoothT);

            float arc = Mathf.Sin(t * Mathf.PI) * curveHeight;
            currentPos.y += arc;
            transform.position = currentPos;
            float scale = Mathf.Lerp(startScale, 1f, smoothT);
            transform.localScale = originalScale * scale;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, smoothT * 2f);
            canvasGroup.alpha = Mathf.Lerp(0.8f, 1f, smoothT);
            yield return null;
        }
        yield return StartCoroutine(BounceRoutine());
        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;
        canvasGroup.alpha = 1f;
        isAnimating = false;
        onComplete?.Invoke();
    }

    IEnumerator BounceRoutine()
    {
        float duration = 0.12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * (overshootScale - 1f);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        transform.localScale = originalScale;
    }

    public void ShowImmediate()
    {
        StopAllCoroutines();
        canvasGroup.alpha = 1f;
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;
        isAnimating = false;
    }
    public void PunchScale(float punchAmount = 1.1f, float duration = 0.1f)
    {
        if (isAnimating) return;
        StartCoroutine(PunchScaleRoutine(punchAmount, duration));
    }
    IEnumerator PunchScaleRoutine(float punchAmount, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * (punchAmount - 1f);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        transform.localScale = originalScale;
    }
    #endregion
}