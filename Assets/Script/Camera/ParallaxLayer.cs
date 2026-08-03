using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera transform used to calculate the parallax movement.")]
    public Transform cameraTransform;

    [Header("Movement")]
    [Range(-2f, 2f)]
    [Tooltip("How strongly the layer follows the camera movement.")]
    public float parallaxMultiplier = 0.5f;

    [Tooltip("Enable horizontal parallax movement.")]
    public bool affectX = true;

    [Tooltip("Enable vertical parallax movement.")]
    public bool affectY = false;

    [Header("Depth Based Multiplier")]
    [Tooltip("Scale the parallax amount based on the layer-camera depth difference.")]
    public bool useDepthMultiplier;

    [Tooltip("Reference distance used when applying depth-based scaling.")]
    public float depthReference = 10f;

    [Header("Startup")]
    [Tooltip("Automatically find the main camera if no camera is assigned.")]
    public bool autoFindMainCamera = true;

    [Tooltip("Re-capture the starting positions whenever the component is enabled.")]
    public bool recaptureOnEnable = true;

    private Vector3 startLayerPosition;
    private Vector3 startCameraPosition;
    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    private void OnEnable()
    {
        if (recaptureOnEnable)
        {
            CaptureStartPositions();
        }
    }

    private void LateUpdate()
    {
        if (!TryInitialize())
            return;

        ApplyParallax();
    }

    private void ApplyParallax()
    {
        Vector3 cameraDelta = cameraTransform.position - startCameraPosition;
        float multiplier = GetEffectiveMultiplier();

        Vector3 targetPosition = startLayerPosition;

        if (affectX)
        {
            targetPosition.x += cameraDelta.x * multiplier;
        }

        if (affectY)
        {
            targetPosition.y += cameraDelta.y * multiplier;
        }

        targetPosition.z = startLayerPosition.z;
        transform.position = targetPosition;
    }

    private float GetEffectiveMultiplier()
    {
        if (!useDepthMultiplier || cameraTransform == null)
            return parallaxMultiplier;

        float safeReference = Mathf.Max(0.001f, Mathf.Abs(depthReference));
        float distance = Mathf.Abs(startLayerPosition.z - cameraTransform.position.z);

        return parallaxMultiplier * (distance / safeReference);
    }

    private bool TryInitialize()
    {
        if (initialized && cameraTransform != null)
            return true;

        if (cameraTransform == null && autoFindMainCamera)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }

        if (cameraTransform == null)
            return false;

        CaptureStartPositions();
        initialized = true;
        return true;
    }

    public void CaptureStartPositions()
    {
        if (cameraTransform == null)
            return;

        startLayerPosition = transform.position;
        startCameraPosition = cameraTransform.position;
    }
}