using System.Collections;
using UnityEngine;

public class ZoomInCamera : MonoBehaviour
{
    public Camera cam;                // 직교 카메라
    public float baseOrthoSize = 5f;  // 기본 사이즈
    public AnimationCurve inCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 인 커브
    public AnimationCurve outCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 아웃 커브
    Coroutine _co;

    void Reset()
    {
        cam = Camera.main;
        if (cam != null && cam.orthographic) baseOrthoSize = cam.orthographicSize;
    }

    public void KickZoomPercent(float zoomPercent, float tIn, float tHold, float tOut, bool overshoot = false, bool useUnscaledTime = true)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoZoom(zoomPercent, tIn, tHold, tOut, overshoot, useUnscaledTime));
    }

    IEnumerator CoZoom(float pct, float tIn, float tHold, float tOut, bool overshoot, bool unscaled)
    {
        if (!cam || !cam.orthographic) yield break;

        float start = cam.orthographicSize;
        float target = baseOrthoSize * (1f - pct); // pct=0.2 => 20% 줌인

        // 인(오버슛 옵션)
        float o = overshoot ? 0.02f : 0f; // 2% 오버슛
        float overTarget = target * (1f - o);
        float t = 0f;
        while (t < tIn)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / tIn);
            float e = inCurve.Evaluate(k); // OutBack 같은 커브를 세팅해두면 됨
            cam.orthographicSize = Mathf.Lerp(start, overTarget, e);
            yield return null;
        }

        // 홀드
        cam.orthographicSize = overTarget;
        if (tHold > 0f)
        {
            float hold = 0f;
            while (hold < tHold)
            {
                hold += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        // 아웃
        t = 0f;
        float outStart = cam.orthographicSize;
        while (t < tOut)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / tOut);
            float e = outCurve.Evaluate(k); // InOutCubic/OutExpo 등
            cam.orthographicSize = Mathf.Lerp(outStart, baseOrthoSize, e);
            yield return null;
        }
        cam.orthographicSize = baseOrthoSize;
        _co = null;
    }
}
