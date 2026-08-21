using UnityEngine;
using System.Collections;

public class SceneShutterTransition : MonoBehaviour
{
    [Header("Shutter")]
    public RectTransform shutterImage;

    [Header("Timing (Open)")]
    public float startDelay = 0.3f;
    public float openDuration = 1f;
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Timing (Close)")]
    public float closeDuration = 1f;
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Distance")]
    [Tooltip("Extra distance to travel past the shutter's own height, so it clears the screen with room to spare.")]
    public float extraDistance = 200f;

    [Header("On Complete")]
    public bool disableAfterOpen = true;

    [Header("Day Complete Banner")]
    [Tooltip("สไปรท์ข้อความ DAY COMPLETE! ที่จะเด้งขึ้นมาก่อนม่านเลื่อนลงมาปิดจอ ถ้าไม่ผูกไว้จะข้ามขั้นตอนนี้ไปเลย")]
    public RectTransform dayCompleteBanner;
    [Tooltip("ใส่ไว้ถ้าอยากให้จางหายไปด้วยตอนยุบ ไม่ใส่ก็ได้")]
    public CanvasGroup dayCompleteCanvasGroup;
    public float bannerAppearDuration = 0.4f;
    public float bannerHoldDuration = 1f;
    public float bannerDisappearDuration = 0.3f;
    [Tooltip("ความแรงเด้งบีบ/ยืดตอนป้ายลงจอดหลังขยายเต็มขนาด")]
    [Range(0f, 0.3f)] public float bannerLandingSquashAmount = 0.08f;
    [Tooltip("ขนาดที่ยืดเก็บแรง (anticipation) สั้นๆ ก่อนป้ายจะยุบหายไป")]
    [Range(1f, 1.5f)] public float bannerAnticipationScale = 1.15f;

    private void Start()
    {
        StartCoroutine(PlayOpenSequence());
    }

    private IEnumerator PlayOpenSequence()
    {
        // เผื่อ shutterImage ถูกปิดไว้ (inactive) ใน Inspector ตอนแก้ฉาก ต้องเปิดก่อนถึงจะเลื่อน/มองเห็นได้
        if (!shutterImage.gameObject.activeSelf)
        {
            shutterImage.gameObject.SetActive(true);
        }

        shutterImage.anchoredPosition = Vector2.zero;

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        float travelDistance = shutterImage.rect.height + extraDistance;
        float time = 0f;

        while (time < openDuration)
        {
            time += Time.deltaTime;
            float t = openCurve.Evaluate(Mathf.Clamp01(time / openDuration));
            shutterImage.anchoredPosition = new Vector2(0f, travelDistance * t);
            yield return null;
        }

        shutterImage.anchoredPosition = new Vector2(0f, travelDistance);

        if (disableAfterOpen)
        {
            shutterImage.gameObject.SetActive(false);
        }
    }

    // เรียกจากภายนอก (เช่นตอนจบวัน) ให้ม่านเลื่อนจากด้านบนลงมาปิดจอ
    public Coroutine PlayCloseSequence()
    {
        return StartCoroutine(PlayCloseRoutine());
    }

    private IEnumerator PlayCloseRoutine()
    {
        // เผื่อ shutterImage ถูกปิดไว้ (inactive) จากตอนจบ PlayOpenSequence หรือใน Inspector
        if (!shutterImage.gameObject.activeSelf)
        {
            shutterImage.gameObject.SetActive(true);
        }

        yield return PlayDayCompleteBanner();

        float travelDistance = shutterImage.rect.height + extraDistance;
        shutterImage.anchoredPosition = new Vector2(0f, travelDistance);

        float time = 0f;

        while (time < closeDuration)
        {
            time += Time.deltaTime;
            float t = closeCurve.Evaluate(Mathf.Clamp01(time / closeDuration));
            shutterImage.anchoredPosition = new Vector2(0f, travelDistance * (1f - t));
            yield return null;
        }

        shutterImage.anchoredPosition = Vector2.zero;
    }

