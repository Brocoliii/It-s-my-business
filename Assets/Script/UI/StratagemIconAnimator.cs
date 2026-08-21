using UnityEngine;
using UnityEngine.UI;

// แอนิเมชันของไอคอน Stratagem ทีละตัว (ใช้กับไอคอนในพูลของ UIManager และกับข้อความ fallback ด้วย)
// แยกเป็นคอมโพเนนต์เพราะ UIManager สร้างไอคอนตอนรันไทม์ แล้ว AddComponent ตัวนี้ให้เอง
// จึงไม่ต้องแก้ Prefab หรือผูกอะไรเพิ่มใน Inspector
//
// สำคัญ: ขยับได้เฉพาะ localScale / localRotation / color เท่านั้น
// ห้ามแตะ anchoredPosition ของไอคอน เพราะถ้า container เป็น Layout Group
// ค่าตำแหน่งจะโดน Layout เขียนทับตอน rebuild แล้วภาพจะกระตุก
// (การสะบัดทั้งแถวไปทำที่ตัว container ใน UIManager แทน)
[DisallowMultipleComponent]
public class StratagemIconAnimator : MonoBehaviour
{
    private Graphic targetGraphic;
    private RectTransform targetRect;
    private Vector3 baseScale = Vector3.one;
    private bool isBound = false;

    // แอนิเมชันเด้งขึ้นตอนขึ้นชุดปุ่มใหม่
    private bool isAppearing = false;
    private float appearTimer = 0f;
    private float appearDelay = 0f;
    private float appearDuration = 0.25f;
    private float appearRotation = 0f;

    // แอนิเมชันเด้งตอนกดถูก
    private bool isPunching = false;
    private float punchTimer = 0f;
    private float punchDelay = 0f;
    private float punchDuration = 0.2f;
    private float punchAmount = 0f;
    private float punchRotation = 0f;

    // แอนิเมชันสะบัดตอนกดผิด
    private float shakeTimer = 0f;
    private float shakeDuration = 0.4f;
    private float shakeAngle = 0f;
    private float shakeSquash = 0f;
    private const float ShakeFrequency = 26f;

    // ไอคอนตัวที่กำลังรอให้กดจะหายใจเบา ๆ ให้รู้ว่าถึงคิวมันแล้ว
    private bool isCurrentStep = false;
    private float pulsePhase = 0f;
    private float pulseSpeed = 1.6f;
    private float pulseAmount = 0.08f;

    private Color currentColor = Color.white;
    private Color targetColor = Color.white;
    private Color flashOverlayColor = Color.white;
    private float flashTimer = 0f;
    private const float ColorLerpSpeed = 16f;

    public void Bind(Graphic graphic)
    {
        // ผูกครั้งเดียวตอนสร้างไอคอนเท่านั้น
        // ถ้าผูกซ้ำระหว่างแอนิเมชันกำลังเล่น จะไปจำสเกลที่ถูกย่ออยู่ (อาจเป็น 0) มาเป็นสเกลฐาน
        if (isBound || graphic == null)
            return;

        targetGraphic = graphic;
        targetRect = graphic.rectTransform;

        if (targetRect != null && targetRect.localScale != Vector3.zero)
        {
            baseScale = targetRect.localScale;
        }

        currentColor = graphic.color;
        targetColor = currentColor;
        isBound = true;
    }

    public void SetPulse(float speed, float amount)
    {
        pulseSpeed = speed;
        pulseAmount = amount;
    }

    // เด้งขึ้นจากสเกล 0 แบบ ease-out back
    // delay ใช้ไล่ให้ไอคอนแต่ละตัวทยอยขึ้นทีละตัวแทนที่จะโผล่พร้อมกันหมด
    public void PlayAppear(float delay, float duration, float rotationKick)
    {
        if (!isBound)
            return;

        isAppearing = true;
        appearTimer = 0f;
        appearDelay = Mathf.Max(0f, delay);
        appearDuration = Mathf.Max(0.01f, duration);
        appearRotation = rotationKick;

        // ล้างแอนิเมชันค้างจากชุดก่อนหน้า ไม่งั้นไอคอนตัวเดิมจะเด้งซ้อนกับที่กำลังขึ้นใหม่
        isPunching = false;
        shakeTimer = 0f;
    }

    public void PlayPunch(float amount, float duration, float delay, float rotationKick)
    {
        if (!isBound)
            return;

        isPunching = true;
        punchTimer = 0f;
        punchDelay = Mathf.Max(0f, delay);
        punchDuration = Mathf.Max(0.01f, duration);
        punchAmount = amount;
        punchRotation = rotationKick;
    }

    public void PlayShake(float angle, float squash, float duration)
    {
        if (!isBound)
            return;

        shakeAngle = angle;
        shakeSquash = squash;
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = shakeDuration;
    }

