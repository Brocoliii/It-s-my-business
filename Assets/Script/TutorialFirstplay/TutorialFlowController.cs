using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using System.Collections;
using System.Collections.Generic;

public class TutorialFlowController : MonoBehaviour
{
    public static bool IsStepPanelOpen { get; private set; }

    [System.Serializable]
    private class TutorialStepPanel
    {
        [Tooltip("Panel shown for this tutorial step.")]
        public GameObject panel;
        [Tooltip("Optional text updated with this step's instruction.")]
        public TMP_Text instructionText;
        [Tooltip("Localized instruction for this tutorial step. Leave empty to use the default step text.")]
        public LocalizedString instructionLocalized;
        [Tooltip("Button that confirms this tutorial step.")]
        public Button okButton;
    }

    private enum TutorialStep
    {
        MoveCamera,
        Cook,
        Sauce,
        Spicy,
        Cup,
        Serve,
        Investigate,
        FinishInvestigation,
        Complete
    }

    [Header("Popup")]
    [SerializeField] private bool buildPopupAutomatically = true;
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TextMeshProUGUI stepContainer;
    [SerializeField] private Vector2 popupOffset = new Vector2(32f, -32f);

    [Header("Localization")]
    [Tooltip("Localized title shown while the tutorial is active.")]
    [SerializeField] private LocalizedString tutorialTitleLocalized;
    [Tooltip("Localized title shown when the tutorial is complete.")]
    [SerializeField] private LocalizedString dayCompleteTitleLocalized;
    [Tooltip("Localized text shown in the popup step container, in the same order as the tutorial steps.")]
    [SerializeField] private LocalizedString[] stepContainerLocalized;

    [Header("Popup Animation")]
    [SerializeField] private float popupAppearDuration = 0.3f;
    [SerializeField] private float popupSuccessDuration = 0.22f;
    [SerializeField, Range(0.5f, 1f)] private float popupAppearStartScale = 0.88f;
    [SerializeField, Range(1f, 1.2f)] private float popupSuccessStartScale = 1.04f;