    // โชว์สไปรท์ DAY COMPLETE! เด้งขยายจาก 0 แบบเกินขนาดแล้วหดกลับ (ease-out back) พร้อมจางเข้า
    // ก่อนม่านจะเลื่อนลงมาปิดจอ ถ้าไม่ได้ผูก dayCompleteBanner ไว้ใน Inspector จะข้ามขั้นตอนนี้ไปเลย
    private IEnumerator PlayDayCompleteBanner()
    {
        if (dayCompleteBanner == null) yield break;

        dayCompleteBanner.gameObject.SetActive(true);

        // ใช้ scale ที่ตั้งไว้บน RectTransform เอง (ปรับขนาดป้ายได้จาก Inspector) แทนการ fix เป็น 1 เสมอ
        Vector3 targetScale = dayCompleteBanner.localScale;
        dayCompleteBanner.localScale = Vector3.zero;
        if (dayCompleteCanvasGroup != null) dayCompleteCanvasGroup.alpha = 0f;

        float time = 0f;
        while (time < bannerAppearDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / bannerAppearDuration);
            float overshoot = 1.70158f;
            float easedT = t - 1f;
            float easeOutBack = 1f + (overshoot + 1f) * Mathf.Pow(easedT, 3f) + overshoot * Mathf.Pow(easedT, 2f);
            dayCompleteBanner.localScale = targetScale * easeOutBack;
            // จางเข้าเร็วกว่าขยาย ให้ตัวหนังสืออ่านออกก่อนที่การเด้งจะนิ่ง
            if (dayCompleteCanvasGroup != null) dayCompleteCanvasGroup.alpha = Mathf.Clamp01(t / 0.6f);
            yield return null;
        }
        dayCompleteBanner.localScale = targetScale;
        if (dayCompleteCanvasGroup != null) dayCompleteCanvasGroup.alpha = 1f;

        // เด้งบีบ/ยืดสั้นๆ ตอนลงจอด ให้ป้ายดูมีน้ำหนักแทนที่จะหยุดนิ่งแข็งๆ ทันที
        yield return PlayBannerLandingSquash(targetScale);

        if (bannerHoldDuration > 0f)
        {
            yield return new WaitForSeconds(bannerHoldDuration);
        }

        // ยุบหาย: ยืดตัวเก็บแรง (anticipation) สั้นๆ ก่อน แล้วค่อยหดยุบตัวหายไปเร็วๆ (ease-in)
        // พร้อมจางหาย (ถ้ามี CanvasGroup) ก่อนปิดทิ้งแล้วให้ม่านเลื่อนลงมาต่อ
        float anticipationDuration = bannerDisappearDuration * 0.25f;
        Vector3 anticipationScale = targetScale * bannerAnticipationScale;

        time = 0f;
        while (time < anticipationDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / anticipationDuration);
            float smoothT = t * t * (3f - 2f * t);
            dayCompleteBanner.localScale = Vector3.Lerp(targetScale, anticipationScale, smoothT);
            yield return null;
        }

        float collapseDuration = bannerDisappearDuration - anticipationDuration;
        Vector3 collapseStartScale = dayCompleteBanner.localScale;

        time = 0f;
        while (time < collapseDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / collapseDuration);
            float easeInT = t * t * t;
            dayCompleteBanner.localScale = Vector3.Lerp(collapseStartScale, Vector3.zero, easeInT);
            if (dayCompleteCanvasGroup != null) dayCompleteCanvasGroup.alpha = 1f - easeInT;
            yield return null;
        }

        dayCompleteBanner.gameObject.SetActive(false);
    }

    // เด้งบีบ/ยืดสั้นๆ (squash-and-stretch) ตอนป้ายลงจอดหลังขยายเต็มขนาด
    private IEnumerator PlayBannerLandingSquash(Vector3 baseScale)
    {
        if (bannerLandingSquashAmount <= 0f) yield break;

        float duration = 0.15f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float squash = Mathf.Sin(t * Mathf.PI) * bannerLandingSquashAmount * (1f - t);
            dayCompleteBanner.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z);
            yield return null;
        }
        dayCompleteBanner.localScale = baseScale;
    }
}
