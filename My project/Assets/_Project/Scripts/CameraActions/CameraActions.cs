using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraActions : MonoBehaviour
{
    [Header("Riferimenti")]
    public Camera targetCamera;

    [Header("Generali")]
    public bool useUnscaledTime = true;

    [Header("Telecamera Morte")]
    public float deathZoomFactor = 0.6f;      // < 1 => zoom-in
    public float deathZoomDuration = 0.35f;
    public float deathHoldAfter = 0.15f;

    [Header("Telecamera Caduta")]
    public float fallDuration = 0.6f;
    public float fallZoomFactor = 0.8f;       // < 1 => zoom-in
    public float fallShakePosAmplitude = 0.3f;
    public float fallShakeRotAmplitude = 1.5f; // gradi
    public float fallShakeFrequency = 35f;     // Hz “perlin”

    [Header("Telecamera Fine")]
    public float endDuration = 0.9f;
    public float endZoomFactor = 0.5f;        // < 1 => zoom-in
    public float endShakePosAmplitude = 0.35f;
    public float endShakeRotAmplitude = 3.0f; // gradi
    public float endSpinSpeed = 280f;         // gradi/sec

    [Header("Telecamera Inghiottimento")]
    public float swallowDuration = 0.7f;
    public float swallowShakePosAmplitude = 0.28f;
    public float swallowShakeRotAmplitude = 2.0f; // gradi
    public float swallowShakeFrequency = 30f;     // Hz “perlin”

    Transform camT;
    bool isPlaying;

    void Awake()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (targetCamera) camT = targetCamera.transform;
    }

    // 1) Telecamera di morte: rapido zoom poi cambio scena
    public void PlayDeathCamera(string sceneName = null)
    {
        if (!Validate()) return;
        if (isPlaying) StopAllCoroutines();
        StartCoroutine(CoDeath(sceneName));
    }

    // 2) Telecamera di caduta: trema con zoom e cambio scena
    public void PlayFallCamera(string sceneName = null)
    {
        if (!Validate()) return;
        if (isPlaying) StopAllCoroutines();
        StartCoroutine(CoFall(sceneName));
    }

    // 3) Telecamera di fine: ruota, trema, zoom, poi cambio scena
    public void PlayEndCamera(string sceneName = null)
    {
        if (!Validate()) return;
        if (isPlaying) StopAllCoroutines();
        StartCoroutine(CoEnd(sceneName));
    }

    // 4) Telecamera di inghiottimento: trema e cambio scena
    public void PlaySwallowCamera(string sceneName = null)
    {
        if (!Validate()) return;
        if (isPlaying) StopAllCoroutines();
        StartCoroutine(CoSwallow(sceneName));
    }

    // ====== Coroutines specifiche ======
    System.Collections.IEnumerator CoDeath(string sceneName)
    {
        isPlaying = true;

        float startZoom = GetZoom();
        float targetZoom = Mathf.Max(0.01f, startZoom * deathZoomFactor);
        float t = 0f;

        while (t < deathZoomDuration)
        {
            float dt = DeltaTime();
            t += dt;
            float k = Mathf.Clamp01(t / deathZoomDuration);
            SetZoom(Mathf.Lerp(startZoom, targetZoom, EaseOutQuad(k)));
            yield return null;
        }

        if (deathHoldAfter > 0f)
            yield return WaitForSecondsRealtimeSafe(deathHoldAfter);

        LoadSceneSafe(sceneName);
        isPlaying = false;
    }

    System.Collections.IEnumerator CoFall(string sceneName)
    {
        isPlaying = true;

        float startZoom = GetZoom();
        float targetZoom = Mathf.Max(0.01f, startZoom * fallZoomFactor);

        Vector3 basePos = camT.localPosition;
        float baseZ = camT.localEulerAngles.z;

        float t = 0f;
        float seed = Random.value * 1000f;

        while (t < fallDuration)
        {
            float dt = DeltaTime();
            t += dt;
            float k = Mathf.Clamp01(t / fallDuration);

            // Zoom
            SetZoom(Mathf.Lerp(startZoom, targetZoom, EaseOutQuad(k)));

            // Shake (perlin)
            Vector2 n = Perlin2(seed, t * fallShakeFrequency);
            Vector3 posOffset = new Vector3(n.x, n.y, 0f) * fallShakePosAmplitude * (1f - k); // decresce verso la fine
            float rotOffset = (Perlin1(seed + 77.7f, t * fallShakeFrequency) * 2f - 1f) * fallShakeRotAmplitude * (1f - k);

            camT.localPosition = basePos + posOffset;
            camT.localRotation = Quaternion.Euler(0f, 0f, baseZ + rotOffset);

            yield return null;
        }

        // Ripristina prima del cambio scena
        camT.localPosition = basePos;
        camT.localRotation = Quaternion.Euler(0f, 0f, baseZ);

        LoadSceneSafe(sceneName);
        isPlaying = false;
    }

    System.Collections.IEnumerator CoEnd(string sceneName)
    {
        isPlaying = true;

        float startZoom = GetZoom();
        float targetZoom = Mathf.Max(0.01f, startZoom * endZoomFactor);

        Vector3 basePos = camT.localPosition;
        float baseZ = camT.localEulerAngles.z;

        float t = 0f;
        float seed = Random.value * 1000f;

        while (t < endDuration)
        {
            float dt = DeltaTime();
            t += dt;
            float k = Mathf.Clamp01(t / endDuration);

            // Zoom
            SetZoom(Mathf.Lerp(startZoom, targetZoom, EaseInOutQuad(k)));

            // Shake
            Vector2 n = Perlin2(seed, t * endSpinSpeed * 0.1f);
            Vector3 posOffset = new Vector3(n.x, n.y, 0f) * endShakePosAmplitude * (1f - k);
            float shakeRot = (Perlin1(seed + 33.3f, t * endSpinSpeed * 0.1f) * 2f - 1f) * endShakeRotAmplitude * (1f - k);

            // Spin
            float spin = endSpinSpeed * t;

            camT.localPosition = basePos + posOffset;
            camT.localRotation = Quaternion.Euler(0f, 0f, baseZ + spin + shakeRot);

            yield return null;
        }

        // Ripristina prima del cambio scena
        camT.localPosition = basePos;
        camT.localRotation = Quaternion.Euler(0f, 0f, baseZ);

        LoadSceneSafe(sceneName);
        isPlaying = false;
    }

    System.Collections.IEnumerator CoSwallow(string sceneName)
    {
        isPlaying = true;

        Vector3 basePos = camT.localPosition;
        float baseZ = camT.localEulerAngles.z;

        float t = 0f;
        float seed = Random.value * 1000f;

        while (t < swallowDuration)
        {
            float dt = DeltaTime();
            t += dt;
            float k = Mathf.Clamp01(t / swallowDuration);

            // Shake
            Vector2 n = Perlin2(seed, t * swallowShakeFrequency);
            Vector3 posOffset = new Vector3(n.x, n.y, 0f) * swallowShakePosAmplitude * (1f - k);
            float rotOffset = (Perlin1(seed + 55.5f, t * swallowShakeFrequency) * 2f - 1f) * swallowShakeRotAmplitude * (1f - k);

            camT.localPosition = basePos + posOffset;
            camT.localRotation = Quaternion.Euler(0f, 0f, baseZ + rotOffset);

            yield return null;
        }

        // Ripristina prima del cambio scena
        camT.localPosition = basePos;
        camT.localRotation = Quaternion.Euler(0f, 0f, baseZ);

        LoadSceneSafe(sceneName);
        isPlaying = false;
    }

    // ====== Utility ======
    bool Validate()
    {
        if (!targetCamera)
        {
            Debug.LogError("[CameraActions] Nessuna Camera assegnata.");
            return false;
        }
        if (!camT) camT = targetCamera.transform;
        return true;
    }

    float GetZoom()
    {
        return targetCamera.orthographic ? targetCamera.orthographicSize : targetCamera.fieldOfView;
    }

    void SetZoom(float v)
    {
        if (targetCamera.orthographic)
            targetCamera.orthographicSize = Mathf.Max(0.01f, v);
        else
            targetCamera.fieldOfView = Mathf.Clamp(v, 1f, 179f);
    }

    float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    System.Collections.IEnumerator WaitForSecondsRealtimeSafe(float seconds)
    {
        if (useUnscaledTime)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    // Easing
    static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

    // Perlin helpers (0..1)
    static float Perlin1(float seed, float t) => Mathf.PerlinNoise(seed, t);
    static Vector2 Perlin2(float seed, float t)
    {
        float nx = Mathf.PerlinNoise(seed + 1.123f, t) * 2f - 1f;
        float ny = Mathf.PerlinNoise(seed + 9.87f, t + 17.17f) * 2f - 1f;
        return new Vector2(nx, ny);
    }

    void LoadSceneSafe(string sceneName)
    {
        var current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(string.IsNullOrEmpty(sceneName) ? current : sceneName);
    }
}
