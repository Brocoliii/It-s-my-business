using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// แผงหยุดเกม กด ESC ระหว่างเล่นแล้วเกมจะหยุดนิ่ง พร้อมปุ่ม เล่นต่อ / ตั้งค่า / กลับเมนูหลัก
//
// วิธีใช้ (ทำครั้งเดียวต่อฉากด่าน):
//   1. สร้าง GameObject เปล่าในฉาก ตั้งชื่อ "PauseMenu" แล้วลากสคริปต์นี้ใส่
//   2. สร้างแผง Pause Panel ใน Canvas (ให้อยู่ล่างสุดของ Hierarchy จะได้วาดทับ HUD ตัวอื่น)
//      ข้างในมีปุ่ม 3 ปุ่ม: เล่นต่อ / ตั้งค่า / กลับเมนูหลัก
//   3. ลากแผงกับปุ่มทั้งหมดมาใส่ช่องด้านล่าง
//      ไม่ต้องไปผูก On Click ที่ตัวปุ่มเอง ระบบผูกให้ตอนเริ่มฉาก
//   4. แผงตั้งค่า (ตัวเดียวกับที่มี AudioSettingsUI / CameraSettingsUI) ไม่มีก็ได้
//      ถ้าใส่ไว้ ปุ่มตั้งค่าจะสลับไปเปิดแผงนั้นให้ แล้วปุ่มย้อนกลับจะพากลับมาที่แผงหยุดเกม
//      สำคัญ: แผงตั้งค่าต้องเป็น "แผงพี่น้อง" ของแผงหยุดเกม ห้ามเอาไปเป็นลูกของแผงหยุดเกม
//      เพราะตอนกดปุ่มตั้งค่า แผงหยุดเกมจะถูกปิด ถ้าแผงตั้งค่าเป็นลูกมันจะถูกปิดตามไปด้วย
//
// แผงทั้งสองจะถูกปิดให้เองตอนเริ่มฉาก ไม่ต้องไปปิดค้างไว้ใน Inspector เอง
public class PauseMenuController : MonoBehaviour
{
    // ให้ระบบอื่นเช็คได้ว่าตอนนี้หยุดเกมอยู่ไหม (InputManager กับ CameraController ใช้ตัวนี้)
    // เป็น static เพราะสองตัวนั้นทำงานทุกเฟรม ไม่ควรต้องไปไล่หา component นี้เอง
    public static bool IsPaused { get; private set; }

    [Header("แผงหยุดเกม")]
    [Tooltip("แผงที่จะเด้งขึ้นมาตอนกด ESC ปล่อยว่าง = ไม่มีแผงให้โชว์ (เกมยังหยุดได้ แต่ผู้เล่นจะไม่เห็นปุ่มอะไรเลย)")]
    [SerializeField] private GameObject pausePanel;

    [Header("แผงตั้งค่า (ไม่มีก็ได้)")]
    [Tooltip("แผงตั้งค่าตัวเดียวกับที่มี AudioSettingsUI / CameraSettingsUI อยู่\nปล่อยว่าง = ปุ่มตั้งค่าจะไม่ทำอะไร")]
    [SerializeField] private GameObject settingsPanel;

    [Header("ปุ่มในแผงหยุดเกม")]
    [Tooltip("ปุ่ม \"เล่นต่อ\"")]
    [SerializeField] private Button resumeButton;
    [Tooltip("ปุ่ม \"ตั้งค่า\"")]
    [SerializeField] private Button settingsButton;
    [Tooltip("ปุ่ม \"กลับเมนูหลัก\"")]
    [SerializeField] private Button mainMenuButton;

    [Header("ปุ่มย้อนกลับในแผงตั้งค่า")]
    [Tooltip("ปุ่ม \"ย้อนกลับ\" ที่อยู่ในแผง Settings ลากมาใส่ช่องนี้ช่องเดียว\nกดแล้วจะกลับมาที่แผงหยุดเกม ไม่ใช่เล่นต่อทันที")]
    [SerializeField] private Button settingsBackButton;

