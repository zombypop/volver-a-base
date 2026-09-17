using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float maxMoveSpeed = 2.5f;
    [SerializeField] private float jumpForce = 4f;
    [SerializeField] private float gripDeceleration = 40f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private bool isGripping;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Hold left mouse to "grab" the ground and scrub off speed until stopped.
        isGripping = Input.GetMouseButton(0);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
        }

        // Face the direction of travel, so the player looks downhill while descending.
        if (spriteRenderer != null && Mathf.Abs(rb.linearVelocity.x) > 0.05f)
        {
            spriteRenderer.flipX = rb.linearVelocity.x < 0f;
        }
    }

    void FixedUpdate()
    {
        // Prefer the collider's real contact with the ground layer — reliable while sliding
        // on the slope — and fall back to the feet overlap check as a backup.
        isGrounded =
            (bodyCollider != null && bodyCollider.IsTouchingLayers(groundLayer)) ||
            (groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer));

        if (isGripping && isGrounded)
        {
            // Digging in: bleed the whole velocity toward zero fast, fighting the slope's
            // pull each step so the player anchors and holds instead of creeping down.
            Vector2 gripped = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, gripDeceleration * Time.fixedDeltaTime);
            rb.linearVelocity = gripped;
        }
        else if (Mathf.Abs(moveInput) > 0.01f)
        {
            // Drive velocity.x toward the target speed at a fixed rate (independent of mass),
            // so steering feels immediate instead of fighting the player's weight.
            float targetSpeed = moveInput * maxMoveSpeed;
            float newSpeedX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
        }
        // else: leave velocity alone — releasing the grip lets inertia and the icy slope slide again

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
        }
    }
}
