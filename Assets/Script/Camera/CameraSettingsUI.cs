using UnityEngine;
using UnityEngine.UI;
using TMPro;

// หน้าตั้งค่ากล้อง ลากสคริปต์นี้ใส่แผง Settings Panel ตัวเดียวกับ AudioSettingsUI ได้เลย
// แล้วผูกสไลเดอร์เข้าช่อง Pan Speed Slider และผูก Toggle เข้าช่องความหนืด/แรงไถล
//
// ไม่ต้องตั้ง Min/Max ของสไลเดอร์เอง สคริปต์ตั้งให้ตอนเริ่มฉาก
// ช่องไหนไม่มีก็ปล่อยว่างได้ ระบบข้ามให้เอง
// ค่าที่ผู้เล่นตั้งจะถูกจำไว้ในเครื่อง เปิดเกมใหม่ก็ยังเป็นค่าเดิม
public class CameraSettingsUI : MonoBehaviour
{
    [Header("สไลเดอร์ความเร็วแพนกล้อง")]
    [Tooltip("ความเร็วตอนกดคลิกขวาลากจอ และตอนดันขอบจอ")]
    public Slider panSpeedSlider;

    [Header("สวิตช์ความรู้สึกของกล้อง (ไม่มีก็ได้)")]
    [Tooltip("เปิด = กล้องค่อย ๆ ไล่ตามเมาส์แบบนุ่ม ๆ, ปิด = กล้องติดมือทันทีไม่มีหน่วง")]
    public Toggle panSmoothToggle;
    [Tooltip("เปิด = ปล่อยเมาส์แล้วกล้องไถลต่อ, ปิด = ปล่อยแล้วหยุดตรงนั้นเลย")]
    public Toggle panInertiaToggle;

    [Header("ข้อความบอกเปอร์เซ็นต์ (ไม่มีก็ได้)")]
    public TextMeshProUGUI panSpeedValueText;

    [Header("ปุ่มคืนค่าเริ่มต้น (ไม่มีก็ได้)")]
    public Button resetButton;

    // ตอนเปิดหน้ามาแล้วใส่ค่าที่บันทึกไว้กลับเข้าสไลเดอร์ สไลเดอร์จะยิง OnValueChanged ออกมาด้วย
    // ต้องกันไว้ ไม่งั้นค่าที่โหลดมาจะถูกเขียนทับด้วยตัวมันเองไปเรื่อย ๆ
    private bool isRefreshing = false;

    private void Awake()
    {
        // ผูกที่ Awake ไม่ใช่ Start เพราะแผงตั้งค่ามักเริ่มมาแบบปิดอยู่ (Start จะไม่ทำงานเลย)
        if (panSpeedSlider != null)
        {
            // ตั้งช่วงค่าจากโค้ด กันเคสลืมตั้งใน Inspector แล้วลากได้แค่ 0 - 1 จนกล้องหยุดนิ่ง
            panSpeedSlider.wholeNumbers = false;
            panSpeedSlider.minValue = CameraSettings.MinPanSpeedScale;
            panSpeedSlider.maxValue = CameraSettings.MaxPanSpeedScale;
            panSpeedSlider.onValueChanged.AddListener(OnPanSpeedChanged);
        }

        if (panSmoothToggle != null) panSmoothToggle.onValueChanged.AddListener(OnPanSmoothChanged);
        if (panInertiaToggle != null) panInertiaToggle.onValueChanged.AddListener(OnPanInertiaChanged);

        if (resetButton != null) resetButton.onClick.AddListener(OnClickReset);
    }

    private void OnEnable()
    {
        RefreshFromSettings();
    }

    private void OnDisable()
    {
        // เขียนค่าลงเครื่องตอนปิดหน้าตั้งค่า จะได้ไม่หายถ้าเกมถูกปิดแบบไม่สวย
        CameraSettings.Save();
    }

    // ดึงค่าปัจจุบันมาใส่สไลเดอร์
    public void RefreshFromSettings()
    {
        isRefreshing = true;

        if (panSpeedSlider != null) panSpeedSlider.value = CameraSettings.PanSpeedScale;
        if (panSmoothToggle != null) panSmoothToggle.isOn = CameraSettings.PanSmoothEnabled;
        if (panInertiaToggle != null) panInertiaToggle.isOn = CameraSettings.PanInertiaEnabled;

        isRefreshing = false;

        UpdateValueText();
    }

    private void OnPanSpeedChanged(float value)
    {
        if (isRefreshing) return;

        CameraSettings.SetPanSpeedScale(value);
        UpdateValueText();
    }

    private void OnPanSmoothChanged(bool isOn)
    {
        if (isRefreshing) return;

        CameraSettings.SetPanSmoothEnabled(isOn);
    }

    private void OnPanInertiaChanged(bool isOn)
    {
        if (isRefreshing) return;

        CameraSettings.SetPanInertiaEnabled(isOn);
    }

    private void OnClickReset()
    {
        CameraSettings.ResetToDefault();
        RefreshFromSettings();
    }

    private void UpdateValueText()
    {
        if (panSpeedValueText == null) return;

        panSpeedValueText.text = Mathf.RoundToInt(CameraSettings.PanSpeedScale * 100f) + "%";
    }

    private void OnDestroy()
    {
        if (panSpeedSlider != null) panSpeedSlider.onValueChanged.RemoveListener(OnPanSpeedChanged);
        if (panSmoothToggle != null) panSmoothToggle.onValueChanged.RemoveListener(OnPanSmoothChanged);
        if (panInertiaToggle != null) panInertiaToggle.onValueChanged.RemoveListener(OnPanInertiaChanged);
        if (resetButton != null) resetButton.onClick.RemoveListener(OnClickReset);
    }
}
