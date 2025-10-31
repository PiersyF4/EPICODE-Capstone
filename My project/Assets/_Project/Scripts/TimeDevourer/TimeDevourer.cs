using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class TimeDevourer : MonoBehaviour
{
    public enum Mode { ConstantSpeed, FollowPlayer }

    [Header("Modalità")]
    public Mode mode = Mode.FollowPlayer;
    public Transform target;
    public float baseSpeed = 2.4f;
    public float maxSpeed = 5f;
    public float minDistance = 6f;
    public float catchupDistance = 12f;
    public bool lockY = true;
    public float fixedY = 0f;
    public float startOffsetX = -6f;

    [Header("Danno al contatto")]
    public int touchDamage = 9999;
    [Tooltip("Lascia vuoto per colpire tutto. Se impostato, colpisce solo quel tag.")]
    public string damageTargetTag = "Player";

    [Header("Eventi")]
    public UnityEvent onPlayerConsumed;

    Rigidbody2D rb;
    Collider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // setup sicuro
        col.isTrigger = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;
    }

    void Start()
    {
        if (target && mode == Mode.FollowPlayer)
        {
            var p = transform.position;
            p.x = target.position.x + startOffsetX;
            if (lockY) p.y = fixedY;
            transform.position = p;
        }
    }

    void FixedUpdate()
    {
        float speed = baseSpeed;

        if (mode == Mode.FollowPlayer && target)
        {
            float dist = Mathf.Abs(target.position.x - transform.position.x);
            float t = Mathf.InverseLerp(minDistance, catchupDistance, dist);
            speed = Mathf.Lerp(baseSpeed, maxSpeed, t);

            if (!lockY)
            {
                float y = Mathf.Lerp(transform.position.y, target.position.y, 2f * Time.fixedDeltaTime);
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
            }
            else if (Mathf.Abs(transform.position.y - fixedY) > 0.001f)
            {
                transform.position = new Vector3(transform.position.x, fixedY, transform.position.z);
            }
        }

        Vector3 next = transform.position + Vector3.right * speed * Time.fixedDeltaTime;
        rb.MovePosition(next);
    }

    // Il danno è stato spostato sugli eventi di trigger per evitare kill senza contatto.
    void Update() { }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryConsume(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Copre i casi in cui si nasce già in sovrapposizione o si perde l'Enter
        TryConsume(other);
    }

    void TryConsume(Collider2D other)
    {
        // rispetta il filtro tag, se impostato
        if (!string.IsNullOrEmpty(damageTargetTag) && !other.CompareTag(damageTargetTag))
            return;

        // se è impostato un target, limita il consumo a quel target (root)
        if (target && other.transform.root != target.root)
            return;

        // prova a prendere il Damageable dal target (root o parent)
        var hp = other.GetComponent<Damageable>();
        if (hp == null) hp = other.GetComponentInParent<Damageable>();

        if (hp != null && hp.CurrentHealth > 0)
        {
            // kill immediata
            hp.SetHealth(0);
            if (other.CompareTag("Player"))
                onPlayerConsumed?.Invoke();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var a = transform.position + Vector3.up * 20f;
        var b = transform.position + Vector3.down * 20f;
        Gizmos.DrawLine(a, b);
    }
}
