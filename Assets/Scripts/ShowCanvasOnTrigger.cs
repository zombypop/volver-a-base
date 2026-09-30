using System.Collections;
using UnityEngine;

/// <summary>
/// Reveals a CanvasGroup (e.g. the WASD-movement hint) while the player stands inside this
/// trigger box, fading it in on enter and — unless disabled — back out on exit. Put this on
/// the GameObject holding the (trigger) Box Collider 2D; the collider defines the zone.
///
/// The CanvasGroup is auto-found on this object or its children if not assigned in the
/// Inspector, so the canvas and the trigger can live on the same object or be wired up by hand.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShowCanvasOnTrigger : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup; // the hint canvas to reveal; auto-found on self/children if empty
    [SerializeField] private float fadeDuration = 0.3f; // seconds to fade the alpha in/out (0 = instant)
    [SerializeField] private bool startVisible = false; // on = hint starts shown (alpha 1); off = starts hidden until entered

    private Coroutine fadeRoutine;
    private bool consumed; // once the player has entered and left, the hint never shows again

    // Default a freshly-added trigger collider to "is trigger" for convenience in the editor.
    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>();

        // Start shown or hidden per startVisible; non-interactive either way (it's just a hint).
        if (canvasGroup != null)
        {
            canvasGroup.alpha = startVisible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || !IsPlayer(other)) return;

        FadeTo(1f); // player entered — reveal the hint
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (consumed || !IsPlayer(other)) return;

        consumed = true; // shown once: don't reveal again after this exit
        FadeTo(0f);      // player left — hide the hint (alpha 0)
    }

    // The player's body collider sits on the same object as PlayerMovement, but check the
    // parents too in case a child collider is what entered the trigger (matches the pickups).
    private bool IsPlayer(Collider2D other) =>
        other.GetComponent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovement>() != null;

    private void FadeTo(float target)
    {
        if (canvasGroup == null) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = target; // instant
            return;
        }

        fadeRoutine = StartCoroutine(Fade(target));
    }

    private IEnumerator Fade(float target)
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = target;
        fadeRoutine = null;
    }
}
