using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// หน้าตั้งค่าภาพ ลากสคริปต์นี้ใส่แผง Settings Panel ตัวเดียวกับ AudioSettingsUI / CameraSettingsUI ได้เลย
// แล้วผูก Dropdown เข้าช่อง Resolution Dropdown กับ Display Mode Dropdown และผูก Toggle เข้าช่อง VSync
//
// ไม่ต้องพิมพ์รายการความละเอียดเองใน Inspector สคริปต์ใส่ให้ตามจอของเครื่องที่เล่นอยู่
// รายการใน Display Mode Dropdown ก็ใส่ให้เหมือนกัน (Fullscreen / Windowed)
// ช่องไหนไม่มีก็ปล่อยว่างได้ ระบบข้ามให้เอง
// ค่าที่ผู้เล่นตั้งจะถูกจำไว้ในเครื่อง เปิดเกมใหม่ก็ยังเป็นค่าเดิม
//
// *** ตอนกดเล่นใน Editor การเปลี่ยนความละเอียด/โหมดจอจะไม่เห็นผล *** (Unity คุมหน้าต่าง Game view เอง)
// ต้อง Build ออกมาเป็นเกมจริงถึงจะเห็น ส่วน VSync เห็นผลได้ตั้งแต่ใน Editor
public class DisplaySettingsUI : MonoBehaviour
{
    [Header("ความละเอียดจอ")]
    [Tooltip("รายการตัวเลือกใส่ให้อัตโนมัติตามจอของเครื่อง ไม่ต้องพิมพ์เอง")]
    public TMP_Dropdown resolutionDropdown;

    [Header("โหมดการแสดงผล")]
    [Tooltip("3 ตัวเลือก: เต็มจอ / เต็มจอไร้ขอบ / หน้าต่าง ใส่ให้อัตโนมัติเหมือนกัน")]
    public TMP_Dropdown displayModeDropdown;
    [Tooltip("ข้อความในตัวเลือกโหมดจอ แก้เป็นภาษาไทยได้ตามใจ")]
    public string fullscreenLabel = "Fullscreen";
    [Tooltip("โหมดไร้ขอบใช้ความละเอียดเท่าจอเสมอ ตัวเลือกความละเอียดจะถูกปิดตอนเลือกโหมดนี้")]
    public string borderlessLabel = "Borderless";
    public string windowedLabel = "Windowed";

    [Header("VSync")]
    [Tooltip("เปิด = ล็อกเฟรมตามจอ ภาพไม่ฉีก, ปิด = ปล่อยเฟรมวิ่งเต็มที่ ตอบสนองไวขึ้นแต่ภาพอาจฉีก")]
    public Toggle vsyncToggle;

    [Header("ปุ่มคืนค่าเริ่มต้น (ไม่มีก็ได้)")]
    public Button resetButton;

    [Header("ตัวนับเฟรม (ไม่มีก็ได้)")]
    [Tooltip("ใส่ไว้เช็คว่า VSync ทำงานจริงไหม เปิด VSync แล้วเฟรมควรนิ่งที่ค่า refresh rate ของจอ (60/144/...)\nต้องดูในเกมที่ Build แล้วเท่านั้น ใน Editor ตัวเลขไม่ตรงความจริง")]
    public TextMeshProUGUI fpsText;
    [Tooltip("เว้นกี่วินาทีถึงอัปเดตตัวเลขที ตั้งสั้นไปตัวเลขจะกระพริบจนอ่านไม่ทัน")]
    public float fpsUpdateInterval = 0.25f;

    private float fpsElapsed = 0f;
    private int fpsFrameCount = 0;

    // ลำดับตัวเลือกใน Dropdown ต้องตรงกับลำดับที่ BuildDisplayModeOptions ใส่เข้าไป
    // และตรงกับตัวเลขใน DisplaySettings.DisplayMode ด้วย
    // (ไม่มีของเต็มจอเพราะมันเป็นตัว default อยู่แล้วตอนแปลงกลับใน OnDisplayModeChanged)
    private const int BorderlessOptionIndex = (int)DisplaySettings.DisplayMode.Borderless;
    private const int WindowedOptionIndex = (int)DisplaySettings.DisplayMode.Windowed;

    // ตอนเปิดหน้ามาแล้วใส่ค่าที่บันทึกไว้กลับเข้า Dropdown ตัว Dropdown จะยิง OnValueChanged ออกมาด้วย
    // ต้องกันไว้ ไม่งั้นแค่เปิดหน้าตั้งค่าเฉย ๆ จอก็จะถูกสั่งเปลี่ยนความละเอียดใหม่ทุกครั้ง
    private bool isRefreshing = false;

    private void Awake()
    {
        // ผูกที่ Awake ไม่ใช่ Start เพราะแผงตั้งค่ามักเริ่มมาแบบปิดอยู่ (Start จะไม่ทำงานเลย)
        BuildResolutionOptions();
        BuildDisplayModeOptions();

        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (displayModeDropdown != null) displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
        if (vsyncToggle != null) vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        if (resetButton != null) resetButton.onClick.AddListener(OnClickReset);
    }

    private void OnEnable()
    {
        RefreshFromSettings();
    }

    private void OnDisable()
    {
        // เขียนค่าลงเครื่องตอนปิดหน้าตั้งค่า จะได้ไม่หายถ้าเกมถูกปิดแบบไม่สวย
        DisplaySettings.Save();
    }

