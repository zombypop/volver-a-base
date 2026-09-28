using UnityEngine;

public class VerticalParallax : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Range(0f, 1f)]
    private float parallaxAmount = 0.2f;

    private float startY;
    private float cameraStartY;

    private void Start()
    {
        startY = transform.position.y;
        cameraStartY = cameraTransform.position.y;
    }

    private void LateUpdate()
    {
        float cameraDelta =
            cameraTransform.position.y - cameraStartY;

        transform.position = new Vector3(
            transform.position.x,
            startY + cameraDelta * parallaxAmount,
            transform.position.z
        );
    }
}