using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private enum StartTransitionType
    {
        BlackFade,
        BlackCircle
    }

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;

    [SerializeField] private GameObject settingsPanel;

    [Header("Credits")]
    [Tooltip("แผงเครดิตที่จะแสดงเมื่อกดปุ่ม Credit")]
    [SerializeField] private GameObject creditsPanel;
    [Tooltip("ตัวควบคุมหนังสือเครดิต ถ้าเว้นว่างระบบจะค้นหาจาก creditsPanel")]
    [SerializeField] private CreditsBookUI creditsBook;
    [Tooltip("วัตถุที่ต้องการให้มีแอนิเมชันเปิด/ปิด เช่น หนังสือเครดิต สามารถใส่ได้หลายวัตถุ")]
    [SerializeField] private Transform[] creditsAnimationTargets;
    [Tooltip("ปุ่ม Credit บนหน้าเมนูหลัก")]
    [SerializeField] private Button creditsButton;
    [Tooltip("ปุ่มย้อนกลับที่อยู่ในแผงเครดิต")]
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private float creditsAnimationDuration = 0.2f;
    [SerializeField] private AnimationCurve creditsAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("หลายแผงตั้งค่า (ทางเลือก)")]
    [Tooltip("ถ้าใช้หลายแผง ให้ลากแผงแม่ที่มี SettingsPanelCarousel ไว้ในช่องนี้\nตั้งค่า panelButtons ใน SettingsPanelCarousel ได้ถ้าต้องการปุ่มไปแต่ละแผง\nถ้าเว้นว่าง ระบบจะใช้ settingsPanel เดิม")]
    [SerializeField] private GameObject settingsPanelsRoot;

    [Header("Start scenes")]
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [SerializeField] private string level1SceneName = "lv1";

    [Header("Start choice popup")]
    [Tooltip("Popup shown after pressing Play. Add two buttons and assign them below.")]
    [SerializeField] private GameObject startChoicePanel;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button level1Button;
    [SerializeField] private Button closeStartChoiceButton;
    [SerializeField] private float startChoiceAnimationDuration = 0.2f;
    [SerializeField] private AnimationCurve startChoiceAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Logo intro (optional)")]
    [SerializeField] private LogoIntroVideo logoIntroVideo;

    [Header("เฟดดำตอนกดเริ่มเกม")]
    [Tooltip("ผูก ScreenFader ในฉากเองก็ได้ ถ้าเว้นว่างไว้ระบบจะหา/สร้างให้เองตอนเริ่มฉาก")]
    [SerializeField] private ScreenFader screenFader;

    [Header("วงกลมดำตอนกดเริ่มเกม")]
    [Tooltip("วงกลมดำจะขยายจากกึ่งกลางจอก่อนโหลดฉากเกม ถ้าเว้นว่างไว้ระบบจะสร้างให้เอง")]
    [SerializeField] private BlackCircleTransition blackCircleTransition;

    [Header("รูปแบบการเปลี่ยนฉากตอนกดเริ่ม")]
    [SerializeField] private StartTransitionType startTransition = StartTransitionType.BlackCircle;

    [Header("ปุ่มย้อนกลับในแผงตั้งค่า")]
    [Tooltip("ปุ่ม \"ย้อนกลับ\" ที่อยู่ในแผง Settings ลากมาใส่ช่องนี้ช่องเดียว\nไม่ต้องไปผูก On Click ที่ตัวปุ่มเอง ระบบผูกให้ตอนเริ่มฉาก")]
    [SerializeField] private Button settingsBackButton;

    [Header("ปุ่มโหลดเกม")]
    [Tooltip("ปุ่ม \"โหลดเกม\" ในเมนูหลัก ลากมาใส่ช่องนี้ช่องเดียว\nไม่ต้องไปผูก On Click ที่ตัวปุ่มเอง ระบบผูกให้ตอนเริ่มฉาก\nแผงเลือกช่องเซฟถูกสร้างให้เองตอนกด ไม่ต้องเตรียมอะไรในฉาก")]
    [SerializeField] private Button loadButton;
    [Tooltip("ติ๊กถูก = ปิดปุ่มโหลดไว้ตอนที่ยังไม่มีเซฟสักช่อง ผู้เล่นจะได้ไม่กดแล้วเจอแผงเปล่า")]
    [SerializeField] private bool disableLoadButtonWhenEmpty = true;

    // กันกดปุ่มเริ่มซ้ำระหว่างที่ม่านกำลังเฟดดำอยู่
    private bool isStarting;
    private CanvasGroup startChoiceCanvasGroup;
    private Vector3 startChoiceBaseScale = Vector3.one;
    private bool startChoiceBaseScaleInitialized;
    private Coroutine startChoiceAnimation;
    private CanvasGroup[] creditsCanvasGroups;
    private Vector3[] creditsBaseScales;
    private bool creditsBaseScaleInitialized;
    private Coroutine creditsAnimation;

    private void Awake()
    {
        // ผูกที่ Awake เพราะแผงตั้งค่าเริ่มมาแบบปิดอยู่ ปุ่มข้างในเลยยังไม่ทำงานถ้าไปรอผูกทีหลัง
        // ผูกจากโค้ดแทนการผูกใน Inspector จะได้ไม่มีเคสลืมผูกแล้วปุ่มกดไม่ติด
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(OnClickBack);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnClickCredits);
        if (creditsBackButton != null) creditsBackButton.onClick.AddListener(OnClickBack);
        if (loadButton != null) loadButton.onClick.AddListener(OnClickLoad);
        if (tutorialButton != null) tutorialButton.onClick.AddListener(OnClickTutorial);
        if (level1Button != null) level1Button.onClick.AddListener(OnClickLevel1);
        if (closeStartChoiceButton != null) closeStartChoiceButton.onClick.AddListener(OnClickCloseStartChoice);

        PrepareStartChoicePanel();
        HideStartChoicePanelImmediate();
        PrepareCreditsPanel();

        if (startTransition == StartTransitionType.BlackFade)
        {
            if (screenFader == null) screenFader = ScreenFader.FindOrCreate();
        }
        else if (blackCircleTransition == null)
        {
            blackCircleTransition = BlackCircleTransition.FindOrCreate();
        }
    }

    private void Start()
    {
        if (logoIntroVideo == null)
        {
            logoIntroVideo = FindObjectOfType<LogoIntroVideo>();
        }

        if (logoIntroVideo != null && logoIntroVideo.IsPlaying)
        {
            return;
        }

        ShowMainMenu();
    }



    // กดเริ่มเกม: เปิดแผงให้เลือกว่าจะเริ่มจากบทสอนหรือด่าน 1
    public void OnClickStart()
    {
        if (isStarting) return;

        if (startChoicePanel == null)
        {
            Debug.LogError("[MainMenu] Start choice panel is not assigned.");
            return;
        }

        ShowStartChoicePanel();
    }

    public void OnClickTutorial()
    {
        StartScene(tutorialSceneName);
    }

    public void OnClickLevel1()
    {
        StartScene(level1SceneName);
    }

    public void OnClickCloseStartChoice()
    {
        if (isStarting) return;

        HideStartChoicePanel();
    }

    private void StartScene(string sceneName)
    {
        if (isStarting) return;

        isStarting = true;
        StartCoroutine(StartSceneAfterPopupCloses(sceneName));
    }

    private IEnumerator StartSceneAfterPopupCloses(string sceneName)
    {
        yield return HideStartChoicePanelRoutine();

        if (startTransition == StartTransitionType.BlackFade)
        {
            if (screenFader == null)
            {
                screenFader = ScreenFader.FindOrCreate();
            }

            if (screenFader == null)
            {
                Debug.LogError("[MainMenu] สร้าง ScreenFader ไม่สำเร็จ จึงไม่โหลดฉากเกม");
                isStarting = false;
                yield break;
            }

            screenFader.FadeOutAndLoadScene(sceneName);
            yield break;
        }

        if (blackCircleTransition == null)
        {
            blackCircleTransition = BlackCircleTransition.FindOrCreate();
        }

        if (blackCircleTransition == null)
        {
            Debug.LogError("[MainMenu] สร้าง BlackCircleTransition ไม่สำเร็จ จึงไม่โหลดฉากเกม");
            isStarting = false;
            yield break;
        }

        if (!blackCircleTransition.BeginTransition(sceneName))
        {
            isStarting = false;
        }
    }

    private void PrepareStartChoicePanel()
    {
        if (startChoicePanel == null) return;

        startChoiceCanvasGroup = startChoicePanel.GetComponent<CanvasGroup>();
        if (startChoiceCanvasGroup == null)
        {
            startChoiceCanvasGroup = startChoicePanel.AddComponent<CanvasGroup>();
        }

        if (!startChoiceBaseScaleInitialized)
        {
            startChoiceBaseScale = startChoicePanel.transform.localScale;
            startChoiceBaseScaleInitialized = true;
        }
    }

    private void ShowStartChoicePanel()
    {
        if (startChoicePanel == null) return;

        PrepareStartChoicePanel();
        if (startChoiceAnimation != null) StopCoroutine(startChoiceAnimation);
        startChoiceAnimation = StartCoroutine(AnimateStartChoicePanel(true));
    }

    private void HideStartChoicePanel()
    {
        if (startChoicePanel == null) return;

        PrepareStartChoicePanel();
        if (startChoiceAnimation != null) StopCoroutine(startChoiceAnimation);
        startChoiceAnimation = StartCoroutine(AnimateStartChoicePanel(false));
    }

    private void HideStartChoicePanelImmediate()
    {
        if (startChoicePanel == null) return;

        if (startChoiceAnimation != null) StopCoroutine(startChoiceAnimation);
        startChoicePanel.SetActive(false);
        startChoicePanel.transform.localScale = startChoiceBaseScale;
        if (startChoiceCanvasGroup != null)
        {
            startChoiceCanvasGroup.alpha = 0f;
            startChoiceCanvasGroup.interactable = false;
            startChoiceCanvasGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator HideStartChoicePanelRoutine()
    {
        if (startChoicePanel == null || !startChoicePanel.activeSelf)
            yield break;

        PrepareStartChoicePanel();
        yield return AnimateStartChoicePanel(false);
    }

    private IEnumerator AnimateStartChoicePanel(bool show)
    {
        if (startChoicePanel == null || startChoiceCanvasGroup == null)
            yield break;

        if (show)
        {
            startChoicePanel.SetActive(true);
            startChoiceCanvasGroup.interactable = false;
            startChoiceCanvasGroup.blocksRaycasts = true;
        }

        float duration = Mathf.Max(0.01f, startChoiceAnimationDuration);
        float elapsed = 0f;
        float startAlpha = show ? 0f : startChoiceCanvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        float startScale = show ? 0.9f : 1f;
        float endScale = show ? 1f : 0.9f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = startChoiceAnimationCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            startChoiceCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            startChoicePanel.transform.localScale =
                startChoiceBaseScale * Mathf.Lerp(startScale, endScale, progress);
            yield return null;
        }

        startChoiceCanvasGroup.alpha = endAlpha;
        startChoicePanel.transform.localScale = startChoiceBaseScale * endScale;
        startChoiceCanvasGroup.interactable = show;
        startChoiceCanvasGroup.blocksRaycasts = show;
        if (!show) startChoicePanel.SetActive(false);
        startChoiceAnimation = null;
    }

    // กดโหลดเกม: เปิดแผงเลือกช่องเซฟ (ช่องอัตโนมัติ + ช่องผู้เล่น)
    // แผงสร้างตัวเองตอนถูกเรียกครั้งแรก ไม่ต้องเตรียมอะไรไว้ในฉาก
    public void OnClickLoad()
    {
        if (isStarting) return;

        SaveSlotPanel.OpenLoadPanel();
    }

    public void OnClickSetting()
    {
        GameObject panelToShow = GetSettingsPanel();
        if (panelToShow == null)
        {
            Debug.LogWarning("[MainMenu] ยังไม่ได้ผูก Settings Panel ไว้ใน Inspector เลยไม่มีแผงตั้งค่าให้เปิด");
            return;
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        panelToShow.SetActive(true);
        SetSettingsNavigationVisible(true);
    }

    public void OnClickCredits()
    {
        if (creditsPanel == null)
        {
            Debug.LogWarning("[MainMenu] ยังไม่ได้ผูก Credits Panel ไว้ใน Inspector เลยไม่มีแผงเครดิตให้เปิด");
            return;
        }

        if (creditsBook == null)
        {
            creditsBook = creditsPanel.GetComponentInChildren<CreditsBookUI>(true);
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (settingsPanelsRoot != null) settingsPanelsRoot.SetActive(false);
        creditsPanel.SetActive(true);
        PrepareCreditsPanel();
        if (creditsAnimation != null) StopCoroutine(creditsAnimation);
        creditsAnimation = StartCoroutine(AnimateCreditsPanel(true));

        if (creditsBook != null)
        {
            creditsBook.Open();
        }
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }



    // กลับจากแผงตั้งค่ามาหน้าเมนูหลัก
    public void OnClickBack()
    {
        if (creditsPanel != null && creditsPanel.activeSelf)
        {
            ShowMainMenu();
            return;
        }

        ShowMainMenu();
    }



    private void ShowMainMenu()
    {
        if (creditsAnimation != null)
        {
            StopCoroutine(creditsAnimation);
            creditsAnimation = null;
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (startChoicePanel != null) startChoicePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (settingsPanelsRoot != null) settingsPanelsRoot.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (creditsAnimationTargets != null)
        {
            for (int i = 0; i < creditsAnimationTargets.Length; i++)
            {
                if (creditsAnimationTargets[i] != null && creditsBaseScales != null &&
                    i < creditsBaseScales.Length)
                {
                    creditsAnimationTargets[i].localScale = creditsBaseScales[i];
                }
            }
        }
        SetSettingsNavigationVisible(false);

        // เช็คทุกครั้งที่กลับมาหน้าเมนูหลัก ไม่ใช่แค่ตอนเริ่มฉาก
        // เผื่อผู้เล่นเพิ่งลบเซฟทิ้งหรือเพิ่งเซฟมาจากที่อื่นในรอบเดียวกัน
        if (loadButton != null && disableLoadButtonWhenEmpty)
        {
            loadButton.interactable = SaveManager.HasAnySave();
        }
    }

    private void PrepareCreditsPanel()
    {
        if (creditsBook == null && creditsPanel != null)
        {
            creditsBook = creditsPanel.GetComponentInChildren<CreditsBookUI>(true);
        }

        if (creditsAnimationTargets == null || creditsAnimationTargets.Length == 0)
        {
            creditsAnimationTargets = creditsBook != null
                ? new[] { creditsBook.transform }
                : new Transform[0];
        }

        if (creditsAnimationTargets.Length == 0) return;

        if (creditsCanvasGroups == null || creditsCanvasGroups.Length != creditsAnimationTargets.Length)
        {
            creditsCanvasGroups = new CanvasGroup[creditsAnimationTargets.Length];
            creditsBaseScales = new Vector3[creditsAnimationTargets.Length];
            creditsBaseScaleInitialized = false;
        }

        for (int i = 0; i < creditsAnimationTargets.Length; i++)
        {
            Transform target = creditsAnimationTargets[i];
            if (target == null) continue;

            if (creditsCanvasGroups[i] == null)
            {
                creditsCanvasGroups[i] = target.GetComponent<CanvasGroup>();
                if (creditsCanvasGroups[i] == null)
                {
                    creditsCanvasGroups[i] = target.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (!creditsBaseScaleInitialized)
            {
                creditsBaseScales[i] = target.localScale;
            }
        }

        creditsBaseScaleInitialized = true;
    }

    private IEnumerator AnimateCreditsPanel(bool show)
    {
        PrepareCreditsPanel();
        if (creditsAnimationTargets == null || creditsCanvasGroups == null)
        {
            creditsAnimation = null;
            yield break;
        }

        for (int i = 0; i < creditsAnimationTargets.Length; i++)
        {
            if (creditsAnimationTargets[i] == null || creditsCanvasGroups[i] == null) continue;

            creditsCanvasGroups[i].interactable = false;
            creditsCanvasGroups[i].blocksRaycasts = show;
        }

        float duration = Mathf.Max(0.01f, creditsAnimationDuration);
        float elapsed = 0f;
        float startScale = show ? 0.9f : 1f;
        float endScale = show ? 1f : 0.9f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = creditsAnimationCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < creditsAnimationTargets.Length; i++)
            {
                if (creditsAnimationTargets[i] == null || creditsCanvasGroups[i] == null) continue;

                float startAlpha = show ? 0f : creditsCanvasGroups[i].alpha;
                float endAlpha = show ? 1f : 0f;
                creditsCanvasGroups[i].alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                creditsAnimationTargets[i].localScale =
                    creditsBaseScales[i] * Mathf.Lerp(startScale, endScale, progress);
            }
            yield return null;
        }

        for (int i = 0; i < creditsAnimationTargets.Length; i++)
        {
            if (creditsAnimationTargets[i] == null || creditsCanvasGroups[i] == null) continue;

            creditsCanvasGroups[i].alpha = show ? 1f : 0f;
            creditsAnimationTargets[i].localScale = creditsBaseScales[i] * endScale;
            creditsCanvasGroups[i].interactable = show;
            creditsCanvasGroups[i].blocksRaycasts = show;
        }
        if (!show)
        {
            creditsPanel.SetActive(false);
            creditsAnimation = null;
            ShowMainMenu();
        }
        else
        {
            creditsAnimation = null;
        }
    }

    private GameObject GetSettingsPanel()
    {
        return settingsPanelsRoot != null ? settingsPanelsRoot : settingsPanel;
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
        if (settingsBackButton != null) settingsBackButton.onClick.RemoveListener(OnClickBack);
        if (creditsButton != null) creditsButton.onClick.RemoveListener(OnClickCredits);
        if (creditsBackButton != null) creditsBackButton.onClick.RemoveListener(OnClickBack);
        if (loadButton != null) loadButton.onClick.RemoveListener(OnClickLoad);
        if (tutorialButton != null) tutorialButton.onClick.RemoveListener(OnClickTutorial);
        if (level1Button != null) level1Button.onClick.RemoveListener(OnClickLevel1);
        if (closeStartChoiceButton != null) closeStartChoiceButton.onClick.RemoveListener(OnClickCloseStartChoice);
    }
}