    // นับเฟรมเอง ไม่ใช้ 1/deltaTime ตรง ๆ เพราะตัวเลขจะเต้นจนอ่านไม่ออก
    // ใช้ unscaledDeltaTime เพราะแผงตั้งค่ามักเปิดจากเมนูหยุดเกมที่ timeScale = 0 อยู่
    private void Update()
    {
        if (fpsText == null) return;

        fpsElapsed += Time.unscaledDeltaTime;
        fpsFrameCount++;

        // เช็ค fpsElapsed ด้วย กันเคสตั้ง Interval เป็น 0 แล้วหารด้วยศูนย์
        if (fpsElapsed < fpsUpdateInterval || fpsElapsed <= 0f) return;

        int fps = Mathf.RoundToInt(fpsFrameCount / fpsElapsed);

        // โชว์ refresh rate ของจอไว้ข้าง ๆ ด้วย ไม่งั้นดูตัวเลข FPS เฉย ๆ จะแยกไม่ออกว่า
        // "144 FPS" คือ VSync ล็อกอยู่ที่จอ 144Hz หรือคือ VSync ไม่ทำงานเลยกันแน่
        string vsyncState;
        if (QualitySettings.vSyncCount > 0) vsyncState = "ON";
        else if (DisplaySettings.IsUsingFrameCapFallback) vsyncState = "ON (ล็อกเฟรมแทน)";
        else vsyncState = "OFF";

        string line = fps + " FPS | จอ " + DisplaySettings.CurrentRefreshRate + "Hz | VSync " + vsyncState;

        if (Application.isEditor) line += " (ใน Editor ไม่มีผล ต้อง Build)";

        fpsText.text = line;

        fpsElapsed = 0f;
        fpsFrameCount = 0;
    }

    // ดึงค่าปัจจุบันมาใส่ Dropdown กับ Toggle
    public void RefreshFromSettings()
    {
        isRefreshing = true;

        if (resolutionDropdown != null)
        {
            resolutionDropdown.value = DisplaySettings.ResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        if (displayModeDropdown != null)
        {
            displayModeDropdown.value = (int)DisplaySettings.Mode;
            displayModeDropdown.RefreshShownValue();
        }

        if (vsyncToggle != null) vsyncToggle.isOn = DisplaySettings.VSyncEnabled;

        // ต้องอยู่หลังตั้งค่า Dropdown ทั้งสองตัว เพราะตัวนี้อ่านโหมดปัจจุบันมาตัดสิน
        RefreshResolutionAvailability();

        isRefreshing = false;
    }

    // โหมดไร้ขอบใช้ความละเอียดเท่าจอเสมอ เลือกอย่างอื่นไม่ได้ เลยปิดตัวเลือกไว้ให้เห็นชัด ๆ
    // ปล่อยให้กดได้แต่กดแล้วไม่มีอะไรเกิดขึ้น ผู้เล่นจะนึกว่าเกมพัง
    private void RefreshResolutionAvailability()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.interactable = DisplaySettings.CanChooseResolution;
    }

    // ใส่รายการความละเอียดตามจอของเครื่องที่เล่นอยู่ เช่น "1920 x 1080"
    private void BuildResolutionOptions()
    {
        if (resolutionDropdown == null) return;

        Vector2Int[] list = DisplaySettings.AvailableResolutions;

        List<string> labels = new List<string>();
        for (int i = 0; i < list.Length; i++)
        {
            labels.Add(list[i].x + " x " + list[i].y);
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
    }

    private void BuildDisplayModeOptions()
    {
        if (displayModeDropdown == null) return;

        // ลำดับต้องตรงกับ DisplaySettings.DisplayMode (เต็มจอ = 0, ไร้ขอบ = 1, หน้าต่าง = 2)
        // เพราะ OnDisplayModeChanged เอาเลข index ของ Dropdown ไปแปลงเป็นโหมดตรง ๆ
        List<string> labels = new List<string> { fullscreenLabel, borderlessLabel, windowedLabel };

        displayModeDropdown.ClearOptions();
        displayModeDropdown.AddOptions(labels);
    }

    private void OnResolutionChanged(int index)
    {
        if (isRefreshing) return;

        DisplaySettings.SetResolutionIndex(index);
    }

    private void OnDisplayModeChanged(int index)
    {
        if (isRefreshing) return;

        DisplaySettings.DisplayMode selected;
        if (index == BorderlessOptionIndex) selected = DisplaySettings.DisplayMode.Borderless;
        else if (index == WindowedOptionIndex) selected = DisplaySettings.DisplayMode.Windowed;
        // เหลือแค่ตัวเต็มจอ (และเผื่อ index หลุดช่วง ให้ตกมาที่โหมดปลอดภัยที่สุด)
        else selected = DisplaySettings.DisplayMode.Fullscreen;

        DisplaySettings.SetMode(selected);

        // เข้า/ออกโหมดไร้ขอบแล้วตัวเลือกความละเอียดต้องเปลี่ยนสถานะตาม
        RefreshResolutionAvailability();
    }

    private void OnVSyncChanged(bool isOn)
    {
        if (isRefreshing) return;

        DisplaySettings.SetVSyncEnabled(isOn);
    }

    private void OnClickReset()
    {
        DisplaySettings.ResetToDefault();
        RefreshFromSettings();
    }

    private void OnDestroy()
    {
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (displayModeDropdown != null) displayModeDropdown.onValueChanged.RemoveListener(OnDisplayModeChanged);
        if (vsyncToggle != null) vsyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
        if (resetButton != null) resetButton.onClick.RemoveListener(OnClickReset);
    }
}
