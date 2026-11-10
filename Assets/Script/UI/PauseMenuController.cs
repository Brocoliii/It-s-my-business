using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// แผงหยุดเกม กด ESC ระหว่างเล่นแล้วเกมจะหยุดนิ่ง พร้อมปุ่ม เล่นต่อ / ตั้งค่า / กลับเมนูหลัก
//
// วิธีใช้ (ทำครั้งเดียวต่อฉากด่าน):
//   1. สร้าง GameObject เปล่าในฉาก ตั้งชื่อ "PauseMenu" แล้วลากสคริปต์นี้ใส่
//   2. สร้างแผง Pause Panel ใน Canvas (ให้อยู่ล่างสุดของ Hierarchy จะได้วาดทับ HUD ตัวอื่น)
//      ข้างในมีปุ่ม 3 ปุ่ม: เล่นต่อ / ตั้งค่า / กลับเมนูหลัก
//   3. ลากแผงกับปุ่มทั้งหมดมาใส่ช่องด้านล่าง
//      ไม่ต้องไปผูก On Click ที่ตัวปุ่มเอง ระบบผูกให้ตอนเริ่มฉาก
//   4. แผงตั้งค่า (ตัวเดียวหรือหลายแผงย่อยที่มี AudioSettingsUI / CameraSettingsUI) ไม่มีก็ได้
//      ถ้าใช้หลายแผง ให้ใส่ SettingsPanelCarousel ไว้ที่ตัวแม่ แล้วผูกแผงย่อยกับปุ่มลูกศร
//      หรือใส่ปุ่มเลือกแผงโดยตรงใน panelButtons เพื่อให้หน้า Audio มีปุ่ม Camera/Resolution
//      ปุ่มตั้งค่าจะสลับไปเปิดแผงนั้นให้ แล้วปุ่มย้อนกลับจะพากลับมาที่แผงหยุดเกม
//      สำคัญ: แผงตั้งค่าต้องเป็น "แผงพี่น้อง" ของแผงหยุดเกม ห้ามเอาไปเป็นลูกของแผงหยุดเกม
//      เพราะตอนกดปุ่มตั้งค่า แผงหยุดเกมจะถูกปิด ถ้าแผงตั้งค่าเป็นลูกมันจะถูกปิดตามไปด้วย
//
// แผงเมนูทั้งหมดจะถูกปิดให้เองตอนเริ่มฉาก ไม่ต้องไปปิดค้างไว้ใน Inspector เอง
public class PauseMenuController : MonoBehaviour
{
    // ให้ระบบอื่นเช็คได้ว่าตอนนี้หยุดเกมอยู่ไหม (InputManager กับ CameraController ใช้ตัวนี้)
    // เป็น static เพราะสองตัวนั้นทำงานทุกเฟรม ไม่ควรต้องไปไล่หา component นี้เอง
    public static bool IsPaused { get; private set; }

    [Header("แผงหยุดเกม")]
    [Tooltip("แผงที่จะเด้งขึ้นมาตอนกด ESC ปล่อยว่าง = ไม่มีแผงให้โชว์ (เกมยังหยุดได้ แต่ผู้เล่นจะไม่เห็นปุ่มอะไรเลย)")]
    [SerializeField] private GameObject pausePanel;

    [Header("แอนิเมชันแผงหยุดเกม")]
    [Tooltip("ระยะเวลาเปิด/ปิดแผงหยุดเกม หน่วยเป็นวินาที")]
    [SerializeField] private float pausePanelAnimationDuration = 0.2f;
    [Tooltip("ขนาดเริ่มต้นของแผงตอนเปิด ถ้าเป็น 0 จะเริ่มจากขนาด 92%")]
    [SerializeField] private float pausePanelStartScale = 0.92f;

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

    [Header("หลายแผงตั้งค่า (ทางเลือก)")]
    [Tooltip("ถ้าใช้หลายแผง ให้ลากแผงแม่ที่มี SettingsPanelCarousel ไว้ในช่องนี้\nถ้าเว้นว่าง ระบบจะใช้ settingsPanel เดิม")]
    [SerializeField] private GameObject settingsPanelsRoot;

    [Header("ฉากเมนูหลัก")]
    [Tooltip("ชื่อฉากเมนูหลัก ต้องสะกดตรงกับชื่อฉากจริง และเพิ่มฉากนี้ใน File > Build Settings ด้วย")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("เงื่อนไขการกดหยุด")]
    [Tooltip("ติ๊กถูก = กด ESC หยุดเกมได้เฉพาะตอนกำลังเล่นอยู่ (สถานะ Playing) แต่ยังหยุดได้ตอนเปิดสมุดหรือแผงสรุปผล\nติ๊กออก = หยุดได้ทุกสถานะ ยกเว้นตอนแพ้/ชนะที่ไม่มีแผงสรุปผล")]
    [SerializeField] private bool onlyPauseWhilePlaying = true;
    [Tooltip("ติ๊กถูก = ห้ามกดหยุดเกมระหว่างแอบฟัง (กดค้างอยู่บนตัวคนให้สืบ) ต้องปล่อยเมาส์ก่อนถึงกด ESC ได้\nกันเคสกดหยุดกลางคันเพื่อหนีมินิเกมกดปุ่ม หรือเอาไปคิดคำตอบต่อตอนเกมหยุดนิ่ง")]
    [SerializeField] private bool blockPauseWhileInvestigating = true;

