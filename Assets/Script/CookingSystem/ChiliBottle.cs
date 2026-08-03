using UnityEngine;
using UnityEngine.InputSystem;

public class ChiliBottle : MonoBehaviour, IDraggable
{
    [Header("Shake Settings")]
    [SerializeField] private float requiredShake = 150f;
    [SerializeField] private float minShakeDelta = 2f;
    [SerializeField] private float seasoningCooldown = 0.5f;

    [Tooltip("Reset shake progress if the player stops shaking.")]
    [SerializeField] private float shakeResetDelay = 0.4f;

    [Header("Layer Settings")]
    public string defaultLayer = "Tools";
    public string dragLayer = "Dragging";

    private Vector3 startPos;
    private SpriteRenderer sr;

    private float shakeProgress;
    private float lastShakeDirection;
    private float lastSeasoningTime = -999f;
    private float lastShakeMoveTime;

    private void Start()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
    }

    public void OnBeginDrag()
    {
        shakeProgress = 0f;
        lastShakeDirection = 0f;
        lastShakeMoveTime = Time.time;

        if (sr != null)
            sr.sortingLayerName = dragLayer;

        transform.rotation = Quaternion.Euler(0, 0, 135f);
    }

    public void OnDrag(Vector2 mousePos)
    {
        transform.position = mousePos;

        if (Mouse.current == null)
            return;

        float deltaY = Mouse.current.delta.ReadValue().y;
        float absDeltaY = Mathf.Abs(deltaY);

        // Reset progress if player stopped shaking.
        if (Time.time - lastShakeMoveTime > shakeResetDelay)
        {
            shakeProgress = 0f;
            lastShakeDirection = 0f;
        }

        if (absDeltaY < minShakeDelta)
            return;

        lastShakeMoveTime = Time.time;

        float direction = Mathf.Sign(deltaY);

        if (lastShakeDirection != 0f && direction != lastShakeDirection)
        {
            shakeProgress += absDeltaY;
        }
        else if (lastShakeDirection == 0f)
        {
            shakeProgress += absDeltaY * 0.5f;
        }

        lastShakeDirection = direction;

        if (shakeProgress >= requiredShake)
        {
            if (TryApplySeasoning(mousePos))
            {
                shakeProgress = 0f;
            }
        }
    }

    private bool TryApplySeasoning(Vector2 mousePos)
    {
        if (Time.time < lastSeasoningTime + seasoningCooldown)
            return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

        foreach (var hit in hits)
        {
            FoodInstance food = hit.collider.GetComponent<FoodInstance>();

            if (food != null &&
                food.currentSeasoning != null &&
                food.spicyLevel < 3)
            {
                food.AddSpicy();

                lastSeasoningTime = Time.time;

                // Small feedback
                transform.position += Vector3.up * 0.2f;

                return true;
            }
        }

        return false;
    }

    public void OnEndDrag()
    {
        transform.position = startPos;
        transform.rotation = Quaternion.identity;

        if (sr != null)
            sr.sortingLayerName = defaultLayer;

        shakeProgress = 0f;
        lastShakeDirection = 0f;
    }
}