    // instant = true ใช้ตอนแฟลชสีตอบสนองการกด ต้องเปลี่ยนสีทันทีไม่ให้ค่อย ๆ ไล่สี
    // ไม่งั้นแฟลชสั้น ๆ 0.15 วิ จะไล่สีไปไม่ทันจนแทบมองไม่เห็น
    public void SetColor(Color color, bool instant)
    {
        targetColor = color;

        if (instant)
        {
            currentColor = color;
        }
    }

    // ทาสีทับชั่วคราวแล้วค่อยไล่กลับไปสีเดิมเอง (ใช้กับข้อความ fallback ที่ไม่มีสีรายไอคอน)
    public void FlashColor(Color color, float duration)
    {
        flashOverlayColor = color;
        flashTimer = Mathf.Max(0f, duration);
    }

    public void SetCurrentStep(bool isCurrent)
    {
        if (isCurrentStep == isCurrent)
            return;

        isCurrentStep = isCurrent;
        pulsePhase = 0f;
    }

    // คืนค่าทุกอย่างกลับสภาพเดิมตอนซ่อน HUD
    // ถ้าไม่คืน ไอคอนจะถูกเก็บเข้าพูลทั้งที่สเกล/มุมยังเพี้ยน แล้วรอบหน้าจะโผล่มาเบี้ยว
    public void ResetState()
    {
        isAppearing = false;
        isPunching = false;
        shakeTimer = 0f;
        flashTimer = 0f;
        isCurrentStep = false;
        pulsePhase = 0f;
        currentColor = targetColor;

        if (targetRect != null)
        {
            targetRect.localScale = baseScale;
            targetRect.localRotation = Quaternion.identity;
        }

        ApplyColor(1f);
    }

    private void LateUpdate()
    {
        if (!isBound || targetRect == null)
            return;

        float dt = Time.deltaTime;
        float scaleMultiplier = 1f;
        float squash = 0f;
        float rotation = 0f;
        float alphaMultiplier = 1f;

        if (isAppearing)
        {
            appearTimer += dt;

            if (appearTimer < appearDelay)
            {
                // ยังไม่ถึงคิวของไอคอนตัวนี้ ย่อไว้ที่ 0 และซ่อนสีไว้ก่อน
                targetRect.localScale = Vector3.zero;
                ApplyColor(0f);
                return;
            }

            float t = Mathf.Clamp01((appearTimer - appearDelay) / appearDuration);
            scaleMultiplier *= EaseOutBack(t);
            alphaMultiplier *= Mathf.Clamp01(t * 3f);
            rotation += appearRotation * (1f - t) * Mathf.Cos(t * Mathf.PI * 2f);

            if (t >= 1f)
            {
                isAppearing = false;
            }
        }

        if (isPunching)
        {
            punchTimer += dt;
            float elapsed = punchTimer - punchDelay;

            if (elapsed >= 0f)
            {
                float t = Mathf.Clamp01(elapsed / punchDuration);
                float wave = Mathf.Sin(t * Mathf.PI);
                scaleMultiplier *= 1f + punchAmount * wave;
                rotation += punchRotation * wave * Mathf.Sin(t * Mathf.PI * 2f);

                if (t >= 1f)
                {
                    isPunching = false;
                }
            }
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            float remaining = Mathf.Clamp01(shakeTimer / shakeDuration);
            float wave = Mathf.Sin(Time.time * ShakeFrequency);
            rotation += shakeAngle * wave * remaining;
            squash += shakeSquash * wave * remaining;
        }

        if (isCurrentStep && !isAppearing)
        {
            pulsePhase += dt * pulseSpeed;
            float breathe = (Mathf.Sin(pulsePhase * Mathf.PI * 2f) + 1f) * 0.5f;
            scaleMultiplier *= 1f + pulseAmount * breathe;
        }

        Vector3 scale = baseScale * scaleMultiplier;
        scale.x *= 1f + squash;
        scale.y *= 1f - squash;
        targetRect.localScale = scale;
        targetRect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        if (flashTimer > 0f)
        {
            flashTimer -= dt;
            currentColor = flashOverlayColor;
        }
        else
        {
            currentColor = Color.Lerp(currentColor, targetColor, 1f - Mathf.Exp(-ColorLerpSpeed * dt));
        }

        ApplyColor(alphaMultiplier);
    }

    private void ApplyColor(float alphaMultiplier)
    {
        if (targetGraphic == null)
            return;

        Color c = currentColor;
        c.a *= alphaMultiplier;
        targetGraphic.color = c;
    }

    private static float EaseOutBack(float t)
    {
        float overshoot = 1.70158f;
        float shifted = t - 1f;
        return 1f + (overshoot + 1f) * Mathf.Pow(shifted, 3f) + overshoot * Mathf.Pow(shifted, 2f);
    }
}