    [Header("เสียงตอนหยุดเกม")]
    [Tooltip("ติ๊กถูก = ปิดเสียงทั้งเกมค้างไว้ตอนหยุด (เพลง เสียงเตาย่าง ฯลฯ เงียบหมด)\nข้อควรรู้: เสียงกดปุ่มในแผงหยุดเกมจะเงียบไปด้วย เพราะมันปิดที่ AudioListener ทีเดียวทั้งเกม")]
    [SerializeField] private bool muteGameAudioWhilePaused = false;

    // จำค่าความเร็วเวลาก่อนหยุดไว้คืนตอนเล่นต่อ ปกติเป็น 1 แต่กันไว้เผื่อมีระบบสโลว์โมชันในอนาคต
    private float timeScaleBeforePause = 1f;

    private InputManager inputManager;
    private CanvasGroup pausePanelCanvasGroup;
    private Coroutine pausePanelAnimation;
    private Coroutine pointerHoverRefresh;
    private Vector3 pausePanelOriginalScale = Vector3.one;

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
        UIButtonSound.BindButton(resumeButton);
        UIButtonSound.BindButton(settingsButton);
        UIButtonSound.BindButton(mainMenuButton);
        UIButtonSound.BindButton(settingsBackButton);

        if (pausePanel != null)
        {
            pausePanelCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
            if (pausePanelCanvasGroup == null) pausePanelCanvasGroup = pausePanel.AddComponent<CanvasGroup>();

            pausePanelOriginalScale = pausePanel.transform.localScale;
            pausePanelCanvasGroup.alpha = 0f;
            pausePanelCanvasGroup.interactable = false;
            pausePanelCanvasGroup.blocksRaycasts = false;
        }

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
        if (IsSettingsVisible())
        {
            ShowPausePanel();
            return;
        }

