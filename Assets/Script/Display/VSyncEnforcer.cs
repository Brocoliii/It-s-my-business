using UnityEngine;

// ตัวเฝ้าดูว่า VSync ที่สั่งไปกดเฟรมได้จริงหรือเปล่า
//
// ทำไมต้องมี: การ์ดจอตั้งทับเกมได้ (NVIDIA Control Panel / AMD Software ตั้ง "Vertical sync: Off")
// พอโดนตั้งทับ QualitySettings.vSyncCount = 1 ก็เงียบไปเฉย ๆ เฟรมยังวิ่งเกิน refresh rate ของจอ
// และจะสั่ง Application.targetFrameRate ช่วยก็ไม่ได้ เพราะ Unity ทิ้งค่านั้นทั้งหมดตอน vSyncCount ไม่ใช่ 0
//
// ตัวนี้เลยวัดเฟรมจริงหลังเปิด VSync ถ้าจับได้ว่าไม่ถูกกดลง
// จะสลับไปใช้วิธีล็อกเฟรมด้วย targetFrameRate แทน (ปิด vSyncCount ก่อน ไม่งั้นค่าถูกทิ้ง)
// เฟรมจะได้ไม่วิ่งเกินจอเหมือนที่ผู้เล่นสั่งไว้ แม้จะกันภาพฉีกได้ไม่ดีเท่า VSync ของจริง
//
// ไม่ต้องวางในฉาก DisplaySettings สร้างตัวนี้ขึ้นมาเองตอนต้องใช้
public class VSyncEnforcer : MonoBehaviour
{
    private const string ObjectName = "VSyncEnforcer";

    // เฟรมแรก ๆ หลังเปลี่ยนโหมดจอมันกระตุกและเด้งเกินจริง รอให้นิ่งก่อนค่อยเริ่มนับ
    private const float SettleDuration = 0.5f;
    private const float SampleDuration = 1.5f;

    // ยอมให้เกิน refresh rate ได้นิดหน่อย จอ 60Hz วัดได้ 62 ถือว่า VSync ทำงานอยู่
    private const float OvershootRatio = 1.15f;

    private const int FallbackRefreshRate = 60;

    private static VSyncEnforcer instance;

    private bool isChecking = false;
    private float settleElapsed = 0f;
    private float sampleElapsed = 0f;
    private int sampleFrames = 0;

    // เริ่มวัดรอบใหม่ เรียกทุกครั้งที่เปิด VSync หรือเปลี่ยนโหมดจอ
    public static void BeginCheck()
    {
        EnsureExists();
        if (instance == null) return;

        instance.isChecking = true;
        instance.settleElapsed = 0f;
        instance.sampleElapsed = 0f;
        instance.sampleFrames = 0;
    }

    // ผู้เล่นปิด VSync ระหว่างที่ยังวัดอยู่ ก็ไม่ต้องวัดต่อแล้ว
    public static void CancelCheck()
    {
        if (instance == null) return;
        instance.isChecking = false;
    }

    private static void EnsureExists()
    {
        if (instance != null) return;

        GameObject go = new GameObject(ObjectName);
        instance = go.AddComponent<VSyncEnforcer>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        // กันเคสมีคนเผลอลากสคริปต์นี้ใส่ฉากเองด้วย จะได้ไม่วัดซ้อนกันสองตัว
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ใช้ unscaledDeltaTime เพราะแผงตั้งค่าเปิดจากเมนูหยุดเกมที่ timeScale = 0 อยู่
    private void Update()
    {
        if (!isChecking) return;

        if (settleElapsed < SettleDuration)
        {
            settleElapsed += Time.unscaledDeltaTime;
            return;
        }

        sampleElapsed += Time.unscaledDeltaTime;
        sampleFrames++;

        if (sampleElapsed < SampleDuration) return;

        isChecking = false;

        float measuredFps = sampleFrames / sampleElapsed;
        int refreshRate = DisplaySettings.CurrentRefreshRate;
        if (refreshRate <= 0) refreshRate = FallbackRefreshRate;

        // เฟรมต่ำกว่า refresh rate อยู่แล้วก็ไม่ต้องทำอะไร (เครื่องรันไม่ทันเอง ไม่ใช่ VSync พัง)
        if (measuredFps <= refreshRate * OvershootRatio) return;

        DisplaySettings.UseFrameCapFallback(refreshRate, measuredFps);
    }
}
