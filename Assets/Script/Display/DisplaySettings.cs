using System.Collections.Generic;
using UnityEngine;

// ค่าตั้งค่าภาพที่ผู้เล่นปรับเองได้ (ความละเอียดจอ, เต็มจอ/หน้าต่าง, VSync)
// เก็บไว้ในเครื่องเหมือนค่าเสียงกับค่ากล้อง เปิดเกมใหม่ก็ยังเป็นค่าเดิม
//
// *** ห้ามลากไฟล์นี้ไปใส่ GameObject *** (Unity จะฟ้องว่า "The script class can't be abstract!")
// ไฟล์นี้ไม่ใช่ MonoBehaviour เป็นแค่ที่เก็บค่ากลาง วางไว้เฉย ๆ ในโฟลเดอร์พอ
// ตัวที่ต้องลากใส่แผง Settings Panel คือ DisplaySettingsUI
//
// ไม่ต้องวางอะไรในฉากเพื่อให้ค่าถูกใช้ตอนเปิดเกม ApplyOnGameStart() ข้างล่างสั่งเองให้แล้ว
public static class DisplaySettings
{
    private const string WidthKey = "display_res_width";
    private const string HeightKey = "display_res_height";
    private const string ModeKey = "display_mode";
    // คีย์เก่าสมัยที่ยังมีแค่ เต็มจอ/หน้าต่าง เก็บไว้อ่านค่าของคนที่เคยเล่นเวอร์ชันก่อนหน้า
    private const string LegacyFullscreenKey = "display_fullscreen";
    private const string VSyncKey = "display_vsync";

    public const bool DefaultVSyncEnabled = true;

    // โหมดการแสดงผลที่ผู้เล่นเลือกได้ 3 แบบ
    //
    // Fullscreen (เต็มจอของจริง) = สั่งให้ตัวจอเปลี่ยนโหมดตามความละเอียดที่เลือกจริง ๆ
    //   ภาพเลยเต็มจอพอดีทุกความละเอียด ไม่ต้องขยายภาพ ไม่มีขอบดำ สีไม่เพี้ยน
    //   เป็นโหมดเดียวที่เลือกความละเอียดต่ำกว่าจอแล้วยังได้ภาพเต็มจอสวย ๆ
    //
    // Borderless (เต็มจอแบบไร้ขอบ) = หน้าต่างไม่มีขอบขนาดเท่าจอ ไม่ไปแตะโหมดของจอเลย
    //   สลับไปแอปอื่นแล้วกลับมาไวที่สุด เหมาะกับคนที่เปิดโปรแกรมอื่นคู่ไปด้วย
    //   แลกกับใช้ความละเอียดเท่าจอเสมอ เลือกต่ำกว่านั้นไม่ได้ (ดู EffectiveResolution ว่าทำไม)
    //
    // Windowed (หน้าต่าง) = หน้าต่างมีขอบ ขนาดตามความละเอียดที่เลือก
    //
    // *** ห้ามสลับลำดับตัวเลข *** ค่านี้ถูกเก็บลง PlayerPrefs เป็นตัวเลข
    // สลับลำดับเมื่อไร คนที่เคยตั้งค่าไว้จะเปิดเกมมาเจอโหมดอื่นทันที
    public enum DisplayMode
    {
        Fullscreen = 0,
        Borderless = 1,
        Windowed = 2,
    }

    public const DisplayMode DefaultMode = DisplayMode.Fullscreen;

    // ยอมให้สัดส่วนต่างจากจอได้เท่าไรถึงยังนับว่าเป็นสัดส่วนเดียวกัน
    // 1366x768 (1.779) กับ 1920x1080 (1.778) ไม่เท่ากันเป๊ะแต่ต้องถือว่าเป็น 16:9 เหมือนกัน
    private const float AspectTolerance = 0.02f;

