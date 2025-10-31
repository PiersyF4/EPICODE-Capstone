using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LeverControl : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private PlatformController platform; // assegna in Inspector

    [Header("Interazione")]
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool isOneShot = true;
    [SerializeField] private float overrideDurationSeconds = -1; // > 0 per forzare la durata al via

    bool playerInRange;
    bool fired = false;

    void Awake()
    {
        // Assicura che il collider sia trigger per rilevare il Player
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Facoltativo: prova ad auto-rilevare una piattaforma se non assegnata
        if (!platform)
            platform = FindObjectOfType<PlatformController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        playerInRange = false;
    }

    void Update()
    {
        if (!playerInRange) return;
        if (!Input.GetKeyDown(interactKey)) return;
        if (fired) return;
        
        platform.StartMovement();

        // Correzione: usa Quaternion.Euler per impostare la rotazione
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        fired = true;
    }
}
