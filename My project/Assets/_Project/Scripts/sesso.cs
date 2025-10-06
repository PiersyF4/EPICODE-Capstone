using UnityEngine;

public class sesso : MonoBehaviour
{
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogWarning("Manca il Rigidbody2D su " + name);
    }

    void FixedUpdate()
    {
        var tr = transform.position;
        var rbp = rb ? (Vector2)rb.position : Vector2.zero;
        Debug.Log($"{name}  rb.pos={rbp}  tr.pos={tr}");
    }
}
