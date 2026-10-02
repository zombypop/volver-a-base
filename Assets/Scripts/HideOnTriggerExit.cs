using System.Collections;
using UnityEngine;

/// <summary>
/// Fades a CanvasGroup out to alpha 0 the first time the player leaves this trigger, then
/// never shows it again. Put this on the GameObject holding the (trigger) Box Collider 2D —
/// typically the child that also carries the Image and CanvasGroup; the collider defines the
/// zone and the image stays visible until the player walks out of it.
///
/// The CanvasGroup is auto-found on this object or its children if not assigned in the
/// Inspector, so the image and the trigger can live on the same object or be wired up by hand.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HideOnTriggerExit : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup; // the image to hide; auto-found on self/children if empty
    [SerializeField] private float fadeDuration = 0.3f; // seconds to fade the alpha to 0 (0 = instant)

    private Coroutine fadeRoutine;
    private bool consumed; // once the player has left and it's hidden, it never fades again

    // Default a freshly-added trigger collider to "is trigger" for convenience in the editor.
    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>();

        // It's just an image, never a clickable UI element.
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (consumed || !IsPlayer(other)) return;

        consumed = true; // hidden once: don't fade again after this exit
        FadeToZero();    // player left — hide the image (alpha 0)
    }

    // The player's body collider sits on the same object as PlayerMovement, but check the
    // parents too in case a child collider is what entered the trigger (matches the pickups).
    private bool IsPlayer(Collider2D other) =>
        other.GetComponent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovement>() != null;

    private void FadeToZero()
    {
        if (canvasGroup == null) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = 0f; // instant
            return;
        }

        fadeRoutine = StartCoroutine(Fade());
    }

    private IEnumerator Fade()
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(start, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        fadeRoutine = null;
    }
}
