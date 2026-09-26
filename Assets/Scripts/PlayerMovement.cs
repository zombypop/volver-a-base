using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private bool icyTerrain = true;          // off = normal footing: releasing the keys stops you instead of sliding
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float maxMoveSpeed = 2.5f;

    [Header("Snow trudge")]
    [SerializeField] private float startupTime = 0.5f;         // seconds of effort to work up from a trudge to full walking speed
    [SerializeField] private float startupSpeedFactor = 0.3f;  // fraction of maxMoveSpeed you manage the instant you start (post-hole into the snow)
    [SerializeField] private float jumpForce = 4f;
    [SerializeField] private float gripDeceleration = 40f;
    [SerializeField] private float climbHopHeight = 0.5f;      // how high the little climb hop reaches (meters)
    [SerializeField] private float climbHopSideSpeed = 1.5f;   // sideways push into the climb (units/sec)
    [SerializeField] private float doubleClickTime = 0.3f;     // max gap between clicks to count as a double
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Ground angle probe")]
    [SerializeField] private float groundRayLength = 0.6f;   // how far down to look for the surface under the player

    [Header("Rope descent")]
    [SerializeField] private float descendSpeed = 1.5f;    // safe rappel speed downward (units/sec)
    [SerializeField] private float maxRopeLength = 5f;      // how far below the anchor the rope reaches
    [SerializeField] private float ropeCenterSpeed = 4f;    // max speed the rope can pull the player back toward center
    [SerializeField] private float ropeSwingResponsiveness = 3f; // how hard the rope resists sideways sway (higher = stiffer, less swing)
    [SerializeField] private LineRenderer ropeRenderer;     // visual rope; auto-created if left empty
    [SerializeField] private Material ropeMaterial;         // look of the rope line; falls back to a plain sprite shader if empty
    [SerializeField] private float ropeWidth = 0.05f;       // thickness of the rope line; tweakable live in Play mode
    [SerializeField] private float ropeDamagePerMeter = 2f; // extra mountain-impact damage per meter of rope paid out
    [SerializeField] private float minSwingSpeedForBonus = 1.5f; // sideways speed needed for a hit to count as a "swinging" hit

    [Header("Animation")]
    [SerializeField] private float walkAnimSpeedThreshold = 0.05f; // horizontal speed above which the walk anim plays

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsSlidingHash = Animator.StringToHash("IsSliding");
    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private bool isGripping;
    private bool climbHopRequested;
    private float moveHoldTime;        // how long we've been trudging in the current direction (drives the snow ramp)
    private float lastMoveSign;        // direction we were last steering; a flip restarts the trudge
    private float lastClickTime = -1f;
    private EdgeAnchor nearbyAnchor;   // an edge we're overlapping and could grab
    private EdgeAnchor ropedAnchor;    // the edge we're currently descending from (null = not on rope)
    private PlayerHealth health;
    private float groundAngle;         // signed slope angle of the ground under us: 0 = flat, +45 uphill-right, -45 uphill-left

    // How far below the anchor we've paid out rope right now (0 when not roped).
    // Used by WindZone to scale gust force and here to scale mountain-impact damage.
    public float CurrentRopeLength => ropedAnchor != null ? Mathf.Max(0f, ropedAnchor.RopeOrigin.y - rb.position.y) : 0f;
    public bool IsOnRope => ropedAnchor != null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        health = GetComponent<PlayerHealth>();
        animator = GetComponentInChildren<Animator>();          // lives on the PlayerSprite child
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // same child — root has no renderer to flip

        // Make sure we have a rope to draw. If none was assigned in the Inspector,
        // spin up a simple one so the mechanic works out of the box.
        if (ropeRenderer == null)
        {
            ropeRenderer = gameObject.AddComponent<LineRenderer>();
            ropeRenderer.numCapVertices = 2;
            ropeRenderer.sortingOrder = 10;
        }

        // Use the assigned RopeMat so the line looks like an actual rope; the vertex colors
        // are left white so the material's own look shows through untinted. Fall back to a
        // plain sprite shader (with a rope-brown tint) only when no material was assigned.
        if (ropeMaterial != null)
        {
            ropeRenderer.material = ropeMaterial;
            ropeRenderer.startColor = ropeRenderer.endColor = Color.white;
        }
        else
        {
            ropeRenderer.material = new Material(Shader.Find("Sprites/Default"));
            ropeRenderer.startColor = ropeRenderer.endColor = new Color(0.5f, 0.35f, 0.2f);
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

        // Three grounded looks: walk when the player is steering under power, slope when
        // they're coasting/sliding across the terrain with no input, idle when basically
        // still. Roped descent uses none of these.
        if (animator != null)
        {
            bool onGround = ropedAnchor == null && isGrounded;
            bool moving = Mathf.Abs(rb.linearVelocity.x) > walkAnimSpeedThreshold;
            bool hasInput = Mathf.Abs(moveInput) > 0.01f;

            bool walking = onGround && moving && hasInput;
            bool sliding = onGround && moving && !hasInput;

            animator.SetBool(IsWalkingHash, walking);
            animator.SetBool(IsSlidingHash, sliding);
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

        // Probe the surface directly under the player to read the slope angle: cast straight
        // down and measure how far the hit's normal tilts off vertical. Flat ground reads ~0,
        // a slope up to the right reads +45, up to the left -45.
        Vector2 rayStart = groundCheck != null ? (Vector2)groundCheck.position : rb.position;
        RaycastHit2D groundHit = Physics2D.Raycast(rayStart, Vector2.down, groundRayLength, groundLayer);
        if (groundHit.collider != null)
        {
            groundAngle = Vector2.SignedAngle(Vector2.up, groundHit.normal);
            Debug.Log($"Ground angle: {groundAngle:0.0}°  ({groundHit.collider.name})");
        }
        else
        {
            groundAngle = 0f;
            Debug.Log("Ground angle: no surface below (airborne)");
        }

        if (isGripping && isGrounded)
        {
            // Digging in: bleed the whole velocity toward zero fast, fighting the slope's
            // pull each step so the player anchors and holds instead of creeping down.
            Vector2 gripped = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, gripDeceleration * Time.fixedDeltaTime);
            rb.linearVelocity = gripped;
            moveHoldTime = 0f; // let go and you'll have to break trail through the snow again
        }
        else if (Mathf.Abs(moveInput) > 0.01f)
        {
            // Break trail through deep snow: each fresh step (or reversal) starts as a slow
            // trudge and works up to full walking pace over startupTime, so getting moving
            // takes effort instead of snapping to speed.
            float moveSign = Mathf.Sign(moveInput);
            if (moveSign != lastMoveSign) moveHoldTime = 0f;
            lastMoveSign = moveSign;
            moveHoldTime = Mathf.Min(moveHoldTime + Time.fixedDeltaTime, startupTime);

            float ramp = startupTime > 0f ? Mathf.Clamp01(moveHoldTime / startupTime) : 1f;
            float speedCap = maxMoveSpeed * Mathf.Lerp(startupSpeedFactor, 1f, ramp);

            // Drive velocity.x toward the (ramped) target speed at a fixed rate (independent of
            // mass), so steering feels immediate instead of fighting the player's weight.
            float targetSpeed = moveSign * speedCap;
            float newSpeedX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
        }
        else if (!icyTerrain && isGrounded)
        {
            // Normal footing: no input means brake to a stop instead of coasting on the slope.
            float newSpeedX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
            moveHoldTime = 0f;
        }
        else
        {
            // Icy coast: leave velocity alone so inertia and the slope keep sliding — but the
            // next deliberate step still has to break trail from a trudge.
            moveHoldTime = 0f;
        }

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

        // Pull the player back toward hanging under the anchor, but as a spring rather than
        // an instant snap — this is what lets wind gusts actually swing the player sideways
        // instead of being corrected away within a single physics step.
        float offsetX = pos.x - origin.x;
        float xVel = Mathf.Clamp(-offsetX * ropeSwingResponsiveness, -ropeCenterSpeed, ropeCenterSpeed);

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
            // Applied every frame so changing ropeWidth in the Inspector updates live in Play mode.
            ropeRenderer.startWidth = ropeRenderer.endWidth = ropeWidth;
            ropeRenderer.SetPosition(0, ropedAnchor.RopeOrigin);
            ropeRenderer.SetPosition(1, rb.position);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        MountainHazard hazard = collision.collider.GetComponent<MountainHazard>();
        if (hazard == null || health == null) return;

        // A light graze shouldn't hurt — only a proper slam into the wall counts.
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < hazard.MinImpactSpeed) return;

        float overSpeed = impactSpeed - hazard.MinImpactSpeed;
        float damage = hazard.BaseDamage + overSpeed * hazard.ImpactSpeedDamageMultiplier;

        // Extra damage only for an actual swinging hit: on the rope AND moving sideways
        // at impact — not just for hanging on a long rope that happens to bump the wall.
        float lateralSpeed = Mathf.Abs(collision.relativeVelocity.x);
        bool isSwingingHit = IsOnRope && lateralSpeed >= minSwingSpeedForBonus;
        if (isSwingingHit)
        {
            damage += CurrentRopeLength * ropeDamagePerMeter;
        }

        health.TakeDamage(damage);
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
