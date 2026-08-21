using UnityEngine;

public class GrillSpriteLoop : MonoBehaviour
{
    [SerializeField] private SpriteRenderer baseRenderer;
    [SerializeField] private SpriteRenderer overlayRenderer;
    [SerializeField] private Sprite[] sprites;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.5f;

    private float fadeTimer;

    private void Start()
    {
        if (baseRenderer == null)
            baseRenderer = GetComponent<SpriteRenderer>();

        if (sprites.Length > 0)
            baseRenderer.sprite = sprites[0];

        if (overlayRenderer != null && sprites.Length > 1)
            overlayRenderer.sprite = sprites[1];
    }

    private void Update()
    {
        if (overlayRenderer == null || fadeDuration <= 0f)
            return;

        fadeTimer += Time.deltaTime;

        float t = Mathf.PingPong(fadeTimer, fadeDuration) / fadeDuration;
        SetAlpha(overlayRenderer, t);
    }

    private void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color c = renderer.color;
        c.a = alpha;
        renderer.color = c;
    }
}
