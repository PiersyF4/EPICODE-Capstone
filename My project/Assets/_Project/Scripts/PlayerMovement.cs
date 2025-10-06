using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Fisica")]
    [SerializeField] private float desiredGravityScale = 3f;
    [SerializeField] private float fallGravityScale = 4.5f;      // gravità maggiore in discesa
    [SerializeField] private float earlyReleaseGravityScale = 5f; // gravità quando si rilascia presto

    [Header("Salto Base")]
    [SerializeField] private float initialJumpImpulse = 10f;      // impulso iniziale
    [SerializeField] private float maxHoldTime = 0.20f;           // per quanto tempo si può "caricare" il salto
    [SerializeField] private float holdForce = 18f;               // forza continua mentre si tiene premuto
    [SerializeField] private float maxUpwardSpeed = 14f;          // limite verticale per non “esplodere” in alto
    [SerializeField] private float jumpCutMultiplier = 0.5f;      // riduzione velocità verticale quando si rilascia presto

    [Header("Ground Check (semplice)")]
    [SerializeField] private float groundedVelocityTolerance = 0.05f;

    private Rigidbody2D rb;
    private Vector2 input;
    private float baseScaleX;

    // Stato salto
    private bool isHoldingJump;
    private bool hasStartedJump;
    private float holdTimer;

    // Stato animazioni
    private bool hasJumped;
    private bool isRunning;
    private bool isFalling;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScaleX = transform.localScale.x;
        rb.gravityScale = desiredGravityScale;
        rb.drag = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        HandleFlip();

        // Start jump
        if (Input.GetButtonDown("Jump"))
        {
            TryStartJump();
        }

        // Holding jump
        if (Input.GetButton("Jump"))
        {
            ContinueJumpHold();
        }

        // Release jump
        if (Input.GetButtonUp("Jump"))
        {
            EndJumpHold(earlyRelease: true);
        }

        ApplyAdaptiveGravity();
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(input.x * moveSpeed, rb.velocity.y);
    }

    private void HandleFlip()
    {
        if (input.x != 0f)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Sign(input.x) * Mathf.Abs(baseScaleX);
            transform.localScale = s;
        }
    }

    private bool IsGroundedSimple()
    {
        return Mathf.Abs(rb.velocity.y) <= groundedVelocityTolerance;
    }

    private void TryStartJump()
    {
        if (!IsGroundedSimple()) return;

        // Reset verticale per consistenza
        Vector2 v = rb.velocity;
        v.y = 0f;
        rb.velocity = v;

        rb.AddForce(Vector2.up * initialJumpImpulse, ForceMode2D.Impulse);

        hasStartedJump = true;
        isHoldingJump = true;
        holdTimer = 0f;
    }

    private void ContinueJumpHold()
    {
        if (!isHoldingJump) return;
        if (!hasStartedJump) return;

        holdTimer += Time.deltaTime;

        if (holdTimer >= maxHoldTime)
        {
            // Tempo massimo raggiunto
            EndJumpHold(earlyRelease: false);
            return;
        }

        // Finché saliamo e non abbiamo superato il limite di velocità, aggiungiamo forza
        if (rb.velocity.y < maxUpwardSpeed)
        {
            rb.AddForce(holdForce * Time.deltaTime * Vector2.up, ForceMode2D.Force);
        }
    }

    private void EndJumpHold(bool earlyRelease)
    {
        if (!hasStartedJump) return;

        isHoldingJump = false;

        if (earlyRelease && rb.velocity.y > 0f)
        {
            // Taglio della velocità per salto basso
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
        }
    }

    private void ApplyAdaptiveGravity()
    {
        // Se stiamo ancora “tenendo” il salto e saliamo, usa la gravità base
        if (rb.velocity.y > 0f)
        {
            if (isHoldingJump)
            {
                rb.gravityScale = desiredGravityScale;
            }
            else
            {
                // Se abbiamo rilasciato presto, aumenta un po' la gravità
                rb.gravityScale = earlyReleaseGravityScale;
            }
        }
        else if (rb.velocity.y < 0f)
        {
            // In discesa
            rb.gravityScale = fallGravityScale;
        }
        else
        {
            // Fermissimo in verticale (a terra o quasi)
            rb.gravityScale = desiredGravityScale;
            if (IsGroundedSimple())
            {
                // Reset stato salto
                hasStartedJump = false;
                isHoldingJump = false;
                holdTimer = 0f;
            }
        }
    }
}