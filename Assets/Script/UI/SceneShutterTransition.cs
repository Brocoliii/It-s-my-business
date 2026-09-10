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

    [Header("Baby Dollo")]
    [Tooltip("รูปตุ๊กตา babydollo ที่ห้อยติดอยู่กับม่าน ให้ทำเป็นลูกของ shutterImage ไม่ต้องไปยุ่งกับ Pivot ของรูป เดี๋ยวสคริปต์ล็อกจุดหมุนให้เองตาม Dollo Hang Point ถ้าไม่ผูกไว้จะข้ามการแกว่งไปเลย")]
    public RectTransform babyDollo;
    [Tooltip("วิธีที่แม่นที่สุด: สร้าง Empty GameObject (UI > Empty) วางทับตรงห่วงพวงกุญแจในภาพเป๊ะๆ แล้วลากมาใส่ช่องนี้ ตุ๊กตาจะแกว่งรอบจุดนั้น ถ้าใส่ช่องนี้แล้วจะใช้แทน Dollo Hang Point ทันที (เลือกตัวสคริปต์ในซีนจะเห็นวงกลมเหลืองบอกจุดหมุนกับช่วงที่แกว่ง)")]
    public RectTransform dolloHangAnchor;
    [Tooltip("จุดห่วงแขวนบนตัวรูป (0,0 = มุมล่างซ้าย, 1,1 = มุมบนขวา) ใส่เกิน 0-1 ได้ เช่น Y = 1.3 ถ้าห่วงอยู่เหนือรูปขึ้นไป ใช้เมื่อไม่ได้ผูก Dollo Hang Anchor")]
    public Vector2 dolloHangPoint = new Vector2(0.5f, 1f);
    [Tooltip("มุมแกว่งสูงสุด (องศา)")]
    public float dolloMaxSwingAngle = 22f;
    [Tooltip("ความเร่งของม่าน (พิกเซล/วินาที²) ที่ทำให้เหวี่ยงจนสุดมุม ยิ่งน้อยยิ่งเหวี่ยงแรงง่าย")]
    public float dolloSwingAccelerationReference = 5000f;
    [Tooltip("ความถี่การแกว่งไปกลับ (รอบ/วินาที) ยิ่งมากยิ่งแกว่งถี่")]
    public float dolloSwingFrequency = 1.4f;
    [Tooltip("ความหน่วง 0 = แกว่งไม่หยุด, 1 = หยุดนิ่งโดยแทบไม่แกว่งกลับ")]
    [Range(0f, 1f)] public float dolloSwingDamping = 0.15f;
    [Tooltip("เวลากรองความเร็วม่านให้เนียนก่อนเอาไปหาความเร่ง (วินาที) มากขึ้น = นุ่มขึ้นแต่ตอบสนองช้าลง")]
    public float dolloVelocitySmoothing = 0.06f;
    [Tooltip("เวลาสูงสุดที่ปล่อยให้แกว่งต่อหลังม่านหยุดเคลื่อนที่")]
    public float dolloSettleDuration = 1.5f;

    private float dolloAngle;
    private float dolloAngularVelocity;
    private float dolloSmoothedShutterVelocity;
    private Vector2 dolloRestAnchoredPosition;
    private Quaternion dolloRestRotation = Quaternion.identity;
    private Vector2 dolloAnchorHangOffset;
    private bool dolloRestPoseCaptured;
    private Coroutine dolloSettleRoutine;

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

    private void Awake()
    {
        // จำตำแหน่งตั้งต้นของตุ๊กตาไว้ตั้งแต่ยังไม่หมุน เอาไว้ใช้อ้างอิงตอนชดเชยตำแหน่งให้ห่วงแขวนอยู่นิ่ง
        CaptureDolloRestPose();
    }

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

        SoundManager.Instance.PlaySFX(SoundIds.UiShutter);

        float travelDistance = shutterImage.rect.height + extraDistance;
        float time = 0f;
        float previousY = 0f;

        ResetDolloSwing();

        while (time < openDuration)
        {
            time += Time.deltaTime;
            float t = openCurve.Evaluate(Mathf.Clamp01(time / openDuration));
            float y = travelDistance * t;
            shutterImage.anchoredPosition = new Vector2(0f, y);
            UpdateDolloSwing((y - previousY) / Mathf.Max(Time.deltaTime, 0.0001f), Time.deltaTime);
            previousY = y;
            yield return null;
        }

        shutterImage.anchoredPosition = new Vector2(0f, travelDistance);

        // ปล่อยให้ตุ๊กตาแกว่งค้างต่ออีกหน่อยหลังม่านหยุด ก่อนจะปิดม่านทิ้ง
        yield return SettleDolloSwing();

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

        SoundManager.Instance.PlaySFX(SoundIds.UiShutter);

        float travelDistance = shutterImage.rect.height + extraDistance;
        shutterImage.anchoredPosition = new Vector2(0f, travelDistance);

        float time = 0f;
        float previousY = travelDistance;

        ResetDolloSwing();

        while (time < closeDuration)
        {
            time += Time.deltaTime;
            float t = closeCurve.Evaluate(Mathf.Clamp01(time / closeDuration));
            float y = travelDistance * (1f - t);
            shutterImage.anchoredPosition = new Vector2(0f, y);
            UpdateDolloSwing((y - previousY) / Mathf.Max(Time.deltaTime, 0.0001f), Time.deltaTime);
            previousY = y;
            yield return null;
        }

        shutterImage.anchoredPosition = Vector2.zero;

        // ห้าม yield รอตุ๊กตาแกว่งจนหมดแรงตรงนี้ ไม่งั้นฝั่งที่เรียก (จบวัน -> เปิดสมุด) จะค้างรออีกถึง
        // dolloSettleDuration วินาที ทั้งที่ม่านปิดจอสนิทไปแล้ว กลายเป็นจอดำนิ่งก่อนสมุดจะเด้งขึ้นมา
        // ปล่อยให้แกว่งต่อเป็นคอรูทีนแยก ตุ๊กตายังแกว่งให้เห็นเหมือนเดิม แต่ฉากถัดไปเริ่มได้ทันที
        StartDolloSettle();
    }

    // แกว่งค้างแบบไม่บล็อกคนเรียก เรียกซ้ำได้ รอบเก่าจะถูกตัดทิ้งก่อน
    private void StartDolloSettle()
    {
        if (babyDollo == null) return;

        if (dolloSettleRoutine != null) StopCoroutine(dolloSettleRoutine);
        dolloSettleRoutine = StartCoroutine(SettleDolloSwingDetached());
    }

    private IEnumerator SettleDolloSwingDetached()
    {
        yield return SettleDolloSwing();
        dolloSettleRoutine = null;
    }

    // ตุ๊กตา babydollo ห้อยอยู่กับม่าน จะเหวี่ยงเฉพาะตอนม่าน "ออกตัว" กับ "เบรก" เท่านั้น
    // ไม่ใช่ตอนม่านวิ่งความเร็วคงที่ (ถ้าเล็งมุมตามความเร็ว ตุ๊กตาจะค้างเอียงอยู่ข้างเดียวตลอดที่ม่านวิ่ง)
    // จำลองด้วยสปริงหน่วง (spring-damper): มุมเป้าหมายมาจากความเร่งของม่าน แล้วปล่อยให้แกว่งเข้าหามันเอง
    private void UpdateDolloSwing(float shutterVelocity, float deltaTime)
    {
        if (babyDollo == null) return;

        deltaTime = Mathf.Min(deltaTime, 0.25f);

        // กรองความเร็วให้เนียนก่อนค่อยหาความเร่ง ไม่งั้น deltaTime ที่แกว่งไปมาแต่ละเฟรมจะกลายเป็นความเร่งปลอมๆ สั่นๆ
        float smoothingFactor = 1f - Mathf.Exp(-deltaTime / Mathf.Max(dolloVelocitySmoothing, 0.0001f));
        float smoothedVelocity = Mathf.Lerp(dolloSmoothedShutterVelocity, shutterVelocity, smoothingFactor);
        float shutterAcceleration = (smoothedVelocity - dolloSmoothedShutterVelocity) / Mathf.Max(deltaTime, 0.0001f);
        dolloSmoothedShutterVelocity = smoothedVelocity;

        float normalizedAcceleration = Mathf.Clamp(-shutterAcceleration / Mathf.Max(dolloSwingAccelerationReference, 1f), -1f, 1f);
        float targetAngle = normalizedAcceleration * dolloMaxSwingAngle;
        float omega = 2f * Mathf.PI * Mathf.Max(dolloSwingFrequency, 0.01f);

        // ซอยเป็นสเต็ปย่อยไม่เกิน 1/60 วิ กันสปริงเหวี่ยงหลุดตอนเฟรมตก (deltaTime กระโดด)
        float remaining = deltaTime;
        while (remaining > 0f)
        {
            float step = Mathf.Min(remaining, 1f / 60f);
            remaining -= step;

            float acceleration = (targetAngle - dolloAngle) * omega * omega
                                 - 2f * dolloSwingDamping * omega * dolloAngularVelocity;
            dolloAngularVelocity += acceleration * step;
            dolloAngle += dolloAngularVelocity * step;
        }

        ApplyDolloSwingPose();
    }

    // หมุนตุ๊กตารอบ "ห่วงแขวน" แทนที่จะหมุนรอบ pivot ของ RectTransform เอง
    // RectTransform หมุนรอบ pivot ของมันเสมอ เลยชดเชยตำแหน่งกลับเท่ากับระยะที่ห่วงถูกเหวี่ยงออกไป
    // ห่วงจึงอยู่นิ่งสนิทเหมือนหมุดพวงกุญแจ ส่วนตัวตุ๊กตาห้อยแกว่งอยู่ข้างใต้ โดยไม่ต้องไปแก้ Pivot ในรูป
    private void ApplyDolloSwingPose()
    {
        if (babyDollo == null) return;

        CaptureDolloRestPose();

        Quaternion swing = Quaternion.Euler(0f, 0f, dolloAngle);
        Vector3 hangOffset = GetDolloHangOffset();
        Vector3 compensation = hangOffset - swing * hangOffset;

        // คูณกับท่าหมุนตั้งต้น เผื่อจัดตุ๊กตาเอียงไว้แต่แรกใน Inspector จะได้ไม่โดนรีเซ็ตเป็นตรงเป๊ะ
        babyDollo.localRotation = swing * dolloRestRotation;
        babyDollo.anchoredPosition = dolloRestAnchoredPosition + new Vector2(compensation.x, compensation.y);
    }

    // ระยะจากจุดหมุนของ RectTransform ไปยังห่วงแขวน วัดในระบบพิกัดของพ่อ (ตอนตุ๊กตาอยู่ท่าตั้งต้น)
    private Vector3 GetDolloHangOffset()
    {
        if (dolloHangAnchor != null) return dolloAnchorHangOffset;

        Rect rect = babyDollo.rect;
        Vector3 scale = babyDollo.localScale;
        Vector3 localOffset = new Vector3(
            (dolloHangPoint.x - babyDollo.pivot.x) * rect.width * scale.x,
            (dolloHangPoint.y - babyDollo.pivot.y) * rect.height * scale.y,
            0f);

        return dolloRestRotation * localOffset;
    }

    private void CaptureDolloRestPose()
    {
        if (babyDollo == null || dolloRestPoseCaptured) return;

        dolloRestAnchoredPosition = babyDollo.anchoredPosition;
        dolloRestRotation = babyDollo.localRotation;

        // วัดตำแหน่งห่วงตอนตุ๊กตายังไม่แกว่งเก็บไว้ครั้งเดียว ถ้าวัดสดทุกเฟรมแล้ว dolloHangAnchor
        // ดันเป็นลูกของตุ๊กตา มันจะหมุนตามไปด้วยจนคำนวณจุดหมุนเพี้ยน
        if (babyDollo.parent != null)
        {
            dolloAnchorHangOffset = dolloHangAnchor != null
                ? (Vector2)babyDollo.parent.InverseTransformPoint(dolloHangAnchor.position) - (Vector2)babyDollo.localPosition
                : Vector2.zero;
        }

        dolloRestPoseCaptured = true;
    }

    // วาดจุดหมุนกับช่วงมุมที่จะแกว่งใน Scene view ตอนเลือกสคริปต์ จะได้เล็งห่วงให้ตรงที่ในรูปได้โดยไม่ต้องกด Play
    private void OnDrawGizmosSelected()
    {
        if (babyDollo == null) return;

        Vector3 hangWorld = dolloHangAnchor != null
            ? dolloHangAnchor.position
            : babyDollo.TransformPoint(new Vector3(
                (dolloHangPoint.x - babyDollo.pivot.x) * babyDollo.rect.width,
                (dolloHangPoint.y - babyDollo.pivot.y) * babyDollo.rect.height,
                0f));

        Vector3 hangToDoll = babyDollo.position - hangWorld;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hangWorld, Mathf.Max(babyDollo.rect.height * 0.04f * babyDollo.lossyScale.y, 0.01f));
        Gizmos.DrawLine(hangWorld, babyDollo.position);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.45f);
        Gizmos.DrawLine(hangWorld, hangWorld + Quaternion.AngleAxis(dolloMaxSwingAngle, babyDollo.forward) * hangToDoll);
        Gizmos.DrawLine(hangWorld, hangWorld + Quaternion.AngleAxis(-dolloMaxSwingAngle, babyDollo.forward) * hangToDoll);
    }

    private void ResetDolloSwing()
    {
        if (babyDollo == null) return;

        dolloAngle = 0f;
        dolloAngularVelocity = 0f;
        dolloSmoothedShutterVelocity = 0f;
        ApplyDolloSwingPose();
    }

    // แกว่งต่อจนหมดแรงหลังม่านหยุดนิ่ง (เป้าหมายมุม = 0) ออกก่อนเวลาถ้านิ่งแล้ว จะได้ไม่หน่วงจังหวะฉากถัดไป
    private IEnumerator SettleDolloSwing()
    {
        if (babyDollo == null) yield break;

        float time = 0f;
        while (time < dolloSettleDuration)
        {
            time += Time.deltaTime;
            UpdateDolloSwing(0f, Time.deltaTime);

            if (Mathf.Abs(dolloAngle) < 0.1f && Mathf.Abs(dolloAngularVelocity) < 1f) break;

            yield return null;
        }

        ResetDolloSwing();
    }

    // โชว์สไปรท์ DAY COMPLETE! เด้งขยายจาก 0 แบบเกินขนาดแล้วหดกลับ (ease-out back) พร้อมจางเข้า
    // ก่อนม่านจะเลื่อนลงมาปิดจอ ถ้าไม่ได้ผูก dayCompleteBanner ไว้ใน Inspector จะข้ามขั้นตอนนี้ไปเลย
    private IEnumerator PlayDayCompleteBanner()
    {
        if (dayCompleteBanner == null) yield break;

        dayCompleteBanner.gameObject.SetActive(true);

        // ดังพร้อมจังหวะที่ป้ายเริ่มเด้งขึ้นมา ไม่ใช่ตอนม่านปิด (นั่นคือ ui_shutter)
        SoundManager.Instance.PlaySFX(SoundIds.UiDayComplete);

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
