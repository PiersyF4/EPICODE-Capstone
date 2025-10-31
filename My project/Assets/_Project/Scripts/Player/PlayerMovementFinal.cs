using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementFinal : MonoBehaviour
{
    [SerializeField]
    private float speed = 5f; // Velocità di movimento verso destra quando premuto input

    private Rigidbody2D rb;
    private float moveInput; // 0 = fermo, >0 = destra

    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Consigliato per i character 2D per evitare ribaltamenti
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // Legge l'input orizzontale e ignora completamente quello verso sinistra
        // Usa GetAxisRaw per risposta immediata (0, 1 o -1 tipicamente)
        moveInput = Mathf.Max(0f, Input.GetAxisRaw("Horizontal"));
    }

    private void FixedUpdate()
    {
        // Se il giocatore preme destra, si muove (destra); se preme sinistra o nulla, resta fermo.
        float targetVx = moveInput * speed;
        rb.velocity = new Vector2(targetVx, rb.velocity.y);
    }

    private void OnValidate()
    {
        // Evita valori negativi in Inspector o da codice
        if (speed < 0f) speed = 0f;
    }
}