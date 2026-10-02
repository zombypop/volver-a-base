using UnityEngine;

/// <summary>
/// Parallax background layer that scrolls with the camera and repeats horizontally forever by
/// leap-frogging a few side-by-side copies of itself. Put this on EACH copy.
///
/// Setup per layer (sky / far / mid / near):
///  1. Duplicate the layer a few times and lay the copies edge-to-edge horizontally (each one
///     exactly its sprite-width apart), enough that together they overfill the camera view.
///     Rule of thumb: copies * spriteWidth should comfortably exceed the camera's width.
///  2. Keep the VerticalParallax script on every copy and give them all the SAME settings:
///     the same horizontalParallax, verticalParallax, and Copies (= how many copies you made).
///
/// Each frame a copy moves with the camera at horizontalParallax (1 = pinned to the camera / very
/// far, 0 = fixed in the world / foreground), plus a little vertical drift. When a copy drifts
/// more than half the belt behind the camera it jumps one full belt-width ahead, so the belt of
/// copies is always centred on the camera and no edge ever shows.
/// </summary>
public class VerticalParallax : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform; // falls back to Camera.main if empty

    [Header("Horizontal")]
    [SerializeField, Range(0f, 1f)]
    private float horizontalParallax = 0.5f; // 1 = moves with camera (far), 0 = fixed in world (near)
    [SerializeField]
    private int copies = 3;                  // how many side-by-side duplicates make up this layer
    [SerializeField]
    private float tileWidth = 0f;            // world width of one copy; 0 = auto from the SpriteRenderer

    [Header("Vertical")]
    [SerializeField, Range(0f, 1f)]
    private float verticalParallax = 0.05f;  // keep tiny: a little sink as the player goes down

    private float lastCamX;
    private float lastCamY;
    private float span;                      // full belt width = copies * tileWidth

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        if (tileWidth <= 0f)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) tileWidth = sr.bounds.size.x;
        }

        span = Mathf.Max(1, copies) * tileWidth;

        if (cameraTransform != null)
        {
            lastCamX = cameraTransform.position.x;
            lastCamY = cameraTransform.position.y;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Move by the camera's change since last frame so the copy stays wherever it was authored
        // on frame 1 (no jump) and parallaxes from there.
        float camX = cameraTransform.position.x;
        float camY = cameraTransform.position.y;
        float dx = camX - lastCamX;
        float dy = camY - lastCamY;
        lastCamX = camX;
        lastCamY = camY;

        Vector3 p = transform.position;
        p.x += dx * horizontalParallax; // slower than the camera = parallax
        p.y += dy * verticalParallax;   // subtle vertical drift

        // Endless repeat: once this copy is more than half a belt behind/ahead of the camera, hop
        // it one whole belt-width so it reappears on the far side. The copies leap-frog seamlessly.
        if (copies > 1 && span > 0f)
        {
            float d = camX - p.x;
            if (d > span * 0.5f) p.x += span;
            else if (d < -span * 0.5f) p.x -= span;
        }

        transform.position = p;
    }
}