    // เผื่อเครื่องที่ Screen.resolutions คืนค่าว่าง (เจอได้บนบางแพลตฟอร์ม) จะได้ยังมีตัวเลือกให้เลือก
    private static readonly Vector2Int[] FallbackResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
    };

    private static Vector2Int[] availableResolutions;
    private static Vector2Int nativeResolution;
    private static Vector2Int resolution;
    private static DisplayMode mode = DefaultMode;
    private static bool vSyncEnabled = DefaultVSyncEnabled;
    private static bool isLoaded = false;
    private static bool hasWarnedEditorVSync = false;
    private static bool isUsingFrameCapFallback = false;

    // true = VSync ของจริงใช้ไม่ได้ (โดนตั้งทับจากนอกเกม) ตอนนี้กำลังล็อกเฟรมด้วย targetFrameRate แทนอยู่
    public static bool IsUsingFrameCapFallback
    {
        get { return isUsingFrameCapFallback; }
    }

    // รายการความละเอียดที่จอเครื่องนี้รองรับ เรียงจากเล็กไปใหญ่
    // ตัดตัวซ้ำออกแล้ว (จอเดียวกันมักคืนความละเอียดเดิมมาหลายรอบตาม refresh rate ที่ต่างกัน)
    public static Vector2Int[] AvailableResolutions
    {
        get
        {
            if (availableResolutions == null) BuildResolutionList();
            return availableResolutions;
        }
    }

    // ความละเอียดจริงของจอ (ตัวใหญ่สุดที่จอรองรับ) หามาครั้งเดียวตอนสร้างรายการแล้วจำไว้
    //
    // ห้ามไปอ่าน Screen.currentResolution สด ๆ ระหว่างเกมแทน เพราะพอสั่งเต็มจอของจริงที่ความละเอียดต่ำ
    // ตัวจอจะเปลี่ยนโหมดตามจริง ค่านั้นก็จะกลายเป็นความละเอียดที่เพิ่งสั่งไป ไม่ใช่ของจอ
    // แล้วเกมจะไม่รู้อีกเลยว่าจอจริง ๆ ใหญ่เท่าไร (เลือกกลับไปตัวเต็มจอก็จะยังนึกว่าต้องเต็มจอของจริงอยู่)
    public static Vector2Int NativeResolution
    {
        get
        {
            if (availableResolutions == null) BuildResolutionList();
            return nativeResolution;
        }
    }

    public static Vector2Int Resolution
    {
        get
        {
            EnsureLoaded();
            return resolution;
        }
    }

    public static DisplayMode Mode
    {
        get
        {
            EnsureLoaded();
            return mode;
        }
    }

    // จริงทั้งโหมดเต็มจอของจริงและโหมดไร้ขอบ (สองอันนี้เต็มจอเหมือนกัน ต่างกันแค่วิธี)
    public static bool IsFullscreen
    {
        get { return Mode != DisplayMode.Windowed; }
    }

    // โหมดไร้ขอบไม่ให้เลือกความละเอียด ตัวเลือกในหน้าตั้งค่าเลยควรถูกปิดไว้ตอนอยู่โหมดนี้
    public static bool CanChooseResolution
    {
        get { return Mode != DisplayMode.Borderless; }
    }

    // ความละเอียดที่จะสั่งใช้จริง ไม่ใช่ตัวที่ผู้เล่นเลือกไว้เสมอไป
    //
    // โหมดไร้ขอบต้องเท่าจอเท่านั้น เพราะ Windows ให้หน้าต่างเท่าจออยู่แล้วไม่ว่าเราจะสั่งอะไร
    // ถ้าไปสั่งเลขที่ต่ำกว่านั้น Unity ต้องวาดภาพเล็กแล้วขยายยัดใส่หน้าต่างใหญ่เอง
    // รอบขยายภาพนั้นแหละที่ทำให้ได้ขอบดำ (ถ้าสัดส่วนไม่ตรง) และสีเพี้ยน
    // อยากได้ความละเอียดต่ำแบบเต็มจอสวย ๆ ต้องไปใช้โหมดเต็มจอของจริงแทน
    //
    // ตัวที่ผู้เล่นเลือกไว้ไม่ได้ถูกทับทิ้ง แค่ไม่ได้ถูกใช้ตอนอยู่โหมดไร้ขอบ
    // สลับกลับไปโหมดอื่นเมื่อไรก็ได้ความละเอียดเดิมคืนทันที
    private static Vector2Int EffectiveResolution
    {
        get
        {
            if (mode != DisplayMode.Borderless) return resolution;

            Vector2Int native = NativeResolution;
            return (native.x > 0 && native.y > 0) ? native : resolution;
        }
    }

    public static bool VSyncEnabled
    {
        get
        {
            EnsureLoaded();
            return vSyncEnabled;
        }
    }

    // refresh rate ของจอตอนนี้ (60, 144, ...) ใช้เทียบว่าเฟรมที่เห็นคือ VSync ล็อกอยู่จริงหรือเปล่า
    // เปิด VSync แล้วเฟรมควรนิ่งที่เลขนี้ ถ้าวิ่งเกินไปมากแปลว่า VSync ไม่ได้ทำงาน
    public static int CurrentRefreshRate
    {
        get
        {
            // Unity 2022.2 เปลี่ยนช่องนี้เป็น refreshRateRatio ตัวเก่าถูกประกาศเลิกใช้
            // เขียนแยกไว้ทั้งสองแบบ โปรเจกต์จะอยู่เวอร์ชันไหนก็คอมไพล์ผ่าน
#if UNITY_2022_2_OR_NEWER
            return Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
#else
            return Screen.currentResolution.refreshRate;
#endif
        }
    }

    // ตำแหน่งที่ Dropdown ควรชี้อยู่ตอนนี้
    //
    // ใช้ความละเอียดที่สั่งใช้จริง ไม่ใช่ตัวที่ผู้เล่นเลือกไว้ เพราะตอนอยู่โหมดไร้ขอบสองอันนี้ไม่ตรงกัน
    // ถ้าไปโชว์ตัวที่เลือกไว้ ผู้เล่นจะเห็น Dropdown เขียน 1280x720 ทั้งที่จอกำลังรัน 1920x1080 อยู่
    public static int ResolutionIndex
    {
        get
        {
            EnsureLoaded();
            return NearestIndex(EffectiveResolution);
        }
    }

    // หาตำแหน่งของความละเอียดที่ให้มาในรายการ
    // ถ้าไม่มีในรายการแล้ว (ย้ายไปเสียบจออื่น) จะคืนตัวที่ใกล้เคียงที่สุดแทน
    private static int NearestIndex(Vector2Int value)
    {
        Vector2Int[] list = AvailableResolutions;
        int nearestIndex = 0;
        long nearestDistance = long.MaxValue;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == value) return i;

            long dw = list[i].x - value.x;
            long dh = list[i].y - value.y;
            long distance = dw * dw + dh * dh;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    public static void SetResolutionIndex(int index)
    {
        Vector2Int[] list = AvailableResolutions;
        if (list.Length == 0) return;

        index = Mathf.Clamp(index, 0, list.Length - 1);
        SetResolution(list[index]);
    }

    // เปลี่ยนแล้วสั่งใช้เลย ไม่เหมือนสไลเดอร์เสียงที่รอปิดหน้าค่อยเขียน
    // เพราะผู้เล่นต้องเห็นผลทันทีถึงจะรู้ว่าเลือกอันที่ต้องการหรือยัง
    public static void SetResolution(Vector2Int value)
    {
        EnsureLoaded();
        if (value.x <= 0 || value.y <= 0) return;

        resolution = value;
        Apply();
    }

    public static void SetMode(DisplayMode value)
    {
        EnsureLoaded();
        mode = value;
        Apply();
    }

    public static void SetVSyncEnabled(bool value)
    {
        EnsureLoaded();
        vSyncEnabled = value;
        ApplyVSync();
    }

    // เปิด/ปิด VSync ต้องสั่งสองอย่างคู่กัน ไม่ใช่แค่ vSyncCount
    //
    // targetFrameRate ที่ค้างอยู่จะกดเฟรมทับ VSync จนดูเหมือน VSync ไม่ทำงาน
    // (บางแพลตฟอร์ม Unity ตั้งมาให้เอง และค่านี้อยู่ข้ามฉาก ใครเผลอตั้งไว้ทีเดียวก็ค้างยาว)
    // -1 = ไม่จำกัดเฟรม ปล่อยให้ VSync เป็นคนคุมแทนตามที่ผู้เล่นเลือก
    private static void ApplyVSync()
    {
        // เริ่มจากขอ VSync ของจริงก่อนเสมอ (กันภาพฉีกได้ดีกว่าการล็อกเฟรมเฉย ๆ)
        // ถ้าการ์ดจอตั้งทับจนใช้ไม่ได้ VSyncEnforcer จะสลับไปวิธีล็อกเฟรมให้เองทีหลัง
        isUsingFrameCapFallback = false;
        QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        Application.targetFrameRate = -1;

        // ใน Editor ค่านี้สั่งไปก็ไม่มีผลกับหน้าต่าง Game view
        // Unity ให้ Game view ใช้สวิตช์ VSync ของตัวเอง (ปุ่มบนแถบเครื่องมือของ Game view)
        // เลยต้องบอกไว้ ไม่งั้นจะนึกว่าโค้ดพัง ทั้งที่ต้อง Build ถึงจะเห็นผลจริง
        // และไม่ต้องวัดเฟรมด้วย ใน Editor ยังไงก็วัดได้ว่า "ไม่ถูกกด" ทุกรอบ
        if (Application.isEditor)
        {
            if (!hasWarnedEditorVSync)
            {
                hasWarnedEditorVSync = true;
                Debug.LogWarning("[DisplaySettings] VSync ตั้งค่าไว้แล้ว (vSyncCount = " + QualitySettings.vSyncCount +
                    ") แต่ใน Editor จะไม่เห็นผล เพราะ Game view ใช้สวิตช์ VSync ของตัวเอง ต้อง Build ออกมาทดสอบ");
            }
            return;
        }

        if (vSyncEnabled) VSyncEnforcer.BeginCheck();
        else VSyncEnforcer.CancelCheck();
    }

    // เรียกจาก VSyncEnforcer ตอนจับได้ว่าเปิด VSync แล้วเฟรมยังวิ่งเกิน refresh rate ของจอ
    // แปลว่ามีอะไรตั้งทับอยู่ (ส่วนใหญ่คือการ์ดจอ) เลยหันไปล็อกเฟรมด้วย targetFrameRate แทน
    public static void UseFrameCapFallback(int frameRate, float measuredFps)
    {
        if (frameRate <= 0) return;

        // ต้องปิด vSyncCount ก่อน ไม่งั้น Unity ทิ้ง targetFrameRate ทั้งหมด แล้วจะไม่ได้อะไรเลยสักทาง
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = frameRate;
        isUsingFrameCapFallback = true;

        Debug.LogWarning("[DisplaySettings] เปิด VSync แล้วแต่เฟรมยังวิ่งถึง " + Mathf.RoundToInt(measuredFps) +
            " ทั้งที่จอเป็น " + frameRate + "Hz แปลว่า VSync ถูกตั้งทับจากนอกเกม (ส่วนใหญ่คือการ์ดจอ)" +
            " เปลี่ยนไปล็อกเฟรมที่ " + frameRate + " แทน" +
            " อยากได้ VSync ของจริงต้องไปตั้ง Vertical sync ในโปรแกรมการ์ดจอเป็น \"Use the 3D application setting\"");
    }

    public static void ResetToDefault()
    {
        EnsureLoaded();

        // ค่าเริ่มต้นของความละเอียดคือความละเอียดจริงของจอ
        Vector2Int native = NativeResolution;
        if (native.x > 0 && native.y > 0) resolution = native;

        mode = DefaultMode;
        SetVSyncEnabled(DefaultVSyncEnabled);
        Apply();
    }

    // สั่งให้ค่าปัจจุบันมีผลกับจอจริง
    public static void Apply()
    {
        EnsureLoaded();

        ApplyVSync();

        // ใน Editor การสั่งเปลี่ยนความละเอียดไปยุ่งกับหน้าต่าง Game view เฉย ๆ ไม่ได้บอกอะไรจริง
        // ต้อง Build ออกมาเป็นเกมจริงถึงจะเห็นผล เลยข้ามส่วนนี้ไปตอนกดเล่นใน Editor
        if (Application.isEditor) return;

        FullScreenMode unityMode = ToUnityMode(mode);
        Vector2Int target = EffectiveResolution;

        bool sameSize = Screen.width == target.x && Screen.height == target.y;
        bool sameMode = Screen.fullScreenMode == unityMode;

        // ค่าเดิมอยู่แล้วก็ไม่ต้องสั่งซ้ำ
        // นอกจากจอจะกะพริบเปล่า ๆ แล้ว การสั่งซ้ำยังทำให้ backbuffer (ที่พักภาพก่อนออกจอ)
        // ถูกรื้อสร้างใหม่โดยไม่จำเป็นด้วย ซึ่งเป็นจังหวะที่สีมีสิทธิ์เพี้ยน
        // (Apply() ถูกเรียกทุกครั้งที่แตะสวิตช์ VSync ไม่ใช่แค่ตอนเปลี่ยนความละเอียด)
        if (sameSize && sameMode)
        {
            DisplayModeSettler.Cancel();
            return;
        }

        // เปลี่ยนแค่ขนาด ไม่ได้เปลี่ยนโหมด สั่งไปตรง ๆ ได้เลย
        if (sameMode)
        {
            DisplayModeSettler.Cancel();
            Screen.SetResolution(target.x, target.y, unityMode);
            return;
        }

        // เปลี่ยนโหมด: แยกเป็นสองจังหวะ เปลี่ยนโหมดก่อนโดยคงขนาดเดิมไว้
        // แล้วให้ DisplayModeSettler มาสั่งขนาดจริงทีหลัง (เหตุผลอยู่ในไฟล์นั้น)
        Screen.SetResolution(Screen.width, Screen.height, unityMode);
        DisplayModeSettler.Schedule(target, unityMode);
    }

    // แปลงโหมดที่ผู้เล่นเลือก เป็นค่าที่ Unity ใช้จริง
    private static FullScreenMode ToUnityMode(DisplayMode value)
    {
        switch (value)
        {
            case DisplayMode.Borderless: return FullScreenMode.FullScreenWindow;
            case DisplayMode.Windowed: return FullScreenMode.Windowed;
            default: return FullScreenMode.ExclusiveFullScreen;
        }
    }

    // กันค่าที่อ่านจาก PlayerPrefs เพี้ยน (ไฟล์โดนแก้มือ หรือมาจากเวอร์ชันที่ลำดับโหมดไม่ตรงกัน)
    private static DisplayMode ToDisplayMode(int value)
    {
        if (value == (int)DisplayMode.Borderless) return DisplayMode.Borderless;
        if (value == (int)DisplayMode.Windowed) return DisplayMode.Windowed;
        return DisplayMode.Fullscreen;
    }

    public static void Save()
    {
        EnsureLoaded();
        PlayerPrefs.SetInt(WidthKey, resolution.x);
        PlayerPrefs.SetInt(HeightKey, resolution.y);
        PlayerPrefs.SetInt(ModeKey, (int)mode);
        // PlayerPrefs ไม่มีช่องเก็บ bool เลยเก็บเป็น 0/1 เอา
        PlayerPrefs.SetInt(VSyncKey, vSyncEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // เรียกเองตอนเกมเริ่ม ก่อนฉากแรกจะโหลด ผู้เล่นจะได้ไม่ต้องเข้าหน้าตั้งค่าทุกครั้งที่เปิดเกม
    // (Unity จำความละเอียดให้เองอยู่แล้ว แต่ไม่จำ VSync เลยต้องสั่งเองตรงนี้)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnGameStart()
    {
        EnsureLoaded();
        Apply();
    }

    private static void EnsureLoaded()
    {
        if (isLoaded) return;
        isLoaded = true;

        // ค่าเริ่มต้นคือสภาพจอตอนนี้ คนที่ไม่เคยเข้าหน้าตั้งค่าจะได้ไม่โดนเปลี่ยนอะไรเลย
        int defaultWidth = Screen.width;
        int defaultHeight = Screen.height;

        int savedWidth = PlayerPrefs.GetInt(WidthKey, defaultWidth);
        int savedHeight = PlayerPrefs.GetInt(HeightKey, defaultHeight);

        // กันค่าเพี้ยน (ไฟล์ PlayerPrefs โดนแก้มือ หรือเคยบันทึกตอนจอยังไม่พร้อม)
        if (savedWidth <= 0 || savedHeight <= 0)
        {
            savedWidth = defaultWidth;
            savedHeight = defaultHeight;
        }

        resolution = new Vector2Int(savedWidth, savedHeight);

        // ค่าที่บันทึกไว้อาจไม่มีอยู่ในรายการแล้ว (ย้ายไปเสียบจออื่น หรือเคยบันทึกไว้ตอนที่ยังไม่ได้กรองสัดส่วน)
        // ดึงมาที่ตัวใกล้เคียงที่สุดในรายการก่อนใช้ ไม่งั้นค่าสัดส่วนเพี้ยนตัวเก่าจะค้างแล้วเต็มจอมีขอบดำเหมือนเดิม
        // ต้องเทียบกับ resolution ตรง ๆ ไม่ใช่ผ่าน ResolutionIndex
        // เพราะถ้าโหลดมาแล้วอยู่โหมดไร้ขอบ ResolutionIndex จะชี้ไปที่ความละเอียดของจอ
        // แล้วบรรทัดล่างจะทับค่าที่ผู้เล่นเลือกไว้ทิ้ง พอสลับกลับไปโหมดอื่นก็ไม่เหลืออะไรให้คืน
        Vector2Int[] list = AvailableResolutions;
        if (list.Length > 0) resolution = list[NearestIndex(resolution)];

        if (PlayerPrefs.HasKey(ModeKey))
        {
            mode = ToDisplayMode(PlayerPrefs.GetInt(ModeKey, (int)DefaultMode));
        }
        else
        {
            // ยังไม่มีคีย์ใหม่ = คนที่เคยเล่นเวอร์ชันก่อนมีโหมดไร้ขอบ (หรือเพิ่งเปิดเกมครั้งแรก)
            // แปลงค่าเก่าที่มีแค่ เต็มจอ/หน้าต่าง มาให้ ไม่งั้นค่าที่เคยตั้งไว้จะหายหมด
            bool wasFullscreen = PlayerPrefs.GetInt(LegacyFullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
            mode = wasFullscreen ? DisplayMode.Fullscreen : DisplayMode.Windowed;
        }

        vSyncEnabled = PlayerPrefs.GetInt(VSyncKey, DefaultVSyncEnabled ? 1 : 0) != 0;
    }

    private static void BuildResolutionList()
    {
        List<Vector2Int> list = new List<Vector2Int>();
        // เขียนชื่อเต็มกันสับสนกับ property ชื่อ Resolution ข้างบน
        UnityEngine.Resolution[] supported = Screen.resolutions;

        for (int i = 0; i < supported.Length; i++)
        {
            Vector2Int size = new Vector2Int(supported[i].width, supported[i].height);
            if (!list.Contains(size)) list.Add(size);
        }

        if (list.Count == 0) list.AddRange(FallbackResolutions);

        // เรียงจากเล็กไปใหญ่ ที่จอกว้างเท่ากันให้เอาตัวเตี้ยกว่าขึ้นก่อน
        list.Sort((a, b) =>
        {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });

        nativeResolution = FindNativeResolution(list);
        availableResolutions = FilterToNativeAspect(list, nativeResolution).ToArray();
    }

    // ตัวที่ใหญ่ที่สุดที่จอรองรับ = ความละเอียดจริงของจอ (ในทางปฏิบัติจอทั่วไปเป็นแบบนี้หมด)
    // วัดด้วยพื้นที่ ไม่ใช่ความกว้าง เพราะบางจอมีโหมดจอกว้างเตี้ย ๆ ที่กว้างกว่าแต่เล็กกว่าปนมา
    private static Vector2Int FindNativeResolution(List<Vector2Int> list)
    {
        Vector2Int best = new Vector2Int(0, 0);
        long bestArea = 0;

        for (int i = 0; i < list.Count; i++)
        {
            long area = (long)list[i].x * list[i].y;
            if (area > bestArea)
            {
                bestArea = area;
                best = list[i];
            }
        }

        // รายการว่างจริง ๆ ค่อยไปถามสภาพจอตอนนี้แทน (ตอนนี้ยังไม่มีใครสั่งเปลี่ยนโหมดจอ ค่ายังเชื่อได้อยู่)
        if (best.x <= 0 || best.y <= 0)
        {
            best = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }

        return best;
    }

    // ตัดตัวเลือกที่สัดส่วนไม่ตรงกับจอออก เช่น 1280x1024 (5:4) หรือ 1024x768 (4:3) บนจอ 16:9
    // เลือกพวกนั้นตอนเต็มจอยังไงก็ได้ขอบดำ ไม่ว่าจะเต็มจอแบบไหน เพราะภาพกับจอสัดส่วนไม่เท่ากันตั้งแต่ต้น
    // ตัดตัวที่ใหญ่กว่าจอออกด้วย จอแสดงไม่ได้อยู่แล้ว
    private static List<Vector2Int> FilterToNativeAspect(List<Vector2Int> list, Vector2Int native)
    {
        if (native.x <= 0 || native.y <= 0) return list;

        float nativeAspect = (float)native.x / native.y;
        List<Vector2Int> filtered = new List<Vector2Int>();

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].x > native.x || list[i].y > native.y) continue;
            if (Mathf.Abs((float)list[i].x / list[i].y - nativeAspect) > AspectTolerance) continue;

            filtered.Add(list[i]);
        }

        // กันเคสจอแปลก ๆ ที่กรองแล้วเหลือตัวเดียว ผู้เล่นจะปรับอะไรไม่ได้เลย เอารายการเดิมคืนไปดีกว่า
        if (filtered.Count < 2) return list;

        return filtered;
    }
}
