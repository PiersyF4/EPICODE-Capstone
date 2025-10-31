using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float walkSpeed = 5f;
    public float runSpeed = 8.5f;
    public float groundAccel = 60f;
    public float groundDecel = 70f;
    public float airAccel = 45f;
    public float airDecel = 45f;

    [Header("Salto")]
    public float jumpForce = 15f;          // salto “normale”
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f; // rilasci per salto corto
    public float fallGravityMultiplier = 1.5f; // gravità extra in caduta
    public float jumpCooldown = 0.2f;      // tempo minimo tra un salto e l'altro

    [Header("Wall Slide/Jump")]
    public Transform wallCheckL;
    public Transform wallCheckR;
    public float wallCheckRadius = 0.15f;
    public LayerMask wallMask;
    public float wallSlideMaxSpeed = -3.2f;
    public Vector2 wallJumpForce = new Vector2(11f, 15.5f);
    public float wallStickTime = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;
    public LayerMask groundMask;

    [Header("Misc")]
    public bool facingRight = true;

    Rigidbody2D rb;
    float inputX;
    bool runHeld;
    bool grounded;
    bool jumpHeld;

    // timers salto
    float coyoteCounter;
    float jumpBufferCounter;
    float jumpCooldownCounter; // cooldown tra salti

    // wall
    bool wallSliding;
    int wallDir; // -1 sinistra, +1 destra
    float wallStickCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Input (Input Manager classico)
        inputX = Input.GetAxisRaw("Horizontal");
        runHeld = Input.GetButton("Fire3");           // default: Left Shift
        bool jumpPressed = Input.GetButtonDown("Jump");
        jumpHeld = Input.GetButton("Jump");

        // Ground check (escludo il mio stesso Rigidbody2D)
        var gHit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
        grounded = gHit != null && gHit.attachedRigidbody != rb;

        // Coyote
        if (grounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        // Jump buffer
        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // Jump cooldown
        if (jumpCooldownCounter > 0f) jumpCooldownCounter -= Time.deltaTime;

        // Wall check (escludo il mio stesso Rigidbody2D)
        var wl = Physics2D.OverlapCircle(wallCheckL.position, wallCheckRadius, wallMask);
        var wr = Physics2D.OverlapCircle(wallCheckR.position, wallCheckRadius, wallMask);
        bool touchingL = wl != null && wl.attachedRigidbody != rb;
        bool touchingR = wr != null && wr.attachedRigidbody != rb;

        // Determina la direzione del muro in base alla posizione effettiva dei probes nel mondo.
        // Così il flip della scala non scambia logicamente sinistra/destra.
        int dir = 0;
        if (touchingL)
            dir = wallCheckL.position.x < transform.position.x ? -1 : +1;
        if (touchingR)
            dir = wallCheckR.position.x < transform.position.x ? -1 : +1;
        wallDir = dir;

        // Wall slide
        wallSliding = !grounded && wallDir != 0 && rb.velocity.y < 0f;
        if (wallSliding)
        {
            wallStickCounter = wallStickTime;
            if (rb.velocity.y < wallSlideMaxSpeed)
                rb.velocity = new Vector2(rb.velocity.x, wallSlideMaxSpeed);
        }
        else
        {
            if (wallStickCounter > 0f) wallStickCounter -= Time.deltaTime;
        }

        // Salto
        // 1) Wall jump solo se in slide (evita salti "in aria" solo toccando il muro)
        if (jumpBufferCounter > 0f && wallSliding && jumpCooldownCounter <= 0f)
        {
            WallJump(-wallDir);
            jumpBufferCounter = 0f;
        }
        // 2) Salto normale con coyote reale (niente requisito grounded)
        else if (jumpBufferCounter > 0f && coyoteCounter > 0f && jumpCooldownCounter <= 0f)
        {
            Jump();
            jumpBufferCounter = 0f;
        }

        // Jump cut (rilasci il tasto in ascesa)
        if (!grounded && !jumpHeld && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
        }

        // Gravità extra in caduta per feel “Mario”
        if (rb.velocity.y < 0f)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.deltaTime * rb.gravityScale;
        }

        // Flip
        if (inputX > 0 && !facingRight) Flip();
        else if (inputX < 0 && facingRight) Flip();
    }

    void FixedUpdate()
    {
        // target speed: camminata o corsa
        float target = inputX * (runHeld ? runSpeed : walkSpeed);

        // accel/decel diversi a terra e in aria
        float accel = Mathf.Abs(target) > 0.01f
            ? (grounded ? groundAccel : airAccel)
            : (grounded ? groundDecel : airDecel);

        float newX = Mathf.MoveTowards(rb.velocity.x, target, accel * Time.fixedDeltaTime);

        // se appiccicato al muro, limito l’input verso il muro
        if (wallSliding && Mathf.Sign(target) == wallDir)
            newX = rb.velocity.x;

        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    void Jump()
    {
        // salto lungo = arrivi collo sprint => stessa forza ma più distanza per via della velocità orizzontale
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        coyoteCounter = 0f;
        jumpCooldownCounter = jumpCooldown; // avvia cooldown
    }

    void WallJump(int awayDir)
    {
        // stacco netto dal muro
        Vector2 vel = rb.velocity;
        vel.y = 0f;
        rb.velocity = vel;
        rb.AddForce(new Vector2(awayDir * wallJumpForce.x, wallJumpForce.y), ForceMode2D.Impulse);

        // flip nella direzione dello stacco
        if ((awayDir > 0 && !facingRight) || (awayDir < 0 && facingRight)) Flip();

        // reset timers
        coyoteCounter = 0f;
        jumpCooldownCounter = jumpCooldown; // avvia cooldown
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }
}