using System.Collections;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Configurazione movimento")]
    [SerializeField] private float startY = 20f;
    [SerializeField] private float endY = 0f;
    [SerializeField, Min(0f)] private float durationSeconds = 5f;
    [SerializeField] private bool autoStartOnPlay = false;
    [SerializeField] private bool forceStartPositionOnPlay = true;
    [SerializeField, Min(0f)] private float waitAtEndsSeconds = 0f; // pausa ai capi

    private Coroutine _moveRoutine;

    private void Start()
    {
        if (forceStartPositionOnPlay)
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, startY, p.z);
        }

        if (autoStartOnPlay)
        {
            StartMovement();    
        }
    }

    public void StartMovement()
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
        }
        _moveRoutine = StartCoroutine(MoveLoop());
    }

    public void StartMovement(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        durationSeconds = seconds;
        StartMovement();
    }

    public void StopMovement()
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }
    }

    private IEnumerator MoveLoop()
    {
        while (true)
        {
            yield return MoveFromTo(startY, endY, durationSeconds);
            if (waitAtEndsSeconds > 0f) yield return new WaitForSeconds(waitAtEndsSeconds);

            yield return MoveFromTo(endY, startY, durationSeconds);
            if (waitAtEndsSeconds > 0f) yield return new WaitForSeconds(waitAtEndsSeconds);
        }
    }

    private IEnumerator MoveFromTo(float fromY, float toY, float seconds)
    {
        var pos = transform.position;

        if (seconds <= 0f)
        {
            transform.position = new Vector3(pos.x, toY, pos.z);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            float x = Mathf.Lerp(fromY, toY, t);
            transform.position = new Vector3(pos.x, x, pos.z);
            yield return null;
        }

        transform.position = new Vector3(pos.x, toY, pos.z);
    }
}
