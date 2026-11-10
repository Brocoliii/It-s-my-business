using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BlackCircleTransition : MonoBehaviour
{
    [Header("Transition")]
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private AnimationCurve expandCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private int sortingOrder = 30001;
    [Tooltip("Multiplies the final circle size. Increase this to make the circle expand farther beyond the screen edges.")]
    [Min(1f)]
    [SerializeField] private float coverageMultiplier = 1.25f;

    [Header("Circle Sprite")]
    [Tooltip("Optional custom circle sprite. If empty, a smooth circle is generated at runtime.")]
    [SerializeField] private Sprite circleSpriteAsset;

    private Image circleImage;
    private Coroutine transitionRoutine;
    private bool isLoadingScene;
    private Texture2D circleTexture;
    private Sprite runtimeCircleSprite;

    public bool IsTransitioning => transitionRoutine != null;

    public static BlackCircleTransition FindOrCreate()
    {
        BlackCircleTransition transition = FindObjectOfType<BlackCircleTransition>(true);
        if (transition == null)
        {
            transition = new GameObject("Black Circle Transition").AddComponent<BlackCircleTransition>();
        }

        return transition;
    }

    private void Awake()
    {
        EnsureOverlay();
    }

    public bool BeginTransition(string sceneName)
    {
        if (isLoadingScene || string.IsNullOrWhiteSpace(sceneName))
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[BlackCircleTransition] No gameplay scene name was configured.");
            }

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[BlackCircleTransition] Cannot load scene '{sceneName}'. Check its name and Build Settings.");
            return false;
        }

        isLoadingScene = true;
        transitionRoutine = StartCoroutine(ExpandAndLoadRoutine(sceneName));
        return true;
    }

    private IEnumerator ExpandAndLoadRoutine(string sceneName)
    {
        circleImage.gameObject.SetActive(true);
        RectTransform circleTransform = circleImage.rectTransform;
        circleTransform.localScale = Vector3.zero;

        float time = 0f;
        float transitionDuration = Mathf.Max(0.01f, duration);
        while (time < transitionDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = expandCurve.Evaluate(Mathf.Clamp01(time / transitionDuration));
            circleTransform.localScale = Vector3.one * t;
            yield return null;
        }

        circleTransform.localScale = Vector3.one;
        Time.timeScale = 1f;

        AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
        while (loading != null && !loading.isDone)
        {
            yield return null;
        }
    }

    private void EnsureOverlay()
    {
        if (circleImage != null) return;

        GameObject canvasObject = new GameObject("Black Circle Transition Canvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("Black Circle", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        circleImage = imageObject.GetComponent<Image>();
        circleImage.color = Color.black;
        circleImage.raycastTarget = true;
        circleImage.sprite = circleSpriteAsset != null ? circleSpriteAsset : CreateCircleSprite();
        circleImage.type = Image.Type.Simple;
        circleImage.preserveAspect = false;

        RectTransform imageTransform = circleImage.rectTransform;
        imageTransform.anchorMin = new Vector2(0.5f, 0.5f);
        imageTransform.anchorMax = new Vector2(0.5f, 0.5f);
        imageTransform.pivot = new Vector2(0.5f, 0.5f);
        imageTransform.anchoredPosition = Vector2.zero;

        float radius = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height)
            * 0.5f
            * Mathf.Max(1f, coverageMultiplier);
        imageTransform.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        imageObject.SetActive(false);
    }

    private Sprite CreateCircleSprite()
    {
        const int textureSize = 256;
        circleTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        circleTexture.name = "Runtime Black Circle";
        circleTexture.wrapMode = TextureWrapMode.Clamp;
        circleTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[textureSize * textureSize];
        float center = (textureSize - 1) * 0.5f;
        float edge = center - 1f;
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(edge + 0.5f - distance);
                pixels[y * textureSize + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        circleTexture.SetPixels(pixels);
        circleTexture.Apply();
        runtimeCircleSprite = Sprite.Create(
            circleTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f));
        runtimeCircleSprite.name = "Runtime Black Circle Sprite";
        return runtimeCircleSprite;
    }

    private void OnDestroy()
    {
        if (runtimeCircleSprite != null) Destroy(runtimeCircleSprite);
        if (circleTexture != null) Destroy(circleTexture);
    }
}
