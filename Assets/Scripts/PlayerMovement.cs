using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float maxMoveSpeed = 2.5f;
    [SerializeField] private float jumpForce = 4f;
    [SerializeField] private float gripDeceleration = 40f;
    [SerializeField] private float climbHopHeight = 0.5f;      // how high the little climb hop reaches (meters)
    [SerializeField] private float climbHopSideSpeed = 1.5f;   // sideways push into the climb (units/sec)
    [SerializeField] private float doubleClickTime = 0.3f;     // max gap between clicks to count as a double
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Rope descent")]
    [SerializeField] private float descendSpeed = 1.5f;    // safe rappel speed downward (units/sec)
    [SerializeField] private float maxRopeLength = 5f;      // how far below the anchor the rope reaches
    [SerializeField] private float ropeCenterSpeed = 4f;    // how fast the player is pulled under the anchor
    [SerializeField] private LineRenderer ropeRenderer;     // visual rope; auto-created if left empty

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private bool isGripping;
    private bool climbHopRequested;
    private float lastClickTime = -1f;
    private EdgeAnchor nearbyAnchor;   // an edge we're overlapping and could grab
    private EdgeAnchor ropedAnchor;    // the edge we're currently descending from (null = not on rope)

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Make sure we have a rope to draw. If none was assigned in the Inspector,
        // spin up a simple one so the mechanic works out of the box.
        if (ropeRenderer == null)
        {
            ropeRenderer = gameObject.AddComponent<LineRenderer>();
            ropeRenderer.material = new Material(Shader.Find("Sprites/Default"));
            ropeRenderer.startColor = ropeRenderer.endColor = new Color(0.5f, 0.35f, 0.2f);
            ropeRenderer.startWidth = ropeRenderer.endWidth = 0.05f;
            ropeRenderer.numCapVertices = 2;
            ropeRenderer.sortingOrder = 10;
        }
        ropeRenderer.positionCount = 2;
        ropeRenderer.enabled = false;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Hold left mouse to "grab" the ground and scrub off speed until stopped.
        isGripping = Input.GetMouseButton(0);

        // Double left-click: a little climb hop up-and-to-the-left (~half a meter).
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastClickTime <= doubleClickTime && isGrounded)
            {
                climbHopRequested = true;
                lastClickTime = -1f; // consume, so a triple-click doesn't chain hops
            }
            else
            {
                lastClickTime = Time.time;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
        }

        // Hold right mouse near an edge to grab a rope and rappel down; release to fall.
        if (Input.GetMouseButton(1))
        {
            if (ropedAnchor == null && nearbyAnchor != null)
            {
                ropedAnchor = nearbyAnchor; // grab the rope
            }
        }
        else
        {
            ropedAnchor = null; // released — let go of the rope and fall
        }

        // Face the direction of travel, so the player looks downhill while descending.
        if (spriteRenderer != null && Mathf.Abs(rb.linearVelocity.x) > 0.05f)
        {
            spriteRenderer.flipX = rb.linearVelocity.x < 0f;
        }
    }

    void FixedUpdate()
    {
        if (ropedAnchor != null)
        {
            DescendOnRope();
            return;
        }

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

        if (climbHopRequested)
        {
            // Derive the upward speed needed to reach climbHopHeight from the actual gravity
            // acting on this body: v = sqrt(2 * g * h). Keeps the hop ~half a meter regardless
            // of gravityScale tuning.
            float gravity = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            float hopUpSpeed = Mathf.Sqrt(2f * gravity * climbHopHeight);
            rb.linearVelocity = new Vector2(-climbHopSideSpeed, hopUpSpeed);
            climbHopRequested = false;
        }
    }

    // Rappel: hang beneath the anchor and sink at a controlled speed. Gravity is
    // effectively cancelled because we set the velocity outright every physics step.
    private void DescendOnRope()
    {
        Vector2 origin = ropedAnchor.RopeOrigin;
        Vector2 pos = rb.position;

        // Pull the player under the anchor so they hang from it rather than off to the side.
        float xVel = Mathf.Clamp((origin.x - pos.x) / Time.fixedDeltaTime, -ropeCenterSpeed, ropeCenterSpeed);

        // Descend, but stop paying out rope once we reach its full length.
        float depth = origin.y - pos.y;        // how far below the anchor we already are
        float yVel = depth < maxRopeLength ? -descendSpeed : 0f;

        rb.linearVelocity = new Vector2(xVel, yVel);
    }

    void LateUpdate()
    {
        // Draw the rope from the anchor down to the player while attached.
        bool showRope = ropedAnchor != null;
        ropeRenderer.enabled = showRope;
        if (showRope)
        {
            ropeRenderer.SetPosition(0, ropedAnchor.RopeOrigin);
            ropeRenderer.SetPosition(1, rb.position);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        EdgeAnchor anchor = other.GetComponent<EdgeAnchor>();
        if (anchor != null)
        {
            nearbyAnchor = anchor;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        EdgeAnchor anchor = other.GetComponent<EdgeAnchor>();
        if (anchor != null && anchor == nearbyAnchor)
        {
            nearbyAnchor = null; // out of grabbing range (a rope already grabbed stays attached)
        }
    }
}