    [Header("ฉากเมนูหลัก")]
    [Tooltip("ชื่อฉากเมนูหลัก ต้องสะกดตรงกับชื่อฉากจริง และเพิ่มฉากนี้ใน File > Build Settings ด้วย")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("เงื่อนไขการกดหยุด")]
    [Tooltip("ติ๊กถูก = กด ESC หยุดเกมได้เฉพาะตอนกำลังเล่นอยู่ (สถานะ Playing)\nติ๊กออก = หยุดได้ทุกสถานะ ยกเว้นตอนแพ้/ชนะ ซึ่งกันไว้เสมอ")]
    [SerializeField] private bool onlyPauseWhilePlaying = true;
    [Tooltip("ติ๊กถูก = ห้ามกดหยุดเกมระหว่างแอบฟัง (กดค้างอยู่บนตัวคนให้สืบ) ต้องปล่อยเมาส์ก่อนถึงกด ESC ได้\nกันเคสกดหยุดกลางคันเพื่อหนีมินิเกมกดปุ่ม หรือเอาไปคิดคำตอบต่อตอนเกมหยุดนิ่ง")]
    [SerializeField] private bool blockPauseWhileInvestigating = true;

    [Header("เสียงตอนหยุดเกม")]
    [Tooltip("ติ๊กถูก = ปิดเสียงทั้งเกมค้างไว้ตอนหยุด (เพลง เสียงเตาย่าง ฯลฯ เงียบหมด)\nข้อควรรู้: เสียงกดปุ่มในแผงหยุดเกมจะเงียบไปด้วย เพราะมันปิดที่ AudioListener ทีเดียวทั้งเกม")]
    [SerializeField] private bool muteGameAudioWhilePaused = false;

    // จำค่าความเร็วเวลาก่อนหยุดไว้คืนตอนเล่นต่อ ปกติเป็น 1 แต่กันไว้เผื่อมีระบบสโลว์โมชันในอนาคต
    private float timeScaleBeforePause = 1f;

    private InputManager inputManager;

    private void Awake()
    {
        // static ตัวนี้ข้ามฉากไปด้วย ถ้าออกจากฉากตอนหยุดค้างไว้แล้วไม่ล้าง
        // ฉากใหม่จะเปิดมาแล้วโดนมองว่ายังหยุดอยู่ จนกดอะไรไม่ได้ทั้งฉาก
        IsPaused = false;

        // ผูกที่ Awake เพราะแผงหยุดเกมเริ่มมาแบบปิดอยู่ ปุ่มข้างในเลยยังไม่ทำงานถ้าไปรอผูกทีหลัง
        // ผูกจากโค้ดแทนการผูกใน Inspector จะได้ไม่มีเคสลืมผูกแล้วปุ่มกดไม่ติด
        if (resumeButton != null) resumeButton.onClick.AddListener(OnClickResume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnClickSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnClickMainMenu);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(OnClickSettingsBack);

        HideAllPanels();
    }

    private void Start()
    {
        inputManager = FindObjectOfType<InputManager>();
    }

    private void Update()
    {
        // Update ยังเดินอยู่ตอน timeScale = 0 เลยกด ESC ออกจากหน้าหยุดเกมได้ตามปกติ
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (!IsPaused)
        {
            if (CanPause()) PauseGame();
            return;
        }

        // อยู่ในหน้าตั้งค่า: ESC = ถอยกลับมาหน้าหยุดเกมก่อน ไม่ใช่กระโดดกลับไปเล่นต่อเลย
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            ShowPausePanel();
            return;
        }

