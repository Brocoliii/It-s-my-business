using UnityEngine;

// ค่าตั้งค่ากล้องที่ผู้เล่นปรับเองได้ (ความเร็วแพนกล้อง, ความหนืด, การไถลต่อ)
// เก็บไว้ในเครื่องเหมือนค่าเสียง เปิดเกมใหม่ก็ยังเป็นค่าเดิม
//
// *** ห้ามลากไฟล์นี้ไปใส่ GameObject *** (Unity จะฟ้องว่า "The script class can't be abstract!")
// ไฟล์นี้ไม่ใช่ MonoBehaviour เป็นแค่ที่เก็บค่ากลาง วางไว้เฉย ๆ ในโฟลเดอร์พอ
// ตัวที่ต้องลากใส่แผง Settings Panel คือ CameraSettingsUI
//
// ที่ทำเป็น static เพราะแผงตั้งค่าอยู่ฉากเมนูหลัก แต่กล้องอยู่ฉากด่าน
// คนละฉากกันเลยส่งค่าหากันผ่านตรงนี้ ไม่ต้องมี GameObject คอยถือค่าข้ามฉาก
public static class CameraSettings
{
    private const string PanSpeedKey = "cam_pan_speed_scale";
    private const string PanSmoothKey = "cam_pan_smooth_enabled";
    private const string PanInertiaKey = "cam_pan_inertia_enabled";

    // ช่วงที่ให้ผู้เล่นลากได้ 0.2 = ช้ากว่าปกติ 5 เท่า, 3 = เร็วกว่าปกติ 3 เท่า
    // ไม่ให้ลากถึง 0 เพราะกล้องจะขยับไม่ได้เลยแล้วผู้เล่นจะนึกว่าเกมค้าง
    public const float MinPanSpeedScale = 0.01f;
    public const float MaxPanSpeedScale = 3f;
    public const float DefaultPanSpeedScale = 1f;

    // เปิดทั้งคู่เป็นค่าเริ่มต้น กล้องจะได้นุ่มเหมือนเดิมสำหรับคนที่ไม่เคยเข้าหน้าตั้งค่า
    public const bool DefaultPanSmoothEnabled = true;
    public const bool DefaultPanInertiaEnabled = true;

    private static float panSpeedScale = DefaultPanSpeedScale;
    private static bool panSmoothEnabled = DefaultPanSmoothEnabled;
    private static bool panInertiaEnabled = DefaultPanInertiaEnabled;
    private static bool isLoaded = false;

    // ตัวคูณความเร็วแพนกล้อง 1 = ความเร็วปกติตามที่ตั้งไว้ในสคริปต์ CameraController
    public static float PanSpeedScale
    {
        get
        {
            EnsureLoaded();
            return panSpeedScale;
        }
    }

    // ความหนืดตอนแพนกล้อง (กล้องค่อย ๆ ไล่ตามเมาส์แทนที่จะติดมือทันที)
    // ปิดแล้วกล้องจะตามมือแบบตรง ๆ เหมาะกับคนที่รู้สึกว่ากล้องหน่วง หรือคนที่เมาภาพ
    public static bool PanSmoothEnabled
    {
        get
        {
            EnsureLoaded();
            return panSmoothEnabled;
        }
    }

    // แรงไถลหลังปล่อยเมาส์ ปิดแล้วกล้องจะหยุดตรงจุดที่ปล่อยเลย
    public static bool PanInertiaEnabled
    {
        get
        {
            EnsureLoaded();
            return panInertiaEnabled;
        }
    }

    // เปลี่ยนค่าเฉย ๆ ยังไม่เขียนลงเครื่อง (ลากสไลเดอร์ทีนึงยิงหลายสิบครั้ง เขียนดิสก์ทุกครั้งจะหน่วง)
    // ตอนปิดแผงตั้งค่าค่อยเรียก Save() ทีเดียว
    public static void SetPanSpeedScale(float value)
    {
        EnsureLoaded();
        panSpeedScale = Mathf.Clamp(value, MinPanSpeedScale, MaxPanSpeedScale);
    }

    public static void SetPanSmoothEnabled(bool value)
    {
        EnsureLoaded();
        panSmoothEnabled = value;
    }

    public static void SetPanInertiaEnabled(bool value)
    {
        EnsureLoaded();
        panInertiaEnabled = value;
    }

    public static void ResetToDefault()
    {
        SetPanSpeedScale(DefaultPanSpeedScale);
        SetPanSmoothEnabled(DefaultPanSmoothEnabled);
        SetPanInertiaEnabled(DefaultPanInertiaEnabled);
    }

    public static void Save()
    {
        EnsureLoaded();
        PlayerPrefs.SetFloat(PanSpeedKey, panSpeedScale);
        // PlayerPrefs ไม่มีช่องเก็บ bool เลยเก็บเป็น 0/1 เอา
        PlayerPrefs.SetInt(PanSmoothKey, panSmoothEnabled ? 1 : 0);
        PlayerPrefs.SetInt(PanInertiaKey, panInertiaEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static void EnsureLoaded()
    {
        if (isLoaded) return;
        isLoaded = true;

        float saved = PlayerPrefs.GetFloat(PanSpeedKey, DefaultPanSpeedScale);
        panSpeedScale = Mathf.Clamp(saved, MinPanSpeedScale, MaxPanSpeedScale);

        panSmoothEnabled = PlayerPrefs.GetInt(PanSmoothKey, DefaultPanSmoothEnabled ? 1 : 0) != 0;
        panInertiaEnabled = PlayerPrefs.GetInt(PanInertiaKey, DefaultPanInertiaEnabled ? 1 : 0) != 0;
    }
}
