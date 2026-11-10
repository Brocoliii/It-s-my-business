using UnityEngine;
using UnityEngine.UI;
using TMPro;

// หน้าตั้งค่าเสียง ลากใส่แผง Settings Panel แล้วผูกสไลเดอร์ตามช่องด้านล่าง
// ค่าที่ผู้เล่นตั้งจะถูกจำไว้ในเครื่อง เปิดเกมใหม่ก็ยังเป็นค่าเดิม
//
// ช่องไหนไม่มีก็ปล่อยว่างได้ ระบบข้ามให้เอง (เช่นมีแค่สไลเดอร์เดียวรวม ๆ ก็ผูกแค่ Master)
public class AudioSettingsUI : MonoBehaviour
{
    [Header("สไลเดอร์ (ค่า 0 - 1)")]
    [Tooltip("เสียงรวมทั้งเกม")]
    public Slider masterSlider;
    [Tooltip("เสียงเพลงพื้นหลัง")]
    public Slider bgmSlider;
    [Tooltip("เสียงประกอบ")]
    public Slider sfxSlider;
    [Tooltip("เสียงวิดีโอและคัตซีน")]
    public Slider cutsceneSlider;

    [Header("ปุ่มปิดเสียง (ไม่มีก็ได้)")]
    public Toggle muteToggle;

    [Header("ข้อความบอกเปอร์เซ็นต์ (ไม่มีก็ได้)")]
    public TextMeshProUGUI masterValueText;
    public TextMeshProUGUI bgmValueText;
    public TextMeshProUGUI sfxValueText;
    public TextMeshProUGUI cutsceneValueText;

    [Header("เสียงตัวอย่าง")]
    [Tooltip("เล่นเสียงตัวอย่างตอนลากสไลเดอร์เสียงประกอบ ผู้เล่นจะได้ยินว่าดังแค่ไหน")]
    public bool previewOnSfxChange = true;
    public string previewSoundId = SoundIds.UiClick;

    // ระหว่างใส่ค่าที่บันทึกไว้กลับเข้าสไลเดอร์ตอนเปิดหน้า สไลเดอร์จะยิง OnValueChanged ออกมาด้วย
    // ต้องกันไว้ ไม่งั้นจะมีเสียงตัวอย่างดังขึ้นมาเองทุกครั้งที่เปิดหน้าตั้งค่า
    private bool isRefreshing = false;

    private void Awake()
    {
        // ผูกที่ Awake ไม่ใช่ Start เพราะแผงตั้งค่ามักถูกเปิด/ปิดตลอด
        // ถ้าผูกใน OnEnable จะซ้อนกันหลายรอบ ถ้าผูกใน Start จะไม่ทำงานเลยตอนแผงเริ่มมาแบบปิดอยู่
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (cutsceneSlider != null) cutsceneSlider.onValueChanged.AddListener(OnCutsceneChanged);
        if (muteToggle != null) muteToggle.onValueChanged.AddListener(OnMuteChanged);
    }

    private void OnEnable()
    {
        RefreshFromSettings();
    }

    private void OnDisable()
    {
        // เขียนค่าลงเครื่องตอนปิดหน้าตั้งค่า จะได้ไม่หายถ้าเกมถูกปิดแบบไม่สวย
        if (SoundManager.Exists) SoundManager.Instance.SaveVolumeSettings();
    }

    // ดึงค่าปัจจุบันจากตัวจัดการเสียงมาใส่สไลเดอร์
    public void RefreshFromSettings()
    {
        SoundManager sound = SoundManager.Instance;
        if (sound == null) return;

        isRefreshing = true;

        if (masterSlider != null) masterSlider.value = sound.MasterVolume;
        if (bgmSlider != null) bgmSlider.value = sound.BgmVolume;
        if (sfxSlider != null) sfxSlider.value = sound.SfxVolume;
        if (cutsceneSlider != null) cutsceneSlider.value = sound.CutsceneVolume;
        if (muteToggle != null) muteToggle.isOn = sound.IsMuted;

        isRefreshing = false;

        UpdateValueTexts();
    }

    private void OnMasterChanged(float value)
    {
        if (isRefreshing) return;

        SoundManager.Instance.SetMasterVolume(value);
        UpdateValueTexts();
    }

    private void OnBgmChanged(float value)
    {
        if (isRefreshing) return;

        SoundManager.Instance.SetBgmVolume(value);
        UpdateValueTexts();
    }

    private void OnSfxChanged(float value)
    {
        if (isRefreshing) return;

        SoundManager.Instance.SetSfxVolume(value);
        UpdateValueTexts();

        // เสียงตัวอย่างมี Min Interval ในคลังกันถี่อยู่แล้ว ลากสไลเดอร์รัว ๆ เลยไม่กลายเป็นเสียงแตก
        if (previewOnSfxChange && !string.IsNullOrEmpty(previewSoundId))
        {
            SoundManager.Instance.PlaySFX(previewSoundId);
        }
    }

    private void OnCutsceneChanged(float value)
    {
        if (isRefreshing) return;

        SoundManager.Instance.SetCutsceneVolume(value);
        UpdateValueTexts();
    }

    private void OnMuteChanged(bool isOn)
    {
        if (isRefreshing) return;

        SoundManager.Instance.SetMuted(isOn);
    }

    private void UpdateValueTexts()
    {
        SoundManager sound = SoundManager.Instance;
        if (sound == null) return;

        if (masterValueText != null) masterValueText.text = ToPercent(sound.MasterVolume);
        if (bgmValueText != null) bgmValueText.text = ToPercent(sound.BgmVolume);
        if (sfxValueText != null) sfxValueText.text = ToPercent(sound.SfxVolume);
        if (cutsceneValueText != null) cutsceneValueText.text = ToPercent(sound.CutsceneVolume);
    }

    private string ToPercent(float value)
    {
        return Mathf.RoundToInt(value * 100f) + "%";
    }
}