        ResumeGame();
    }

    // แพ้/ชนะไปแล้วห้ามหยุดเกม ไม่งั้นแผงหยุดเกมจะเด้งมาทับหน้าจอแพ้ แล้วกดเล่นต่อได้ทั้งที่จบไปแล้ว
    private bool CanPause()
    {
        // กำลังแอบฟังอยู่: ห้ามหยุดเกม ไม่งั้นจะกดหยุดหนีมินิเกมกดปุ่มได้
        // (การหยุดเกมสั่ง CancelActiveInteraction ซึ่งตัดการแอบฟังทิ้ง = รอดจากการสืบที่ค้างอยู่ฟรี ๆ)
        if (blockPauseWhileInvestigating && inputManager != null && inputManager.IsInvestigating) return false;

        // เผื่อฉากทดสอบที่ไม่มี GameManager วางไว้ ให้หยุดได้ตามปกติ
        if (GameManager.Instance == null) return true;

        GameManager.GameState state = GameManager.Instance.CurrentState;

        if (state == GameManager.GameState.Win || state == GameManager.GameState.Lose) return false;

        return !onlyPauseWhilePlaying || state == GameManager.GameState.Playing;
    }

    public void PauseGame()
    {
        if (IsPaused) return;

        IsPaused = true;

        timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        // ปล่อยของที่ค้างอยู่ในมือก่อนหยุด ไม่งั้นถ้าผู้เล่นปล่อยเมาส์ตอนกดปุ่มในแผงหยุดเกม
        // จังหวะ "ปล่อยคลิก" จะหายไปทั้งจังหวะ แล้วอาหารจะค้างลอยกลางอากาศหลังเล่นต่อ
        if (inputManager != null) inputManager.CancelActiveInteraction();

        if (muteGameAudioWhilePaused && SoundManager.Exists) SoundManager.Instance.SetPaused(true);

        ShowPausePanel();
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        IsPaused = false;

        Time.timeScale = timeScaleBeforePause;

        if (muteGameAudioWhilePaused && SoundManager.Exists) SoundManager.Instance.SetPaused(false);

        HideAllPanels();
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else if (CanPause()) PauseGame();
    }

    public void OnClickResume()
    {
        ResumeGame();
    }

    public void OnClickSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("[PauseMenu] ยังไม่ได้ผูก Settings Panel ไว้ใน Inspector เลยไม่มีแผงตั้งค่าให้เปิด");
            return;
        }

        if (pausePanel != null) pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // กลับจากแผงตั้งค่ามาที่แผงหยุดเกม (เกมยังหยุดอยู่)
    public void OnClickSettingsBack()
    {
        ShowPausePanel();
    }

    public void OnClickMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("[PauseMenu] ยังไม่ได้ตั้ง Main Menu Scene Name ไว้ใน Inspector เลยไม่รู้ว่าต้องกลับไปฉากไหน");
            return;
        }

        // เช็คตั้งแต่ตอนนี้เลย จะได้ไม่คืน timeScale ทิ้งไว้แล้วไปพังตอน LoadScene
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError($"[PauseMenu] โหลดฉาก '{mainMenuSceneName}' ไม่ได้! เช็คชื่อฉากให้ตรง และเพิ่มฉากนี้ใน File > Build Settings ด้วย");
            return;
        }

        // คืนเวลา/เสียงให้เรียบร้อยก่อนออกจากฉาก ไม่งั้นฉากเมนูจะเปิดมาแล้วค้างนิ่งและเงียบสนิท
        ResumeGame();

        // SoundManager ข้ามฉากไปด้วย (DontDestroyOnLoad) เสียงประกอบที่ค้างอยู่เลยตามไปดังในฉากใหม่ได้
        if (SoundManager.Exists) SoundManager.Instance.StopAllSFX();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowPausePanel()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void HideAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(OnClickResume);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnClickSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnClickMainMenu);
        if (settingsBackButton != null) settingsBackButton.onClick.RemoveListener(OnClickSettingsBack);

        // กันเคสฉากถูกเปลี่ยนทิ้งตอนยังหยุดค้างอยู่ (เช่นชนะด่านระหว่างเปิดแผงหยุดเกม)
        // ไม่คืนค่าตรงนี้ ฉากใหม่จะเปิดมาแล้วค้างนิ่งทั้งฉาก
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = timeScaleBeforePause;
            if (muteGameAudioWhilePaused && SoundManager.Exists) SoundManager.Instance.SetPaused(false);
        }
    }
}