    [Header("Tutorial")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private float completeHoldSeconds = 2f;
    [SerializeField] private float cameraMovementRequiredDistance = 1.5f;

    [Header("Step Confirmation Panels")]
    [Tooltip("Assign one panel per tutorial step in order: Move Camera, Cook, Sauce, Spicy, Cup, Serve, Investigate, Finish Investigation.")]
    [SerializeField] private TutorialStepPanel[] stepPanels;
    [SerializeField] private float stepPanelAnimationDuration = 0.2f;
    [SerializeField, Range(0.5f, 1f)] private float stepPanelStartScale = 0.9f;

    [Header("Step Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField] private float highlightSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float highlightStrength = 0.75f;

    [Header("Highlight Sprite Renderers")]
    [Tooltip("Drag the FoodTray SpriteRenderer components here.")]
    [SerializeField] private SpriteRenderer[] foodTrayHighlightRenderers;
    [Tooltip("Drag the GrillStation SpriteRenderer components here.")]
    [SerializeField] private SpriteRenderer[] grillStationHighlightRenderers;
    [Tooltip("Drag the SauceBrush SpriteRenderer components here.")]
    [SerializeField] private SpriteRenderer[] sauceBrushHighlightRenderers;
    [Tooltip("Drag the Malachili bottle SpriteRenderer components here.")]
    [SerializeField] private SpriteRenderer[] malachiliBottleHighlightRenderers;
    [Tooltip("Drag the Cup SpriteRenderer components here.")]
    [SerializeField] private SpriteRenderer[] cupHighlightRenderers;

    private readonly string[] stepLabels =
   {
      "Move the Camera",
      "Grill the Skewers",
      "Brush on the Sauce",
      "Reach Spice Level 3",
      "Fill the Cup",
      "Serve the Customer",
      "Investigate the Group",
      "Finish Investigating"
  };

    private TutorialStep currentStep;
    private CustomerManager customerManager;
    private InvestigationManager investigationManager;
    private GameManager gameManager;
    private int startingCompletedOrders;
    private InvestigateGroup tutorialGroup;
    private bool tutorialGroupRequested;
    private float completeTimer;
    private bool isComplete;
    private bool tutorialStarted;
    private bool currentStepAcknowledged;
    private GameObject activeStepPanel;
    private Coroutine stepPanelAnimationCoroutine;
    private float timeScaleBeforeStepPanel = 1f;
    private bool stepPanelPausedGame;
    private Vector3 cameraStartPosition;
    private bool cameraStartPositionSet;
    private int startingFoodInstanceCount;
    private bool foodHasBeenPickedUp;
    private bool popupTransitioning;
    private Coroutine popupAnimationCoroutine;
    private CanvasGroup popupCanvasGroup;
    private CanvasGroup titleCanvasGroup;
    private CanvasGroup stepCanvasGroup;
    private Vector3 popupBaseScale = Vector3.one;
    private readonly HashSet<FoodInstance> startingFoods = new HashSet<FoodInstance>();
    private readonly List<SpriteRenderer> highlightedRenderers = new List<SpriteRenderer>();
    private readonly List<Color> originalRendererColors = new List<Color>();
    private readonly Dictionary<int, LocalizedString.ChangeHandler> stepLocalizationCallbacks =
        new Dictionary<int, LocalizedString.ChangeHandler>();
    private readonly Dictionary<int, LocalizedString.ChangeHandler> stepContainerLocalizationCallbacks =
        new Dictionary<int, LocalizedString.ChangeHandler>();

    private readonly Color currentColor = new Color(0.35f, 0.75f, 1f, 1f);
    private bool localizationSubscribed;

    private void Awake()
    {
        IsStepPanelOpen = false;
        gameManager = FindObjectOfType<GameManager>();
        customerManager = FindObjectOfType<CustomerManager>();
        investigationManager = FindObjectOfType<InvestigationManager>();

        HideAllStepPanels();
        AddStepPanelListeners();

        if (customerManager != null)
        {
            customerManager.tutorialSingleCustomerMode = true;
            customerManager.minFoodPerOrder = 1;
            customerManager.maxFoodPerOrder = 1;
            customerManager.maxTotalFoodOnScreen = 1;
        }

        if (gameManager != null)
            gameManager.stopSpawningCustomers = true;

        StageConfig stage = gameManager != null ? gameManager.GetCurrentStage() : null;
        if (stage != null)
        {
            stage.maxSpicyLevel = Mathf.Max(stage.maxSpicyLevel, 3);
            stage.allowSauce = true;
        }

        if (investigationManager != null)
        {
            investigationManager.tutorialControlled = true;
        }
    }

    private bool HasMovedCamera()
    {
        if (!cameraStartPositionSet)
            return true;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Vector2 cameraPosition = mainCamera.transform.position;
        Vector2 startPosition = cameraStartPosition;
        return Vector2.Distance(cameraPosition, startPosition)
            >= Mathf.Max(0.05f, cameraMovementRequiredDistance);
    }

    private void Start()
    {
        EnsurePopupAnimationComponents();
        HideTutorialPopup();
        ClearStepHighlight();

        if (!startAutomatically)
            return;

        if (FindObjectOfType<IntroDialogue>() != null)
            return;

        BeginTutorial();
    }

    private void OnEnable()
    {
        IntroDialogue.DialogueFinished += BeginTutorial;
        SubscribeLocalization();
    }

    private void OnDisable()
    {
        IntroDialogue.DialogueFinished -= BeginTutorial;
        UnsubscribeLocalization();
    }

    private void SubscribeLocalization()
    {
        if (localizationSubscribed)
            return;

        if (tutorialTitleLocalized != null)
            tutorialTitleLocalized.StringChanged += OnTutorialTitleChanged;
        if (dayCompleteTitleLocalized != null)
            dayCompleteTitleLocalized.StringChanged += OnDayCompleteTitleChanged;

        if (stepPanels != null)
        {
            for (int i = 0; i < stepPanels.Length; i++)
            {
                TutorialStepPanel stepPanel = stepPanels[i];
                if (stepPanel != null && stepPanel.instructionLocalized != null)
                {
                    int panelIndex = i;
                    LocalizedString.ChangeHandler callback = localizedText =>
                        OnStepInstructionChanged(panelIndex, localizedText);
                    stepLocalizationCallbacks[panelIndex] = callback;
                    stepPanel.instructionLocalized.StringChanged += callback;
                }
            }
        }

        if (stepContainerLocalized != null)
        {
            for (int i = 0; i < stepContainerLocalized.Length; i++)
            {
                if (stepContainerLocalized[i] == null)
                    continue;

                int stepIndex = i;
                LocalizedString.ChangeHandler callback = localizedText =>
                    OnStepContainerChanged(stepIndex, localizedText);
                stepContainerLocalizationCallbacks[stepIndex] = callback;
                stepContainerLocalized[i].StringChanged += callback;
            }
        }

        localizationSubscribed = true;
        RefreshLocalizedStrings();
    }

    private void UnsubscribeLocalization()
    {
        if (!localizationSubscribed)
            return;

        if (tutorialTitleLocalized != null)
            tutorialTitleLocalized.StringChanged -= OnTutorialTitleChanged;
        if (dayCompleteTitleLocalized != null)
            dayCompleteTitleLocalized.StringChanged -= OnDayCompleteTitleChanged;

        if (stepPanels != null)
        {
            for (int i = 0; i < stepPanels.Length; i++)
            {
                TutorialStepPanel stepPanel = stepPanels[i];
                if (stepPanel != null && stepPanel.instructionLocalized != null)
                {
                    if (stepLocalizationCallbacks.TryGetValue(
                        i,
                        out LocalizedString.ChangeHandler callback))
                    {
                        stepPanel.instructionLocalized.StringChanged -= callback;
                    }
                }
            }
        }

        if (stepContainerLocalized != null)
        {
            for (int i = 0; i < stepContainerLocalized.Length; i++)
            {
                if (stepContainerLocalized[i] == null)
                    continue;

                if (stepContainerLocalizationCallbacks.TryGetValue(
                    i,
                    out LocalizedString.ChangeHandler callback))
                {
                    stepContainerLocalized[i].StringChanged -= callback;
                }
            }
        }

        stepLocalizationCallbacks.Clear();
        stepContainerLocalizationCallbacks.Clear();
        localizationSubscribed = false;
    }

    private void RefreshLocalizedStrings()
    {
        if (tutorialTitleLocalized != null)
            tutorialTitleLocalized.RefreshString();
        if (dayCompleteTitleLocalized != null)
            dayCompleteTitleLocalized.RefreshString();

        if (stepPanels != null)
        {
            foreach (TutorialStepPanel stepPanel in stepPanels)
            {
                if (stepPanel != null && stepPanel.instructionLocalized != null)
                    stepPanel.instructionLocalized.RefreshString();
            }
        }

        if (stepContainerLocalized != null)
        {
            foreach (LocalizedString localizedString in stepContainerLocalized)
            {
                if (localizedString != null)
                    localizedString.RefreshString();
            }
        }
    }

    private void OnTutorialTitleChanged(string localizedText)
    {
        if (currentStep != TutorialStep.Complete && titleText != null && !string.IsNullOrWhiteSpace(localizedText))
            titleText.text = localizedText;
    }

    private void OnDayCompleteTitleChanged(string localizedText)
    {
        if (currentStep == TutorialStep.Complete && titleText != null && !string.IsNullOrWhiteSpace(localizedText))
            titleText.text = localizedText;
    }

    private void OnStepInstructionChanged(int panelIndex, string localizedText)
    {
        if (string.IsNullOrWhiteSpace(localizedText) || stepPanels == null ||
            panelIndex != (int)currentStep)
            return;

        if (panelIndex >= 0 && panelIndex < stepPanels.Length &&
            stepPanels[panelIndex] != null && stepPanels[panelIndex].instructionText != null)
        {
            stepPanels[panelIndex].instructionText.text = localizedText;
        }

    }

    private void OnStepContainerChanged(int stepIndex, string localizedText)
    {
        if (stepContainer == null || currentStep == TutorialStep.Complete ||
            stepIndex != (int)currentStep || string.IsNullOrWhiteSpace(localizedText))
            return;

        stepContainer.text = "> " + localizedText;
    }

    private string GetStepContainerText(int stepIndex)
    {
        if (stepContainerLocalized != null && stepIndex >= 0 &&
            stepIndex < stepContainerLocalized.Length &&
            stepContainerLocalized[stepIndex] != null &&
            !string.IsNullOrWhiteSpace(stepContainerLocalized[stepIndex].GetLocalizedString()))
        {
            return stepContainerLocalized[stepIndex].GetLocalizedString();
        }

        return stepLabels[stepIndex];
    }

    private void BeginTutorial()
    {
        if (tutorialStarted || !startAutomatically)
            return;

        tutorialStarted = true;
        startingCompletedOrders = gameManager != null ? gameManager.completedOrders : 0;
        FoodInstance[] existingFoods = FindObjectsOfType<FoodInstance>();
        startingFoodInstanceCount = existingFoods.Length;
        foreach (FoodInstance food in existingFoods)
        {
            if (food != null)
                startingFoods.Add(food);
        }
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraStartPosition = mainCamera.transform.position;
            cameraStartPositionSet = true;
        }
        currentStep = TutorialStep.MoveCamera;

        if (buildPopupAutomatically && popupRoot == null)
        {
            BuildPopup();
        }

        RefreshPopup();
        ShowTutorialPopup(true);
        ShowStepPanel();
        RefreshStepHighlight();
    }

    private void Update()
    {
        if (!tutorialStarted || isComplete || popupTransitioning || gameManager == null)
            return;

        if (gameManager.CurrentState != GameManager.GameState.Playing)
            return;

        if (!currentStepAcknowledged)
            return;

        RefreshStepHighlight();

        switch (currentStep)
        {
            case TutorialStep.MoveCamera:
                if (HasMovedCamera())
                {
                    ReleaseTutorialCustomer();
                    Advance();
                }
                break;
            case TutorialStep.Cook:
                if (HasCookedFood()) Advance();
                break;
            case TutorialStep.Sauce:
                if (HasSaucedFood()) Advance();
                break;
            case TutorialStep.Spicy:
                if (HasLevelThreeFood()) Advance();
                break;
            case TutorialStep.Cup:
                if (HasFoodInCup()) Advance();
                break;
            case TutorialStep.Serve:
                if (gameManager.completedOrders > startingCompletedOrders)
                {
                    Advance();
                    SpawnTutorialGroup();
                }
                break;
            case TutorialStep.Investigate:
                if (tutorialGroup != null && tutorialGroup.IsClueCollected)
                    Advance();
                break;
            case TutorialStep.FinishInvestigation:
                if (tutorialGroup != null && tutorialGroup.IsClueCollected)
                    Advance();
                break;
            case TutorialStep.Complete:
                completeTimer += Time.deltaTime;
                if (completeTimer >= completeHoldSeconds)
                {
                    isComplete = true;
                    gameManager.stopSpawningCustomers = true;
                    gameManager.StartEndOfDaySequence();
                }
                break;
        }
    }

    private void ReleaseTutorialCustomer()
    {
        if (gameManager == null || customerManager == null)
            return;

        gameManager.stopSpawningCustomers = false;

        if (!customerManager.tutorialCustomerHasSpawned
            && customerManager.customerSlots != null
            && customerManager.customerSlots.Length > 0)
        {
            customerManager.SpawnCustomer(0);
        }
    }

    private bool HasCookedFood()
    {
        if (!foodHasBeenPickedUp)
            return false;

        FoodInstance[] foods = FindObjectsOfType<FoodInstance>();
        foreach (FoodInstance food in foods)
        {
            if (food != null
                && food.GetCurrentState() == FoodInstance.CookState.Cooked
                && !startingFoods.Contains(food))
                return true;
        }

        return false;
    }

    private bool HasSaucedFood()
    {
        FoodInstance[] foods = FindObjectsOfType<FoodInstance>();
        foreach (FoodInstance food in foods)
        {
            if (food != null && food.hasSauce)
                return true;
        }

        return false;
    }

    private bool HasLevelThreeFood()
    {
        FoodInstance[] foods = FindObjectsOfType<FoodInstance>();
        foreach (FoodInstance food in foods)
        {
            if (food != null && food.spicyLevel >= 3)
                return true;
        }

        return false;
    }

    private bool HasFoodInCup()
    {
        Cup[] cups = FindObjectsOfType<Cup>();
        foreach (Cup cup in cups)
        {
            if (cup != null && cup.contents.Count > 0)
                return true;
        }

        return false;
    }

    private void SpawnTutorialGroup()
    {
        if (tutorialGroupRequested || investigationManager == null)
            return;

        tutorialGroupRequested = true;
        investigationManager.SpawnTutorialGroup();
        tutorialGroup = FindObjectOfType<InvestigateGroup>();
    }

    private void Advance()
    {
        if (!currentStepAcknowledged)
            return;

        ClearStepHighlight();
        if (popupTransitioning)
            return;

        TutorialStep nextStep = (TutorialStep)((int)currentStep + 1);
        StartPopupAnimation(PopupTransitionToStep(nextStep));
    }

    private IEnumerator PopupTransitionToStep(TutorialStep nextStep)
    {
        yield return AnimatePopup(1f, popupSuccessStartScale, 0f, popupSuccessDuration);

        currentStep = nextStep;
        if (currentStep == TutorialStep.Complete)
        {
            popupAnimationCoroutine = null;
            popupTransitioning = false;
            HideTutorialPopup();
            yield break;
        }

        RefreshPopup();
        ShowStepPanel();
        yield return AnimatePopup(0f, popupAppearStartScale, 1f, popupAppearDuration);
    }

    private void ShowStepPanel()
    {
        HideAllStepPanels();

        if (currentStep == TutorialStep.Complete)
        {
            currentStepAcknowledged = true;
            return;
        }

        int panelIndex = (int)currentStep;
        if (stepPanels == null || panelIndex >= stepPanels.Length || stepPanels[panelIndex] == null)
        {
            currentStepAcknowledged = true;
            return;
        }

        TutorialStepPanel currentPanel = stepPanels[panelIndex];
        if (currentPanel.okButton == null)
        {
            Debug.LogError("[TutorialFlowController] The current tutorial step panel is missing its OK button. Step confirmation is disabled for this step.");
            currentStepAcknowledged = true;
            return;
        }

        currentStepAcknowledged = false;
        if (currentPanel.instructionText != null)
        {
            currentPanel.instructionText.text = stepLabels[panelIndex];
            if (currentPanel.instructionLocalized != null)
                currentPanel.instructionLocalized.RefreshString();
        }

        if (currentPanel.panel != null)
        {
            PauseForStepPanel();
            activeStepPanel = currentPanel.panel;
            SoundManager.Instance.PlaySFX(SoundIds.PopUpAppear);
            StartStepPanelAnimation(activeStepPanel, true);
        }
    }

    private void AcknowledgeCurrentStep()
    {
        if (!tutorialStarted || isComplete || currentStep == TutorialStep.Complete)
            return;

        currentStepAcknowledged = true;
        HideActiveStepPanel(true);
    }

    private void AddStepPanelListeners()
    {
        if (stepPanels == null)
            return;

        foreach (TutorialStepPanel stepPanel in stepPanels)
        {
            if (stepPanel != null && stepPanel.okButton != null)
                stepPanel.okButton.onClick.AddListener(AcknowledgeCurrentStep);
        }
    }

    private void HideAllStepPanels()
    {
        if (stepPanelAnimationCoroutine != null)
        {
            StopCoroutine(stepPanelAnimationCoroutine);
            stepPanelAnimationCoroutine = null;
        }

        if (stepPanels == null)
            return;

        foreach (TutorialStepPanel stepPanel in stepPanels)
        {
            if (stepPanel != null && stepPanel.panel != null)
                stepPanel.panel.SetActive(false);
        }

        activeStepPanel = null;
        ResumeAfterStepPanel();
    }

    private void HideActiveStepPanel(bool animate)
    {
        if (activeStepPanel == null)
            return;

        if (!animate)
        {
            activeStepPanel.SetActive(false);
            activeStepPanel = null;
            ResumeAfterStepPanel();
            return;
        }

        StartStepPanelAnimation(activeStepPanel, false);
    }

    private void StartStepPanelAnimation(GameObject panel, bool show)
    {
        if (stepPanelAnimationCoroutine != null)
            StopCoroutine(stepPanelAnimationCoroutine);

        stepPanelAnimationCoroutine = StartCoroutine(AnimateStepPanel(panel, show));
    }

    private IEnumerator AnimateStepPanel(GameObject panel, bool show)
    {
        if (panel == null)
            yield break;

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        Vector3 endScale = panel.transform.localScale;
        if (endScale == Vector3.zero)
            endScale = Vector3.one;

        Vector3 startScale = show ? endScale * stepPanelStartScale : endScale;
        Vector3 targetScale = show ? endScale : endScale * stepPanelStartScale;
        float startAlpha = show ? 0f : 1f;
        float targetAlpha = show ? 1f : 0f;

        panel.SetActive(true);
        panel.transform.localScale = startScale;
        canvasGroup.alpha = startAlpha;

        float elapsed = 0f;
        float duration = Mathf.Max(0f, stepPanelAnimationDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            panel.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        panel.transform.localScale = targetScale;
        if (!show)
        {
            panel.SetActive(false);
            if (activeStepPanel == panel)
            {
                activeStepPanel = null;
                ResumeAfterStepPanel();
            }
        }

        stepPanelAnimationCoroutine = null;
    }

    private void PauseForStepPanel()
    {
        if (stepPanelPausedGame)
            return;

        timeScaleBeforeStepPanel = Time.timeScale;
        Time.timeScale = 0f;
        stepPanelPausedGame = true;
        IsStepPanelOpen = true;
    }

    private void ResumeAfterStepPanel()
    {
        if (!stepPanelPausedGame)
            return;

        Time.timeScale = timeScaleBeforeStepPanel;
        stepPanelPausedGame = false;
        IsStepPanelOpen = false;
    }

    private void RefreshStepHighlight()
    {
        List<SpriteRenderer> targetRenderers = new List<SpriteRenderer>();

        switch (currentStep)
        {
            case TutorialStep.Cook:
                if (!foodHasBeenPickedUp
                    && FindObjectsOfType<FoodInstance>().Length > startingFoodInstanceCount)
                {
                    foodHasBeenPickedUp = true;
                }

                if (!foodHasBeenPickedUp)
                    AddTargetRenderers(targetRenderers, foodTrayHighlightRenderers);
                else
                    AddTargetRenderers(targetRenderers, grillStationHighlightRenderers);
                break;
            case TutorialStep.Sauce:
                AddTargetRenderers(targetRenderers, sauceBrushHighlightRenderers);
                break;
            case TutorialStep.Spicy:
                AddTargetRenderers(targetRenderers, malachiliBottleHighlightRenderers);
                break;
            case TutorialStep.Cup:
                AddTargetRenderers(targetRenderers, cupHighlightRenderers);
                break;
        }

        if (SameRenderers(targetRenderers))
            return;

        ClearStepHighlight();
        foreach (SpriteRenderer renderer in targetRenderers)
        {
            if (renderer == null)
                continue;

            highlightedRenderers.Add(renderer);
            originalRendererColors.Add(renderer.color);
        }
    }

    private void AddTargetRenderers(List<SpriteRenderer> renderers, SpriteRenderer[] targets)
    {
        if (targets == null)
            return;

        foreach (SpriteRenderer renderer in targets)
        {
            if (renderer == null)
                continue;

            if (!renderers.Contains(renderer))
                renderers.Add(renderer);
        }
    }

    private bool SameRenderers(List<SpriteRenderer> targetRenderers)
    {
        if (targetRenderers.Count != highlightedRenderers.Count)
            return false;

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            if (targetRenderers[i] != highlightedRenderers[i])
                return false;
        }

        return true;
    }

    private void ClearStepHighlight()
    {
        for (int i = 0; i < highlightedRenderers.Count; i++)
        {
            if (highlightedRenderers[i] != null)
                highlightedRenderers[i].color = originalRendererColors[i];
        }

        highlightedRenderers.Clear();
        originalRendererColors.Clear();
    }

    private void UpdateStepHighlightColors()
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * highlightSpeed) + 1f) * 0.5f;
        float strength = pulse * highlightStrength;

