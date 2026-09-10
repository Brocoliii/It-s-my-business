using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ม่านดำครอบทั้งจอ ใช้เฟดดำก่อนเปลี่ยนฉาก (เช่นกด Start ที่เมนูหลักแล้วค่อยเข้าด่าน 1)
// ถ้าไม่ผูก Fade Canvas Group ไว้ใน Inspector จะสร้าง Canvas + ภาพดำให้เองตอนรันไทม์ ไม่ต้องไปจัดฉากเพิ่ม
public class ScreenFader : MonoBehaviour
{
    [Header("Fade Overlay")]
    [Tooltip("ผูก CanvasGroup ของภาพดำเต็มจอเองก็ได้ ถ้าเว้นว่างไว้ระบบจะสร้างให้เองตอนรันไทม์")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("สีของม่าน ปกติคือดำทึบ")]
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Canvas (ตอนสร้างเอง)")]
    [Tooltip("ต้องทับ UI อื่นทุกอันในฉาก ถ้าม่านยังโดน UI อื่นบัง ให้เพิ่มเลขนี้ขึ้นอีก")]
    [SerializeField] private int sortingOrder = 30000;

    public float FadeDuration => fadeDuration;

    private Coroutine fadeRoutine;
    private bool isLoadingScene;

    // เก็บไว้ลบทิ้งตอนตัวเองโดนทำลาย เพราะม่านที่สร้างเองไม่ได้เป็นลูกของออบเจกต์นี้
    private GameObject ownedCanvasObject;

    // หา ScreenFader ในฉาก ถ้าไม่มีก็สร้างใหม่ให้เลย จะได้เรียกใช้ได้โดยไม่ต้องผูกอะไรใน Inspector
    public static ScreenFader FindOrCreate()
    {
        ScreenFader fader = FindObjectOfType<ScreenFader>(true);
        if (fader == null)
        {
            fader = new GameObject("ScreenFader").AddComponent<ScreenFader>();
        }
        return fader;
    }

    private void Awake()
    {
        // สร้างม่านรอไว้ตั้งแต่ต้นฉากเลย จะได้ไม่ต้องไปสร้าง Canvas ตอนกดปุ่มแล้วเฟรมแรกกระตุก
        EnsureOverlay();
    }

    // เฟดดำแล้วโหลดฉากที่ระบุ กันกดซ้ำระหว่างที่ม่านกำลังลงอยู่
    public void FadeOutAndLoadScene(string sceneName)
    {
        if (isLoadingScene) return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[ScreenFader] ยังไม่ได้ตั้งชื่อฉากที่จะโหลด");
            return;
        }

        // เช็คตั้งแต่ตอนนี้เลย จะได้ไม่เฟดดำจนสุดแล้วค่อยไปพังตอน LoadScene แล้วค้างจอดำ
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[ScreenFader] โหลดฉาก '{sceneName}' ไม่ได้! เช็คชื่อฉากให้ตรง และเพิ่มฉากนี้ใน File > Build Settings ด้วย");
            return;
        }

        isLoadingScene = true;
        StartCoroutine(FadeOutAndLoadRoutine(sceneName));
    }

    // เฟดดำเฉยๆ ไม่โหลดฉาก (เผื่ออยากใช้ตอนอื่น) yield return ค่านี้เพื่อรอจนมืดสนิทได้
    public Coroutine FadeToBlack()
    {
        return StartFade(1f);
    }

    // เฟดจากดำกลับมาเห็นภาพ
    public Coroutine FadeFromBlack()
    {
        return StartFade(0f);
    }

    private IEnumerator FadeOutAndLoadRoutine(string sceneName)
    {
        yield return StartFade(1f);

        // กันเคสที่มีอะไรไปตั้ง timeScale = 0 ค้างไว้ ไม่งั้นฉากใหม่จะเปิดมาแล้วค้างนิ่งทั้งฉาก
        Time.timeScale = 1f;

        // โหลดแบบ Async ไม่ใช้ LoadScene ตรงๆ เพราะแบบตรงๆ มันหยุดเกมทั้งเครื่องรอโหลดจนจบ (เกมค้างแป๊บนึงแล้วเด้งเข้าฉากใหม่)
        // แบบนี้เกมยังเดินต่อได้ระหว่างโหลด คนเล่นเห็นแค่จอดำนิ่งๆ ไม่ใช่เกมค้าง
        AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
        while (loading != null && !loading.isDone)
        {
            yield return null;
        }
    }

    private Coroutine StartFade(float targetAlpha)
    {
        EnsureOverlay();

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
        return fadeRoutine;
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        fadeCanvasGroup.gameObject.SetActive(true);
        // บล็อกคลิกระหว่างเฟด จะได้ไม่กดปุ่มอื่นซ้อนตอนม่านกำลังลง
        fadeCanvasGroup.blocksRaycasts = true;

        float startAlpha = fadeCanvasGroup.alpha;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float time = 0f;

        while (time < duration)
        {
            // unscaledDeltaTime เผื่อฉากไหนหยุดเวลาไว้ (timeScale = 0) ม่านจะได้ยังเลื่อนต่อได้
            time += Time.unscaledDeltaTime;
            float t = fadeCurve.Evaluate(Mathf.Clamp01(time / duration));
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        // จางหมดแล้วก็เลิกบังคลิก ไม่งั้นม่านใสๆ จะกินคลิกทั้งจอต่อ
        if (Mathf.Approximately(targetAlpha, 0f))
        {
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        fadeRoutine = null;
    }

    private void EnsureOverlay()
    {
        if (fadeCanvasGroup != null) return;

        // สร้างเป็นออบเจกต์ระดับบนสุด ไม่เอาไปแขวนใต้ออบเจกต์นี้
        // เพราะถ้าบังเอิญไปอยู่ใต้ Canvas อื่น มันจะกลายเป็น Canvas ซ้อน Canvas ที่ใช้ขนาดของ RectTransform ตัวเอง (ได้ 0) แล้วม่านจะมองไม่เห็นเลย
        GameObject canvasObject = new GameObject("ScreenFader Canvas", typeof(RectTransform));
        ownedCanvasObject = canvasObject;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Overlay + sorting order สูงๆ = อยู่หน้าสุดเสมอ ไม่ว่า UI อื่นจะใช้กล้องไหน
        canvas.sortingOrder = sortingOrder;
        canvasObject.AddComponent<GraphicRaycaster>();

        fadeCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        GameObject imageObject = new GameObject("Fade Image", typeof(RectTransform));
        imageObject.transform.SetParent(canvasObject.transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = fadeColor;
        image.raycastTarget = true;

        // ยืดเต็มจอทุกอัตราส่วน
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        canvasObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // ม่านที่สร้างเองเป็นออบเจกต์แยก ต้องเก็บกวาดเองด้วย
        if (ownedCanvasObject != null) Destroy(ownedCanvasObject);
    }
}