        ResumeGame();
    }

    // อนุญาตให้หยุดทับสมุดหรือแผงสรุปผลได้ โดยซ่อนแผงเดิมไว้ชั่วคราวเพื่อกันการรับคลิกซ้อนกัน
    private bool CanPause()
    {
        // กำลังแอบฟังอยู่: ห้ามหยุดเกม ไม่งั้นจะกดหยุดหนีมินิเกมกดปุ่มได้
        // (การหยุดเกมสั่ง CancelActiveInteraction ซึ่งตัดการแอบฟังทิ้ง = รอดจากการสืบที่ค้างอยู่ฟรี ๆ)
        if (blockPauseWhileInvestigating && inputManager != null && inputManager.IsInvestigating) return false;

        // เผื่อฉากทดสอบที่ไม่มี GameManager วางไว้ ให้หยุดได้ตามปกติ
        if (GameManager.Instance == null) return true;

        GameManager.GameState state = GameManager.Instance.CurrentState;

        if (state == GameManager.GameState.Win || state == GameManager.GameState.Lose)
        {
            return StageResultPanel.Instance != null && StageResultPanel.Instance.IsShown;
        }

        if (state == GameManager.GameState.CulpritSelection)
        {
            NotebookManager notebook = FindObjectOfType<NotebookManager>(true);
            return notebook != null && notebook.IsOpen;
        }

        return !onlyPauseWhilePlaying || state == GameManager.GameState.Playing;
    }

    public void PauseGame()
    {
        if (IsPaused) return;

        IsPaused = true;

        timeScaleBeforePause = Time.timeScale;
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

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (settingsPanelsRoot != null) settingsPanelsRoot.SetActive(false);
        SetSettingsNavigationVisible(false);
        if (pausePanel != null)
        {
            ClearPausePanelSelection();
            if (pausePanelAnimation != null) StopCoroutine(pausePanelAnimation);
            pausePanelAnimation = StartCoroutine(AnimatePausePanel(false));
        }

        StopPointerHoverRefresh();
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
        GameObject panelToShow = GetSettingsPanel();
        if (panelToShow == null)
        {
            Debug.LogWarning("[PauseMenu] ยังไม่ได้ผูก Settings Panel ไว้ใน Inspector เลยไม่มีแผงตั้งค่าให้เปิด");
            return;
        }

        if (pausePanelAnimation != null) StopCoroutine(pausePanelAnimation);
        pausePanelAnimation = StartCoroutine(SwitchToSettingsPanel(panelToShow));
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
        Time.timeScale = 1f;

        // SoundManager ข้ามฉากไปด้วย (DontDestroyOnLoad) เสียงประกอบที่ค้างอยู่เลยตามไปดังในฉากใหม่ได้
        if (SoundManager.Exists) SoundManager.Instance.StopAllSFX();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowPausePanel()
    {
        ClearPausePanelSelection();

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (settingsPanelsRoot != null) settingsPanelsRoot.SetActive(false);
        SetSettingsNavigationVisible(false);

        if (pausePanel == null) return;

        SoundManager.Instance.PlaySFX(SoundIds.PauseAppear);

        // Keep the underlying modal UI alive and put pause controls above it.
        pausePanel.transform.SetAsLastSibling();

        if (pausePanelAnimation != null) StopCoroutine(pausePanelAnimation);
        StopPointerHoverRefresh();
        pausePanelAnimation = StartCoroutine(AnimatePausePanel(true));
    }


    private void HideAllPanels()
    {
        if (pausePanelAnimation != null) StopCoroutine(pausePanelAnimation);
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            pausePanelCanvasGroup.alpha = 0f;
            pausePanelCanvasGroup.interactable = false;
            pausePanelCanvasGroup.blocksRaycasts = false;
            pausePanel.transform.localScale = pausePanelOriginalScale;
        }
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (settingsPanelsRoot != null) settingsPanelsRoot.SetActive(false);
    }

    private IEnumerator SwitchToSettingsPanel(GameObject panelToShow)
    {
        yield return AnimatePausePanel(false);

        panelToShow.SetActive(true);
        SetSettingsNavigationVisible(true);
        pausePanelAnimation = null;
    }

    private IEnumerator AnimatePausePanel(bool show)
    {
        if (pausePanel == null || pausePanelCanvasGroup == null) yield break;

        if (show)
        {
            pausePanel.SetActive(true);
            pausePanelCanvasGroup.interactable = false;
            pausePanelCanvasGroup.blocksRaycasts = false;
        }

        float duration = Mathf.Max(0f, pausePanelAnimationDuration);
        float elapsed = 0f;
        float startAlpha = show ? 0f : pausePanelCanvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        Vector3 startScale = show
            ? pausePanelOriginalScale * Mathf.Clamp(pausePanelStartScale, 0.01f, 1f)
            : pausePanel.transform.localScale;
        Vector3 endScale = show ? pausePanelOriginalScale : pausePanelOriginalScale * Mathf.Clamp(pausePanelStartScale, 0.01f, 1f);

        pausePanelCanvasGroup.alpha = startAlpha;
        pausePanel.transform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            pausePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);
            pausePanel.transform.localScale = Vector3.Lerp(startScale, endScale, easedProgress);
            yield return null;
        }

        pausePanelCanvasGroup.alpha = endAlpha;
        pausePanel.transform.localScale = endScale;
        pausePanelCanvasGroup.interactable = show;
        pausePanelCanvasGroup.blocksRaycasts = show;

        if (!show)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            pointerHoverRefresh = StartCoroutine(RefreshPointerHover());
        }

        pausePanelAnimation = null;
    }

    private IEnumerator RefreshPointerHover()
    {
        yield return null;

        if (EventSystem.current == null || Mouse.current == null || pausePanel == null || !pausePanel.activeInHierarchy)
        {
            pointerHoverRefresh = null;
            yield break;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject target = raycastResults[i].gameObject;
            if (target != null && target.transform.IsChildOf(pausePanel.transform))
            {
                Button button = target.GetComponentInParent<Button>();
                if (button != null && button.IsInteractable())
                {
                    pointerData.pointerEnter = button.gameObject;
                    ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerExitHandler);
                    ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerEnterHandler);
                }

                break;
            }
        }

        pointerHoverRefresh = null;
    }

    private void ClearPausePanelSelection()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        Button[] buttons = pausePanel != null ? pausePanel.GetComponentsInChildren<Button>(true) : new Button[0];
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                ExecuteEvents.Execute(buttons[i].gameObject, pointerData, ExecuteEvents.pointerExitHandler);
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void StopPointerHoverRefresh()
    {
        if (pointerHoverRefresh != null)
        {
            StopCoroutine(pointerHoverRefresh);
            pointerHoverRefresh = null;
        }
    }

    private GameObject GetSettingsPanel()
    {
        return settingsPanelsRoot != null ? settingsPanelsRoot : settingsPanel;
    }

    private bool IsSettingsVisible()
    {
        GameObject panel = GetSettingsPanel();
        return panel != null && panel.activeSelf;
    }

    private void SetSettingsNavigationVisible(bool isVisible)
    {
        GameObject panel = GetSettingsPanel();
        if (panel == null) return;

        SettingsPanelCarousel carousel = panel.GetComponent<SettingsPanelCarousel>();
        if (carousel != null) carousel.SetNavigationVisible(isVisible);
    }

    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(OnClickResume);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnClickSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnClickMainMenu);
        if (settingsBackButton != null) settingsBackButton.onClick.RemoveListener(OnClickSettingsBack);
        StopPointerHoverRefresh();

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