        for (int i = highlightedRenderers.Count - 1; i >= 0; i--)
        {
            SpriteRenderer renderer = highlightedRenderers[i];
            if (renderer == null)
            {
                highlightedRenderers.RemoveAt(i);
                originalRendererColors.RemoveAt(i);
                continue;
            }

            Color highlightedColor = Color.Lerp(originalRendererColors[i], highlightColor, strength);
            highlightedColor.a = originalRendererColors[i].a;
            renderer.color = highlightedColor;
        }
    }

    private void BuildPopup()
    {
        GameObject canvasObject = new GameObject("Tutorial Popup Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Tutorial Popup");
        panelObject.transform.SetParent(canvasObject.transform, false);
        popupRoot = panelObject.AddComponent<RectTransform>();
        popupRoot.anchorMin = new Vector2(0f, 1f);
        popupRoot.anchorMax = new Vector2(0f, 1f);
        popupRoot.pivot = new Vector2(0f, 1f);
        popupRoot.anchoredPosition = popupOffset;
        popupRoot.sizeDelta = new Vector2(410f, 0f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.08f, 0.14f, 0.94f);

        VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 18, 18);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        panelObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateText("TUTORIAL", panelObject.transform, 27f, Color.white);
        stepContainer = CreateText(string.Empty, panelObject.transform, 20f, currentColor) as TextMeshProUGUI;
        EnsurePopupAnimationComponents();
    }

    private void EnsurePopupAnimationComponents()
    {
        if (popupRoot == null)
            return;

        popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
        if (popupCanvasGroup == null)
            popupCanvasGroup = popupRoot.gameObject.AddComponent<CanvasGroup>();

        if (titleText != null)
        {
            titleCanvasGroup = titleText.GetComponent<CanvasGroup>();
            if (titleCanvasGroup == null)
                titleCanvasGroup = titleText.gameObject.AddComponent<CanvasGroup>();
        }

        if (stepContainer != null)
        {
            stepCanvasGroup = stepContainer.GetComponent<CanvasGroup>();
            if (stepCanvasGroup == null)
                stepCanvasGroup = stepContainer.gameObject.AddComponent<CanvasGroup>();
        }

        popupBaseScale = popupRoot.localScale;
        if (popupBaseScale == Vector3.zero)
            popupBaseScale = Vector3.one;
    }

    private TMP_Text CreateText(string text, Transform parent, float size, Color color)
    {
        GameObject textObject = new GameObject("Tutorial Text");
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = size;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private void RefreshPopup()
    {
        if (titleText != null)
        {
            bool isCompleteStep = currentStep == TutorialStep.Complete;
            titleText.text = isCompleteStep ? "DAY COMPLETE" : "TUTORIAL";

            if (isCompleteStep)
            {
                if (dayCompleteTitleLocalized != null)
                    dayCompleteTitleLocalized.RefreshString();
            }
            else
            {
                if (tutorialTitleLocalized != null)
                    tutorialTitleLocalized.RefreshString();
            }
        }

        if (stepContainer != null)
        {
            bool showStep = currentStep != TutorialStep.Complete;
            stepContainer.gameObject.SetActive(showStep);

            if (showStep)
            {
                stepContainer.text = "> " + GetStepContainerText((int)currentStep);

                int stepIndex = (int)currentStep;
                if (stepContainerLocalized != null && stepIndex < stepContainerLocalized.Length &&
                    stepContainerLocalized[stepIndex] != null)
                {
                    stepContainerLocalized[stepIndex].RefreshString();
                }
            }
        }
    }

    public void HideTutorialPopup()
    {
        if (popupAnimationCoroutine != null)
        {
            StopCoroutine(popupAnimationCoroutine);
            popupAnimationCoroutine = null;
        }

        popupTransitioning = false;
        if (popupRoot != null)
            popupRoot.gameObject.SetActive(false);

        if (titleText != null)
            titleText.gameObject.SetActive(false);

        if (stepContainer != null)
            stepContainer.gameObject.SetActive(false);

        HideAllStepPanels();
    }

    private void ShowTutorialPopup(bool animate)
    {
        EnsurePopupAnimationComponents();

        if (popupRoot != null)
            popupRoot.gameObject.SetActive(true);

        if (titleText != null)
            titleText.gameObject.SetActive(true);

        if (stepContainer != null && currentStep != TutorialStep.Complete)
            stepContainer.gameObject.SetActive(true);

        if (animate)
        {
            StartPopupAnimation(AnimatePopup(
                0f,
                popupAppearStartScale,
                1f,
                popupAppearDuration));
        }
        else
        {
            SetPopupVisualState(1f, 1f);
        }
    }

    private void StartPopupAnimation(IEnumerator animation)
    {
        if (popupAnimationCoroutine != null)
            StopCoroutine(popupAnimationCoroutine);

        popupAnimationCoroutine = StartCoroutine(RunPopupAnimation(animation));
    }

    private IEnumerator RunPopupAnimation(IEnumerator animation)
    {
        popupTransitioning = true;
        yield return animation;
        popupTransitioning = false;
        popupAnimationCoroutine = null;
    }

    private IEnumerator AnimatePopup(
        float startAlpha,
        float startScale,
        float endAlpha,
        float duration)
    {
        if (popupRoot == null)
            yield break;

        SetPopupVisualState(startAlpha, startScale);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            t = t * t * (3f - 2f * t);
            SetPopupVisualState(
                Mathf.Lerp(startAlpha, endAlpha, t),
                Mathf.Lerp(startScale, 1f, t));
            yield return null;
        }

        SetPopupVisualState(endAlpha, 1f);
    }

    private void SetPopupVisualState(float alpha, float scale)
    {
        if (popupCanvasGroup != null)
            popupCanvasGroup.alpha = Mathf.Clamp01(alpha);

        if (titleCanvasGroup != null)
            titleCanvasGroup.alpha = Mathf.Clamp01(alpha);

        if (stepCanvasGroup != null)
            stepCanvasGroup.alpha = Mathf.Clamp01(alpha);

        if (popupRoot != null)
            popupRoot.localScale = popupBaseScale * scale;
    }

    private void LateUpdate()
    {
        if (tutorialStarted && !isComplete)
            UpdateStepHighlightColors();
    }

    private void OnDestroy()
    {
        ResumeAfterStepPanel();
        IsStepPanelOpen = false;

        if (stepPanels != null)
        {
            foreach (TutorialStepPanel stepPanel in stepPanels)
            {
                if (stepPanel != null && stepPanel.okButton != null)
                    stepPanel.okButton.onClick.RemoveListener(AcknowledgeCurrentStep);
            }
        }

        ClearStepHighlight();
    }
}
