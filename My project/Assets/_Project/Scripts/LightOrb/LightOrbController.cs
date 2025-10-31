using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightOrbController : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("Player a cui andare/orbitare. Se vuoto, verrà cercato per Tag.")]
    public Transform player;
    [Tooltip("Tag del Player da cercare se 'player' è vuoto.")]
    public string playerTag = "Player";

    [Header("Avvicinamento")]
    [Tooltip("Velocità con cui la LightOrb si avvicina al Player.")]
    public float seekSpeed = 6f;
    [Tooltip("Distanza dal Player alla quale passare in modalità orbita.")]
    public float arriveDistance = 1.5f;

    [Header("Orbita")]
    [Tooltip("Raggio dell'orbita attorno al Player.")]
    public float orbitRadius = 2f;
    [Tooltip("Velocità angolare dell'orbita (gradi/secondo).")]
    public float orbitAngularSpeed = 90f;
    [Tooltip("Asse dell'orbita (in 2D usare Z+).")]
    public Vector3 orbitAxis = Vector3.forward;
    [Tooltip("Se true, ruota in senso orario (2D).")]
    public bool clockwise = true;
    [Tooltip("Offset del centro orbita lungo l'asse di orbita.")]
    public float orbitAxisOffset = 0.5f;
    [Tooltip("Se true, l'orb guarda il Player (rotazione in Z per 2D).")]
    public bool facePlayer = true;
    [Tooltip("Offset gradi sulla Z per allineare lo sprite.")]
    public float rotationOffsetZ = 0f;
    [Tooltip("Velocità con cui il raggio corrente converge verso 'orbitRadius'.")]
    public float orbitRadiusSmoothing = 4f;

    [Header("Orientamento durante l'avvicinamento")]
    [Tooltip("Durante l'avvicinamento orienta verso la tangente dell'orbita.")]
    public bool aimOrbitDuringSeek = true;
    [Tooltip("Velocità di allineamento rotazione (gradi/secondo).")]
    public float rotationSpeed = 360f;

    [Header("End Target (comportamento richiesto)")]
    [Tooltip("Riferimento del GameObject di fine livello (tag 'End').")]
    public Transform endTarget;
    [Tooltip("Tag del GameObject di fine livello da cercare se 'endTarget' è vuoto.")]
    public string endTag = "End";
    [Tooltip("Distanza Player-End alla quale l'orb scompare.")]
    public float hideAtPlayerEndDistance = 10f;
    [Tooltip("Distanza Player-End oltre la quale l'orb sta più lontana dal Player.")]
    public float farPlayerEndDistance = 60f;
    [Tooltip("Raggio di orbita quando il Player è molto lontano da End.")]
    public float orbitRadiusFar = 3f;
    [Tooltip("Raggio di orbita quando il Player è molto vicino a End.")]
    public float orbitRadiusNear = 0.3f;

    private enum State { Idle, Seeking, Orbiting }
    private State _state = State.Idle;

    // Base ortonormale del piano di orbita
    private Vector3 _axisN;     // orbitAxis normalizzato
    private Vector3 _refX;      // primo asse sul piano
    private Vector3 _refY;      // secondo asse sul piano

    private float _angleDeg;    // angolo corrente in gradi
    private float _currentRadius;

    private bool _hasDisappeared = false;

    // Start is called before the first frame update
    void Start()
    {
        // Assicura un riferimento al player
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) player = go.transform;
        }

        // Assicura un riferimento a End
        if (endTarget == null && !string.IsNullOrEmpty(endTag))
        {
            var endGo = GameObject.FindGameObjectWithTag(endTag);
            if (endGo != null) endTarget = endGo.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("[LightOrbController] Player non trovato. Rimane in Idle.");
            _state = State.Idle;
            return;
        }

        // Prepara base di orbita
        BuildOrbitBasis();

        // Inizia in modalità Seeking
        _state = State.Seeking;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
            return;

        // Adatta raggio e visibilità in base alla distanza Player-End
        UpdateEndProximityEffects();
        if (_hasDisappeared) return;

        switch (_state)
        {
            case State.Seeking:
                TickSeeking();
                break;
            case State.Orbiting:
                TickOrbiting();
                break;
        }
    }

    private void TickSeeking()
    {
        // Muove verso la posizione attuale del player
        Vector3 targetPos = player.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, seekSpeed * Time.deltaTime);

        // Passa all'orbita quando abbastanza vicino
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= arriveDistance)
        {
            EnterOrbit();
        }

        // Orientamento: durante l'avvicinamento punta già la tangente dell'orbita futura (smooth)
        if (aimOrbitDuringSeek)
        {
            // Centro orbita seguendo il player
            Vector3 center = player.position + _axisN * orbitAxisOffset;

            // Vettore dal centro all'orb (proiettato sul piano di orbita)
            Vector3 r = transform.position - center;
            Vector3 rPlanar = Vector3.ProjectOnPlane(r, _axisN);
            if (rPlanar.sqrMagnitude < 1e-6f)
            {
                // fallback per evitare direzione indefinita
                rPlanar = _refX;
            }

            // Tangente dell'orbita: cross(asse, r) => CCW; per orario invertire
            Vector3 tangent = Vector3.Cross(_axisN, rPlanar).normalized;
            if (clockwise) tangent = -tangent;

            // Ruota in Z verso la tangente con smoothing
            float zTarget = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg + rotationOffsetZ;
            float zNow = transform.eulerAngles.z;
            float zNew = Mathf.MoveTowardsAngle(zNow, zTarget, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.AngleAxis(zNew, Vector3.forward);
        }
        else if (facePlayer)
        {
            Vector3 dir = (player.position - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffsetZ;
                transform.rotation = Quaternion.AngleAxis(z, Vector3.forward);
            }
        }
    }

    private void TickOrbiting()
    {
        // Aggiorna angolo (clockwise in 2D => angolo decresce)
        float direction = clockwise ? -1f : 1f;
        _angleDeg += direction * orbitAngularSpeed * Time.deltaTime;

        // Centro orbita segue il player
        Vector3 center = player.position + _axisN * orbitAxisOffset;

        // Converge gradualmente al raggio desiderato
        _currentRadius = Mathf.MoveTowards(_currentRadius, orbitRadius, orbitRadiusSmoothing * Time.deltaTime);

        // Calcola posizione sull'orbita
        float rad = _angleDeg * Mathf.Deg2Rad;
        Vector3 offset = (_refX * Mathf.Cos(rad) + _refY * Mathf.Sin(rad)) * _currentRadius;
        Vector3 desiredPos = center + offset;

        transform.position = desiredPos;

        if (facePlayer)
        {
            Vector3 dir = (player.position - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffsetZ;
                transform.rotation = Quaternion.AngleAxis(z, Vector3.forward);
            }
        }
    }

    private void EnterOrbit()
    {
        // Costruisce base di orbita se l'asse è cambiato
        BuildOrbitBasis();

        // Proietta la posizione corrente sul piano ortogonale all'asse per ricavare angolo e raggio iniziali
        Vector3 center = player.position + _axisN * orbitAxisOffset;
        Vector3 r = transform.position - center;
        Vector3 rPlanar = Vector3.ProjectOnPlane(r, _axisN);

        // Se siamo quasi sull'asse, posiziona su refX
        if (rPlanar.sqrMagnitude < 1e-6f)
            rPlanar = _refX * Mathf.Max(orbitRadius, 0.001f);

        _currentRadius = Mathf.Max(orbitRadius, rPlanar.magnitude);

        // Angolo a partire dalla base {refX, refY}
        Vector3 rn = rPlanar.normalized;
        float x = Vector3.Dot(rn, _refX);
        float y = Vector3.Dot(rn, _refY);
        _angleDeg = Mathf.Atan2(y, x) * Mathf.Rad2Deg;

        _state = State.Orbiting;
    }

    private void BuildOrbitBasis()
    {
        _axisN = (orbitAxis.sqrMagnitude < 1e-6f) ? Vector3.forward : orbitAxis.normalized;

        // Trova un vettore non parallelo all'asse per costruire refX
        Vector3 any = Mathf.Abs(Vector3.Dot(_axisN, Vector3.forward)) < 0.99f ? Vector3.forward : Vector3.right;
        _refX = Vector3.ProjectOnPlane(any, _axisN).normalized;
        if (_refX.sqrMagnitude < 1e-6f) _refX = Vector3.right; // fallback

        _refY = Vector3.Cross(_axisN, _refX).normalized;
    }

    // Aggiorna raggio/arrivo in base alla distanza Player-End e gestisce la scomparsa
    private void UpdateEndProximityEffects()
    {
        if (_hasDisappeared) return;
        if (endTarget == null || player == null) return;

        float d = Vector3.Distance(player.position, endTarget.position);

        // Scomparsa quando il Player è a ~10 dal target End
        if (d <= hideAtPlayerEndDistance)
        {
            _hasDisappeared = true;
            gameObject.SetActive(false);
            return;
        }

        // Mappa la distanza Player-End al raggio desiderato: più vicino a End => raggio più piccolo
        // t = 1 quando lontano, t = 0 quando vicino alla soglia di hide
        float t = Mathf.InverseLerp(hideAtPlayerEndDistance, farPlayerEndDistance, d);
        float desiredOrbitRadius = Mathf.Lerp(orbitRadiusNear, orbitRadiusFar, t);

        // Aggiorna dinamicamente il raggio e la distanza di arrivo per far entrare in orbita a quel raggio
        orbitRadius = desiredOrbitRadius;
        arriveDistance = Mathf.Max(desiredOrbitRadius, 0.05f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // Disegna raggio di arrivo (seeking) e orbita prevista
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(player.position, arriveDistance);

        // Base di disegno orbita
        Vector3 axisN = (orbitAxis.sqrMagnitude < 1e-6f) ? Vector3.forward : orbitAxis.normalized;
        Vector3 any = Mathf.Abs(Vector3.Dot(axisN, Vector3.forward)) < 0.99f ? Vector3.forward : Vector3.right;
        Vector3 rx = Vector3.ProjectOnPlane(any, axisN).normalized;
        Vector3 ry = Vector3.Cross(axisN, rx).normalized;

        Vector3 center = player.position + axisN * orbitAxisOffset;
        const int seg = 64;
        Vector3 prev = center + rx * orbitRadius;
        for (int i = 1; i <= seg; i++)
        {
            float t = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = center + (rx * Mathf.Cos(t) + ry * Mathf.Sin(t)) * orbitRadius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
#endif
}