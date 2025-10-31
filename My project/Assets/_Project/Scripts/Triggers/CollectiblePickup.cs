using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CollectiblePickup : MonoBehaviour
{
    [Header("Item")]
    public string itemId = "Fragment";
    public int amount = 1;

    [Header("Feedback (opzionali)")]
    public AudioClip sfx;
    public GameObject vfxPrefab;
    public float destroyDelay = 0.0f;

    Collider2D col;
    SpriteRenderer sr;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        sr = GetComponent<SpriteRenderer>(); // ok se c’è, altrimenti ignora
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // trova l'inventario sul Player (o genitore)
        var inv = other.GetComponentInParent<PlayerInventory>();
        if (inv == null)
        {
            Debug.LogWarning("[CollectiblePickup] PlayerInventory non trovato sul Player.");
            return;
        }

        inv.AddItem(itemId, amount);

        // feedback
        if (sfx) AudioSource.PlayClipAtPoint(sfx, transform.position, 1f);
        if (vfxPrefab) Instantiate(vfxPrefab, transform.position, Quaternion.identity);

        // disattiva subito per evitare doppi trigger
        if (sr) sr.enabled = false;
        col.enabled = false;

        // distruggi
        if (destroyDelay <= 0f) Destroy(gameObject);
        else Destroy(gameObject, destroyDelay);
    }
}
