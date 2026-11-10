using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class IntroDialogue : MonoBehaviour
{
    public static event System.Action DialogueFinished;

    [Header("การเดินของ NPC")]
    public Transform startPoint;
    public Transform stopPoint;
    [Tooltip("เวลาหน่วงก่อนเริ่มเดินเข้ามา (วินาที)")]
    public float preWalkDelay = 0f;
    public float walkSpeed = 5f;
    [Tooltip("ความสูงของการเด้งขึ้นลงตอน NPC เดิน (ใส่ 0 ถ้าไม่ต้องการเด้ง)")]
    public float walkBobHeight = 0.15f;
    [Tooltip("ความเร็วของการเด้งขึ้นลงตอนเดิน (ยิ่งมากยิ่งเดินถี่)")]
    public float walkBobSpeed = 10f;
    [Tooltip("เวลาที่ NPC เดินก่อนกล้องจะเริ่มซูมและติดตาม (วินาที)")]
    public float introCameraFollowDelay = 0.35f;
    [Tooltip("เวลาที่ NPC เดินออกก่อนกล้องจะเริ่มซูมออก (วินาที)")]
    public float introCameraReturnDelay = 0.35f;

    [Header("UI กรอบข้อความ")]
    public Transform dialogBubble;
    public TMP_Text dialogText;
    [Tooltip("ความเร็วในการพิมพ์ตัวอักษร (ยิ่งน้อยยิ่งพิมพ์เร็ว)")]
    public float typeSpeed = 0.05f; // ✨ เพิ่มช่องปรับความเร็วการพิมพ์

    [Header("Cinematic Black Bars")]
    [Tooltip("แถบดำด้านบนของจอ (ปล่อยว่างได้ถ้าไม่ใช้)")]
    public RectTransform upperBlackBar;
    [Tooltip("แถบดำด้านล่างของจอ (ปล่อยว่างได้ถ้าไม่ใช้)")]
    public RectTransform lowerBlackBar;
    [Tooltip("ระยะที่แถบดำเลื่อนออกนอกจอก่อนเริ่มบทสนทนา")]
    public float blackBarMoveDistance = 250f;
    [Tooltip("เวลาหน่วงก่อนแถบดำเริ่มเลื่อนเข้ามา")]
    public float blackBarStartDelay = 0.15f;
    [Tooltip("ระยะเวลาแถบดำเลื่อนเข้า/ออก")]
    public float blackBarAnimationDuration = 0.35f;

    [Header("🗣️ สไปรท์ตอนพูด")]
    [Tooltip("SpriteRenderer ของตัว NPC ที่จะสลับสไปรท์ตอนพูด (ถ้าไม่ใส่จะหาให้อัตโนมัติจาก GetComponent)")]
    public SpriteRenderer npcSpriteRenderer;
    [Tooltip("สไปรท์ตอนไม่ได้พูด (ปากปิด/หน้านิ่ง) ถ้าไม่ใส่จะใช้สไปรท์เดิมที่ผูกไว้ตอนเริ่มเกม")]
    public Sprite idleSprite;
    [Tooltip("สไปรท์ที่จะวนสลับกันตอนกำลังพิมพ์ข้อความ (เช่น ปากเปิด/ปากปิดสลับกัน) ให้ดูเหมือนกำลังพูดอยู่")]
    public Sprite[] talkingSprites;
    [Tooltip("ความเร็วในการสลับเฟรมตอนพูด (วินาทีต่อเฟรม ยิ่งน้อยยิ่งขยับปากถี่)")]
    public float talkFrameInterval = 0.12f;

    [Header("🔊 เสียงตอนพูด")]
    [Tooltip("เล่นเสียงพิมพ์ตัวอักษร (SoundIds.DialogueType) ตอน NPC กำลังพูด\nยังไม่ได้ใส่ไฟล์เสียงในคลังก็ไม่พัง แค่เงียบไปเฉย ๆ")]
    public bool playTypingSound = true;
    [Tooltip("รหัสเสียงพิมพ์ตัวอักษรที่ใช้กับบทสนทนานี้ เปลี่ยนเป็น SoundIds.DialogueTypeGameplay สำหรับเสียงของฉาก Gameplay")]
    public string typingSoundId = SoundIds.DialogueType;
    [Tooltip("ให้เสียงดัง 1 ครั้งต่อกี่ตัวอักษร (ใส่ 1 = ดังทุกตัว) ตั้งสูงขึ้นถ้าฟังแล้วรัวเกินไป")]
    [Min(1)] public int typeSoundEveryNChars = 2;
    [Tooltip("รหัสเสียงตอน NPC เดินเข้าฉาก เปลี่ยนได้ถ้าแต่ละฉากต้องการเสียงเปิดตัวคนละแบบ")]
    public string arriveSoundId = SoundIds.DialogueArrive;
    [Tooltip("รหัสเสียงตอน NPC เดินออกจากฉาก เปลี่ยนได้ถ้าแต่ละฉากต้องการเสียงจบคนละแบบ")]
    public string leaveSoundId = SoundIds.DialogueLeave;

    [Header("Debug")]
    [Tooltip("ปุ่มสำหรับข้าม Intro ทั้งหมดทันที (ใช้ตอนทดสอบเกม)")]
    public Button skipIntroButton;

    [Header("Cat Theft Tutorial")]
    [Tooltip("เปิดใช้แผงอธิบายระบบแมวขโมยสำหรับเลเวลนี้หรือไม่")]
    public bool enableCatTheftTutorial = true;
    [Tooltip("แผงอธิบายระบบแมวขโมยที่จะเปิดครั้งแรกก่อน Intro dialogue")]
    public GameObject catTheftTutorialPanel;
    [Tooltip("ปุ่ม OK ของแผงอธิบายระบบแมวขโมย")]
    public Button catTheftTutorialOkButton;
    [Tooltip("ระยะเวลาที่ใช้ตอนแสดง/ซ่อนแผงอธิบายระบบแมวขโมย")]
    public float catTheftTutorialAnimationDuration = 0.25f;
    [Tooltip("ขนาดเริ่มต้นของแผงก่อนเด้งเข้ามา")]
    [Range(0.5f, 1f)] public float catTheftTutorialAnimationStartScale = 0.9f;

    [Header("✨ Visual Polish")]
    [Tooltip("ไอคอน/ลูกศรเตือนให้คลิกไปหน้าถัดไป (ไม่บังคับ) จะเด้งขึ้นลงเฉพาะตอนรอผู้เล่นคลิก")]
    public Transform continueIndicator;
    [Tooltip("ความสูงของการเด้งไอคอน continueIndicator")]
    public float indicatorBounceHeight = 8f;
    [Tooltip("ความเร็วของการเด้งไอคอน continueIndicator")]
    public float indicatorBounceSpeed = 3f;
    [Tooltip("ให้กรอบข้อความเด้งเล็กน้อยทุกครั้งที่เปลี่ยนหน้าข้อความ เพื่อความมีชีวิตชีวา")]
    public bool punchOnNewPage = true;

    [Header("🚦 Ready / Go")]
    [Tooltip("ปิดการแสดง Ready?/Go!!! ทั้งหมด (ปิดไว้เพราะรบกวนตอนทดสอบใน Unity)")]
    public bool enableReadyGoSequence = false;
    [Tooltip("Root object ของไอคอน Ready?/Go!!! (จะถูกซ่อนไว้จนกว่า Intro dialogue จะจบ)")]
    public Transform readyGoRoot;
    [Tooltip("Image เดิมที่ใช้สลับ sprite ระหว่าง Ready กับ Go (ใช้เป็น fallback ถ้าไม่ได้ใส่ Ready Image และ Go Image)")]
    public Image readyGoImage;
    public Sprite readySprite;
    public Sprite goSprite;
    [Tooltip("Image ของ Ready ที่ลากมาใส่โดยตรงได้ เหมือน Day Complete Banner")]
    public RectTransform readyBanner;
    [Tooltip("Image ของ Go ที่ลากมาใส่โดยตรงได้ เหมือน Day Complete Banner")]
    public RectTransform goBanner;
    [Tooltip("เวลาที่ค้างไอคอน Ready? ไว้บนจอ ก่อนเปลี่ยนเป็น Go!!!")]
    public float readyHoldDuration = 0.7f;
    [Tooltip("เวลาที่ค้างไอคอน Go!!! ไว้บนจอ ก่อนเริ่มเกม")]
    public float goHoldDuration = 0.6f;
    [Tooltip("มุมเอียงตอนเด้งเข้ามา (องศา) จะคลายกลับเป็น 0 ระหว่างเด้ง ใส่ 0 ถ้าไม่ต้องการหมุน")]
    public float readyGoTiltAngle = 8f;
    [Tooltip("ความแรงบีบ/ยืด (squash-and-stretch) ตอน 'ลงจอด' หลังเด้งเข้ามาเต็มขนาด")]
    public float readyGoLandingSquashAmount = 0.18f;
    [Tooltip("ระยะเวลาบีบ/ยืดตอนลงจอด")]
    public float readyGoLandingSquashDuration = 0.18f;
    [Tooltip("มุมหมุนตอนหดหายไป (องศา) ให้ความรู้สึกถูกดูดหายแทนที่จะหดตรงๆ")]
    public float readyGoPopOutSpin = 20f;

    private string[] dialogPages;
    private Vector3 savedBubbleScale;
    private CanvasGroup dialogCanvasGroup;
    private Vector3 indicatorBasePos;
    private Coroutine indicatorRoutine;
    private Vector3 savedReadyGoScale;
    private CanvasGroup readyGoCanvasGroup;
    private Coroutine talkingRoutine;
    private SceneShutterTransition shutterTransition;
    private CanvasGroup catTheftTutorialCanvasGroup;
    private Vector3 catTheftTutorialBaseScale = Vector3.one;
    private Coroutine catTheftTutorialAnimation;
    private bool introFlowStarted;
    private Coroutine walkInCoroutine;
    private bool walkInStarted;
    private bool walkInCompleted;
    private Vector2 upperBlackBarTargetPosition;
    private Vector2 lowerBlackBarTargetPosition;
    private Coroutine blackBarAnimation;

    // ตัวแปรเช็คสถานะการคลิก
    private bool isTyping = false;
    private bool skipTyping = false;
    private bool isWaitingForNextPage = false;

    private IEnumerator Start()
    {
        yield return LoadDialoguePages();

        savedBubbleScale = dialogBubble.localScale;
        dialogBubble.localScale = Vector3.zero;
        transform.position = startPoint.position;

        // เผื่อไม่ได้ผูก npcSpriteRenderer ไว้ใน Inspector ให้ลองหาจากตัวเองก่อน
        if (npcSpriteRenderer == null)
        {
            npcSpriteRenderer = GetComponent<SpriteRenderer>();
        }
        // จำสไปรท์เดิมไว้เป็นหน้าปกติ (ตอนไม่ได้พูด) ถ้ายังไม่ได้ตั้งค่าไว้เอง
        if (idleSprite == null && npcSpriteRenderer != null)
        {
            idleSprite = npcSpriteRenderer.sprite;
        }

        dialogCanvasGroup = dialogBubble.GetComponent<CanvasGroup>();
        if (dialogCanvasGroup == null)
        {
            dialogCanvasGroup = dialogBubble.gameObject.AddComponent<CanvasGroup>();
        }
        dialogCanvasGroup.alpha = 0f;

        PrepareBlackBars();

        if (continueIndicator != null)
        {
            indicatorBasePos = continueIndicator.localPosition;
            continueIndicator.gameObject.SetActive(false);
        }

        if (readyGoRoot != null)
        {
            if (readyBanner == null && goBanner == null)
            {
                savedReadyGoScale = readyGoRoot.localScale;
                readyGoRoot.localScale = Vector3.zero;
                readyGoCanvasGroup = readyGoRoot.GetComponent<CanvasGroup>();
                if (readyGoCanvasGroup == null)
                {
                    readyGoCanvasGroup = readyGoRoot.gameObject.AddComponent<CanvasGroup>();
                }
                readyGoCanvasGroup.alpha = 0f;
                readyGoRoot.gameObject.SetActive(false);
            }
        }

        PrepareReadyGoBanner(readyBanner);
        PrepareReadyGoBanner(goBanner);

        // ป้องกัน Image โชว์เป็นกล่องสีขาวตอนยังไม่ได้ผูก sprite (เผื่อ build ที่ไม่มี OnValidate ทำงาน)
        if (readyGoImage != null)
        {
            readyGoImage.enabled = (readyBanner != null && goBanner != null) ||
                readySprite != null || goSprite != null;
        }

        if (skipIntroButton != null)
        {
            skipIntroButton.onClick.AddListener(SkipIntro);
        }

        if (!enableCatTheftTutorial)
        {
            StartWalkIn();
        }

        if (catTheftTutorialPanel != null)
        {
            catTheftTutorialCanvasGroup = catTheftTutorialPanel.GetComponent<CanvasGroup>();
            if (catTheftTutorialCanvasGroup == null)
            {
                catTheftTutorialCanvasGroup = catTheftTutorialPanel.AddComponent<CanvasGroup>();
            }

            catTheftTutorialBaseScale = catTheftTutorialPanel.transform.localScale;
            if (catTheftTutorialBaseScale == Vector3.zero)
            {
                catTheftTutorialBaseScale = Vector3.one;
            }

            catTheftTutorialCanvasGroup.alpha = 0f;
            catTheftTutorialCanvasGroup.interactable = false;
            catTheftTutorialCanvasGroup.blocksRaycasts = false;
            catTheftTutorialPanel.SetActive(false);
        }

        shutterTransition = FindAnyObjectByType<SceneShutterTransition>(FindObjectsInactive.Include);
        if (shutterTransition != null && !shutterTransition.HasFinishedOpening)
        {
            shutterTransition.OpeningFinished += OnShutterOpeningFinished;
        }
        else
        {
            ShowTutorialOrStartIntro();
        }
    }

    private IEnumerator LoadDialoguePages()
    {
        string introMessage = null;

        if (GameManager.Instance != null)
        {
            StageConfig currentStage = GameManager.Instance.GetCurrentStage();
            if (currentStage != null)
            {
                if (currentStage.introMessageLocalized != null &&
                    !currentStage.introMessageLocalized.IsEmpty)
                {
                    AsyncOperationHandle<string> localizedHandle =
                        currentStage.introMessageLocalized.GetLocalizedStringAsync();
                    yield return localizedHandle;

                    if (localizedHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        introMessage = localizedHandle.Result;
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[IntroDialogue] Failed to load the localized intro message. Falling back to Intro Message.",
                            this);
                    }

                    Addressables.Release(localizedHandle);
                }

                if (string.IsNullOrWhiteSpace(introMessage))
                {
                    introMessage = currentStage.introMessage;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(introMessage))
        {
            introMessage = "ภารกิจวันนี้... เอ่อ... ฉันลืมบทน่ะ!";
        }

        dialogPages = introMessage.Split('\n');
    }

    // ป้องกัน Image โชว์เป็นกล่องสีขาว (ค่า default ของ Unity ตอนไม่ได้ใส่ sprite) ตอนยังไม่ได้ผูก readySprite/goSprite
    private void OnValidate()
    {
        if (readyGoImage != null)
        {
            readyGoImage.enabled = readySprite != null || goSprite != null;
        }
    }

    private void OnShutterOpeningFinished()
    {
        if (shutterTransition != null)
        {
            shutterTransition.OpeningFinished -= OnShutterOpeningFinished;
        }

        ShowTutorialOrStartIntro();
    }

    private void ShowTutorialOrStartIntro()
    {
        if (introFlowStarted)
        {
            return;
        }

        introFlowStarted = true;

        if (ShouldShowCatTheftTutorial())
        {
            ShowCatTheftTutorial();
        }
        else
        {
            StartWalkIn();
            StartCoroutine(IntroSequence());
        }
    }

    // เรียกจากปุ่ม Skip (Debug) เพื่อข้าม Intro ทั้งหมดทันทีและเริ่มเกมเลย
    public void SkipIntro()
    {
        StopAllCoroutines();
        CameraController cameraController = FindAnyObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.EndIntroFocus();
        }

        if (shutterTransition != null)
        {
            shutterTransition.OpeningFinished -= OnShutterOpeningFinished;
        }
        StartCoroutine(CloseCatTheftTutorial(false));

        DialogueFinished?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (shutterTransition != null)
        {
            shutterTransition.OpeningFinished -= OnShutterOpeningFinished;
        }
    }

    private bool ShouldShowCatTheftTutorial()
    {
        if (!enableCatTheftTutorial)
        {
            return false;
        }

        if (catTheftTutorialPanel == null)
        {
            Debug.LogWarning("[IntroDialogue] Cat Theft Tutorial Panel is not assigned. Skipping tutorial.", this);
            return false;
        }

        if (catTheftTutorialOkButton == null)
        {
            Debug.LogError("[IntroDialogue] Cat Theft Tutorial Ok Button is not assigned. The tutorial panel cannot be dismissed.", this);
            return false;
        }

        return true;
    }

    private void ShowCatTheftTutorial()
    {
        if (catTheftTutorialAnimation != null)
        {
            StopCoroutine(catTheftTutorialAnimation);
        }

        catTheftTutorialOkButton.onClick.AddListener(ConfirmCatTheftTutorial);
        catTheftTutorialPanel.SetActive(true);
        SoundManager.Instance.PlaySFX(SoundIds.PopUpAppear);
        catTheftTutorialAnimation = StartCoroutine(AnimateCatTheftTutorial(true));
        Debug.Log("[IntroDialogue] Showing Cat Theft Tutorial.", this);
    }

    private void ConfirmCatTheftTutorial()
    {
        if (catTheftTutorialAnimation != null)
        {
            return;
        }

        StartCoroutine(CloseCatTheftTutorialAndStartIntro());
    }

    private IEnumerator CloseCatTheftTutorialAndStartIntro()
    {
        yield return CloseCatTheftTutorial(true);
        StartWalkIn();
        StartCoroutine(IntroSequence());
    }

    private void StartWalkIn()
    {
        if (walkInStarted)
        {
            return;
        }

        walkInStarted = true;
        StartCoroutine(ShowBlackBarsAfterDelay());
        walkInCoroutine = StartCoroutine(WalkIntoPosition());
    }

    private IEnumerator ShowBlackBarsAfterDelay()
    {
        if (blackBarStartDelay > 0f)
        {
            yield return new WaitForSeconds(blackBarStartDelay);
        }

        ShowBlackBars();
    }

    private void PrepareBlackBars()
    {
        if (upperBlackBar != null)
        {
            upperBlackBar.gameObject.SetActive(true);
            upperBlackBarTargetPosition = upperBlackBar.anchoredPosition;
            upperBlackBar.anchoredPosition = upperBlackBarTargetPosition +
                Vector2.up * Mathf.Abs(blackBarMoveDistance);
        }

        if (lowerBlackBar != null)
        {
            lowerBlackBar.gameObject.SetActive(true);
            lowerBlackBarTargetPosition = lowerBlackBar.anchoredPosition;
            lowerBlackBar.anchoredPosition = lowerBlackBarTargetPosition +
                Vector2.down * Mathf.Abs(blackBarMoveDistance);
        }
    }

    private void ShowBlackBars()
    {
        if (upperBlackBar == null && lowerBlackBar == null)
        {
            return;
        }

        if (blackBarAnimation != null)
        {
            StopCoroutine(blackBarAnimation);
        }

        blackBarAnimation = StartCoroutine(AnimateBlackBars(true));
    }

    private IEnumerator HideBlackBars()
    {
        if (upperBlackBar == null && lowerBlackBar == null)
        {
            yield break;
        }

        if (blackBarAnimation != null)
        {
            StopCoroutine(blackBarAnimation);
            blackBarAnimation = null;
        }

        yield return AnimateBlackBars(false);
    }

    private IEnumerator AnimateBlackBars(bool show)
    {
        Vector2 upperStart = upperBlackBar != null
            ? upperBlackBar.anchoredPosition
            : Vector2.zero;
        Vector2 lowerStart = lowerBlackBar != null
            ? lowerBlackBar.anchoredPosition
            : Vector2.zero;
        Vector2 upperEnd = show
            ? upperBlackBarTargetPosition
            : upperBlackBarTargetPosition + Vector2.up * Mathf.Abs(blackBarMoveDistance);
        Vector2 lowerEnd = show
            ? lowerBlackBarTargetPosition
            : lowerBlackBarTargetPosition + Vector2.down * Mathf.Abs(blackBarMoveDistance);
        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, blackBarAnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            if (upperBlackBar != null)
            {
                upperBlackBar.anchoredPosition = Vector2.LerpUnclamped(upperStart, upperEnd, t);
            }

            if (lowerBlackBar != null)
            {
                lowerBlackBar.anchoredPosition = Vector2.LerpUnclamped(lowerStart, lowerEnd, t);
            }

            yield return null;
        }

        if (upperBlackBar != null)
        {
            upperBlackBar.anchoredPosition = upperEnd;
        }

        if (lowerBlackBar != null)
        {
            lowerBlackBar.anchoredPosition = lowerEnd;
        }

        blackBarAnimation = null;
    }

    private IEnumerator WalkIntoPosition()
    {
        if (preWalkDelay > 0f)
        {
            yield return new WaitForSeconds(preWalkDelay);
        }

        // ยิงที่ stopPoint เพราะถ้าเสียงตั้ง Is 3D ไว้ จุดเกิดอยู่นอกจอจะไกลจนแทบไม่ได้ยิน
        SoundManager.Instance.PlaySFX(arriveSoundId, stopPoint.position);
        StartCoroutine(BeginIntroCameraAfterDelay());
        yield return StartCoroutine(WalkTo(stopPoint.position));
        walkInCompleted = true;
        walkInCoroutine = null;
    }

    private IEnumerator BeginIntroCameraAfterDelay()
    {
        if (introCameraFollowDelay > 0f)
        {
            yield return new WaitForSeconds(introCameraFollowDelay);
        }

        CameraController cameraController = FindAnyObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.BeginIntroFocus(transform);
        }
    }

    private IEnumerator CloseCatTheftTutorial(bool animate)
    {
        if (catTheftTutorialOkButton != null)
        {
            catTheftTutorialOkButton.onClick.RemoveListener(ConfirmCatTheftTutorial);
        }

        if (catTheftTutorialPanel == null)
        {
            yield break;
        }

        if (catTheftTutorialAnimation != null)
        {
            StopCoroutine(catTheftTutorialAnimation);
            catTheftTutorialAnimation = null;
        }

        if (animate)
        {
            yield return AnimateCatTheftTutorial(false);
        }
        else
        {
            SetCatTheftTutorialVisualState(0f, catTheftTutorialAnimationStartScale);
            catTheftTutorialPanel.SetActive(false);
        }
    }

    private IEnumerator AnimateCatTheftTutorial(bool show)
    {
        if (catTheftTutorialPanel == null || catTheftTutorialCanvasGroup == null)
            yield break;

        if (show)
        {
            SetCatTheftTutorialVisualState(0f, catTheftTutorialAnimationStartScale);
            catTheftTutorialCanvasGroup.interactable = false;
            catTheftTutorialCanvasGroup.blocksRaycasts = false;
        }

        float startAlpha = show ? 0f : catTheftTutorialCanvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        float startScale = show ? catTheftTutorialAnimationStartScale : 1f;
        float endScale = show ? 1f : catTheftTutorialAnimationStartScale;
        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, catTheftTutorialAnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            SetCatTheftTutorialVisualState(
                Mathf.Lerp(startAlpha, endAlpha, t),
                Mathf.Lerp(startScale, endScale, t));
            yield return null;
        }

        SetCatTheftTutorialVisualState(endAlpha, endScale);
        catTheftTutorialAnimation = null;

        if (show)
        {
            catTheftTutorialCanvasGroup.interactable = true;
            catTheftTutorialCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            catTheftTutorialPanel.SetActive(false);
        }
    }

    private void SetCatTheftTutorialVisualState(float alpha, float scale)
    {
        if (catTheftTutorialCanvasGroup != null)
        {
            catTheftTutorialCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        if (catTheftTutorialPanel != null)
        {
            catTheftTutorialPanel.transform.localScale = catTheftTutorialBaseScale * scale;
        }
    }

    private void Update()
    {
        // ระบบรับการคลิกเมาส์ / กด Space
        if (WasAdvancePressedThisFrame())
        {
            if (isTyping)
            {
                // ถ้ากำลังพิมพ์อยู่ ให้ข้ามไปแสดงข้อความเต็มๆ ทันที
                skipTyping = true;
            }
            else if (isWaitingForNextPage)
            {
                // ถ้าพิมพ์เสร็จแล้ว ให้กดเพื่อไปหน้าถัดไป
                isWaitingForNextPage = false;
            }
        }
    }

    // คลิกซ้าย หรือกด Space ก็ได้ ทำงานเหมือนกันทุกอย่าง (ข้ามการพิมพ์ / ไปหน้าถัดไป)
    private static bool WasAdvancePressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        return WasSpacePressedThisFrame();
    }

    private static bool WasSpacePressedThisFrame()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private IEnumerator IntroSequence()
    {
        // เดินเข้ามาเริ่มทันทีตั้งแต่เปิดฉาก แม้ shutter door จะยังเปิดไม่สุด
        if (!walkInCompleted && walkInCoroutine != null)
        {
            yield return walkInCoroutine;
        }

        // 2. เด้งกรอบข้อความ (ตอนแรกจะยังไม่มีข้อความ)
        dialogText.text = "";
        yield return StartCoroutine(PopInAnimation(dialogBubble));

        // 3. เริ่มลูปแสดงข้อความทีละหน้า
        for (int i = 0; i < dialogPages.Length; i++)
        {
            string sentence = dialogPages[i].Trim();
            if (string.IsNullOrEmpty(sentence)) continue;

            // เด้งกรอบเบาๆ ทุกครั้งที่ขึ้นหน้าข้อความใหม่ (ยกเว้นหน้าแรกที่เพิ่งเด้งเข้ามา)
            if (punchOnNewPage && i > 0)
            {
                yield return StartCoroutine(PunchAnimation(dialogBubble));
            }

            // ✨ เริ่มกระบวนการพิมพ์ทีละตัวอักษร
            isTyping = true;
            skipTyping = false;
            dialogText.text = "";
            SetIndicatorVisible(false);
            StartTalkingAnimation(); // ✨ เริ่มขยับปาก NPC ตอนกำลังพิมพ์ข้อความ

            int charsSinceBlip = 0;

            foreach (char letter in sentence.ToCharArray())
            {
                if (skipTyping) // ถ้าผู้เล่นกดคลิกข้าม
                {
                    dialogText.text = sentence; // โชว์ข้อความทั้งหมดทันที
                    break;
                }

                dialogText.text += letter;
                PlayTypingBlip(letter, ref charsSinceBlip);
                yield return new WaitForSeconds(typeSpeed); // รอเวลาตามความเร็วที่ตั้งไว้
            }

            // พิมพ์จบแล้ว หยุดขยับปาก กลับเป็นหน้านิ่ง แล้วรอผู้เล่นคลิกเพื่อไปหน้าต่อไป
            isTyping = false;
            StopTalkingAnimation();
            isWaitingForNextPage = true;
            SetIndicatorVisible(true);

            while (isWaitingForNextPage)
            {
                yield return null;
            }

            SetIndicatorVisible(false);
        }

        // 4. หดกรอบหายไป
        yield return StartCoroutine(PopOutAnimation(dialogBubble));

        // 5. เดินกลับออกไปนอกจอ
        StartCoroutine(HideBlackBars());
        SoundManager.Instance.PlaySFX(leaveSoundId, transform.position);
        StartCoroutine(EndIntroCameraAfterDelay());
        yield return StartCoroutine(WalkTo(startPoint.position));

        // 5.5 โชว์ Ready? แล้วตามด้วย Go!!! เป็นการนับถอยหลังก่อนเริ่มเกมจริง
        yield return StartCoroutine(PlayReadyGoSequence());

        // 6. สั่งให้เกมเริ่ม และทำลายตัวเองทิ้ง
        DialogueFinished?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        }
        Destroy(gameObject);
    }

    private IEnumerator EndIntroCameraAfterDelay()
    {
        if (introCameraReturnDelay > 0f)
        {
            yield return new WaitForSeconds(introCameraReturnDelay);
        }

        CameraController cameraController = FindAnyObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.EndIntroFocus();
        }
    }

    // ==========================================
    // 🎨 ฟังก์ชันช่วยทำแอนิเมชันตอนเด้ง
    // ==========================================

    // เดินไปยัง destination โดยชะลอความเร็วตอนใกล้ถึง (ease-out) พร้อมเด้งขึ้นลงตอนเดิน
    private IEnumerator WalkTo(Vector3 destination)
    {
        // แยกตำแหน่งจริง (logical) ออกจากตำแหน่งที่แสดงผล (มีเด้งบวกเข้าไป)
        // เพื่อไม่ให้การเด้งขึ้นลงไปกวนเงื่อนไขระยะทางที่ใช้เช็คว่าถึงจุดหมายหรือยัง
        Vector3 logicalPos = transform.position;
        float totalDistance = Vector3.Distance(logicalPos, destination);
        float easeZone = Mathf.Max(totalDistance * 0.3f, 0.01f);
        float bobTimer = 0f;

        while (Vector3.Distance(logicalPos, destination) > 0.01f)
        {
            float remaining = Vector3.Distance(logicalPos, destination);
            float easeFactor = Mathf.Clamp01(remaining / easeZone);
            float currentSpeed = walkSpeed * Mathf.Lerp(0.25f, 1f, easeFactor);
            logicalPos = Vector3.MoveTowards(logicalPos, destination, currentSpeed * Time.deltaTime);

            bobTimer += Time.deltaTime * walkBobSpeed;
            float bobOffset = Mathf.Abs(Mathf.Sin(bobTimer)) * walkBobHeight;
            transform.position = logicalPos + Vector3.up * bobOffset;

            yield return null;
        }
        transform.position = destination;
    }

    private IEnumerator PopInAnimation(Transform target)
    {
        Vector3 originalScale = savedBubbleScale;
        target.localScale = Vector3.zero;
        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            target.localScale = originalScale * scaleFactor;
            if (dialogCanvasGroup != null) dialogCanvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
            yield return null;
        }
        target.localScale = originalScale;
        if (dialogCanvasGroup != null) dialogCanvasGroup.alpha = 1f;
    }

    private IEnumerator PopOutAnimation(Transform target)
    {
        float duration = 0.2f;
        float time = 0f;
        Vector3 startScale = target.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = Mathf.Lerp(1f, 0f, t * t);
            target.localScale = startScale * scaleFactor;
            if (dialogCanvasGroup != null) dialogCanvasGroup.alpha = 1f - t;
            yield return null;
        }
        target.localScale = Vector3.zero;
        if (dialogCanvasGroup != null) dialogCanvasGroup.alpha = 0f;
    }

    // เล่นเสียงพิมพ์ 1 ครั้งต่อทุก typeSoundEveryNChars ตัวอักษรที่ "มองเห็นได้จริง"
    // charsSinceBlip ส่งเป็น ref เพราะต้องนับต่อเนื่องตลอดทั้งหน้าข้อความ ไม่ใช่รีเซ็ตทุกตัวอักษร
    private void PlayTypingBlip(char letter, ref int charsSinceBlip)
    {
        if (!playTypingSound) return;

        // เว้นวรรค/ขึ้นบรรทัด กับสระ-วรรณยุกต์ไทยที่ลอยอยู่บน-ล่างพยัญชนะ ไม่ต้องมีเสียง
        // เพราะผู้เล่นไม่เห็นข้อความยาวขึ้น ถ้าให้ดังด้วยจะฟังเหมือนเสียงรัวไม่ตรงกับตัวหนังสือที่โผล่มา
        if (char.IsWhiteSpace(letter) || IsThaiCombiningMark(letter)) return;

        charsSinceBlip++;
        if (charsSinceBlip < Mathf.Max(1, typeSoundEveryNChars)) return;

        charsSinceBlip = 0;
        SoundManager.Instance.PlaySFX(typingSoundId, transform.position);
    }

    // สระบน/สระล่าง/ไม้ไต่คู้/วรรณยุกต์/การันต์ ที่เขียนซ้อนกับพยัญชนะตัวหน้า ไม่กินความกว้างเพิ่ม
    private static bool IsThaiCombiningMark(char c)
    {
        // เทียบด้วยรหัสยูนิโค้ดแทนตัวอักษรตรง ๆ เผื่อไฟล์โดนบันทึกทับด้วยการเข้ารหัสอื่นแล้วตัวอักษรเพี้ยน
        int code = c;

        return code == 0x0E31                        // ไม้หันอากาศ
            || (code >= 0x0E34 && code <= 0x0E3A)    // สระอิ-อื, สระอุ-อู, พินทุ
            || (code >= 0x0E47 && code <= 0x0E4E);   // ไม้ไต่คู้, วรรณยุกต์, การันต์
    }

    // เริ่มวนสลับสไปรท์ตอนพูด (ปากขยับ) ระหว่างที่ข้อความกำลังพิมพ์อยู่
    private void StartTalkingAnimation()
    {
        if (npcSpriteRenderer == null || talkingSprites == null || talkingSprites.Length == 0) return;

        StopTalkingAnimation(); // กันไว้เผื่อมีลูปเก่าค้างอยู่ ก่อนเริ่มลูปใหม่
        talkingRoutine = StartCoroutine(TalkingAnimationLoop());
    }

    // หยุดวนสลับสไปรท์ แล้วกลับไปที่หน้านิ่ง (idleSprite)
    private void StopTalkingAnimation()
    {
        if (talkingRoutine != null)
        {
            StopCoroutine(talkingRoutine);
            talkingRoutine = null;
        }
        if (npcSpriteRenderer != null && idleSprite != null)
        {
            npcSpriteRenderer.sprite = idleSprite;
        }
    }

    private IEnumerator TalkingAnimationLoop()
    {
        int frameIndex = 0;
        while (true)
        {
            npcSpriteRenderer.sprite = talkingSprites[frameIndex % talkingSprites.Length];
            frameIndex++;
            yield return new WaitForSeconds(talkFrameInterval);
        }
    }

    // เด้งกรอบข้อความเบาๆ (punch) ตอนขึ้นหน้าใหม่ ให้รู้สึกมีชีวิตชีวา
    private IEnumerator PunchAnimation(Transform target)
    {
        Vector3 originalScale = savedBubbleScale;
        float duration = 0.15f;
        float time = 0f;
        float punchStrength = 0.08f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float bump = Mathf.Sin(t * Mathf.PI) * punchStrength;
            target.localScale = originalScale * (1f + bump);
            yield return null;
        }
        target.localScale = originalScale;
    }

    // เล่นลำดับ Ready? -> Go!!! ก่อนเข้าเกมจริง (ถ้าไม่ได้ผูก readyGoRoot ไว้ก็ข้ามไปเลย)
    private IEnumerator PlayReadyGoSequence()
    {
        if (!enableReadyGoSequence) yield break;

        RectTransform readyTarget = readyBanner != null
            ? readyBanner
            : readyGoRoot as RectTransform;
        RectTransform goTarget = goBanner != null
            ? goBanner
            : readyGoRoot as RectTransform;

        if (readyTarget == null || goTarget == null) yield break;

        if (readyGoRoot != null) readyGoRoot.gameObject.SetActive(true);

        CanvasGroup readyCanvasGroup = GetReadyGoCanvasGroup(readyTarget);
        CanvasGroup goCanvasGroup = GetReadyGoCanvasGroup(goTarget);
        Vector3 readyScale = readyTarget.localScale;
        Vector3 goScale = goTarget.localScale;

        readyTarget.gameObject.SetActive(true);
        if (readyBanner == null && readyGoImage != null) readyGoImage.sprite = readySprite;
        // ยิงเสียงพร้อมกับที่ไอคอนเริ่มเด้งเข้ามา จะได้ดังตรงกับที่ผู้เล่นเห็น
        SoundManager.Instance.PlaySFX(SoundIds.UiReady);
        yield return StartCoroutine(ScalePopIn(readyTarget, readyScale, readyCanvasGroup));
        yield return new WaitForSeconds(readyHoldDuration);
        yield return StartCoroutine(ScalePopOut(readyTarget, readyCanvasGroup));

        readyTarget.gameObject.SetActive(false);
        goTarget.gameObject.SetActive(true);
        if (goBanner == null && readyGoImage != null) readyGoImage.sprite = goSprite;
        SoundManager.Instance.PlaySFX(SoundIds.UiGo);
        yield return StartCoroutine(ScalePopIn(goTarget, goScale, goCanvasGroup));
        yield return new WaitForSeconds(goHoldDuration);
        yield return StartCoroutine(ScalePopOut(goTarget, goCanvasGroup));

        goTarget.gameObject.SetActive(false);
        if (readyBanner != null || goBanner != null)
        {
            if (readyGoRoot != null) readyGoRoot.gameObject.SetActive(false);
        }
    }

    private void PrepareReadyGoBanner(RectTransform banner)
    {
        if (banner == null || banner == readyGoRoot) return;

        CanvasGroup canvasGroup = banner.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        banner.gameObject.SetActive(false);
    }

    private CanvasGroup GetReadyGoCanvasGroup(RectTransform target)
    {
        if (target == readyGoRoot) return readyGoCanvasGroup;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    // เด้งเข้าแบบ ease-out-back (overshoot) พร้อมหมุนคลายมุมเอียงและเฟดอัลฟ่า
    // จบด้วยการบีบ/ยืดตอน "ลงจอด" ให้รู้สึกกระแทกหนักแน่นแทนที่จะหยุดนิ่งดื้อๆ
    private IEnumerator ScalePopIn(Transform target, Vector3 originalScale, CanvasGroup canvasGroup)
    {
        target.localScale = Vector3.zero;
        target.localRotation = Quaternion.Euler(0f, 0f, readyGoTiltAngle);
        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            target.localScale = originalScale * scaleFactor;
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(readyGoTiltAngle, 0f, Mathf.Clamp01(t * 1.3f)));
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
            yield return null;
        }
        target.localScale = originalScale;
        target.localRotation = Quaternion.identity;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        yield return StartCoroutine(PlayLandingSquash(target, originalScale));
    }

    // เด้งบีบ/ยืดสั้นๆ (squash-and-stretch) ตอนลงจอดหลังขยายเต็มขนาด สลายแรงเด้งลงเรื่อยๆ จนนิ่ง
    private IEnumerator PlayLandingSquash(Transform target, Vector3 baseScale)
    {
        if (readyGoLandingSquashAmount <= 0f) yield break;

        float time = 0f;
        while (time < readyGoLandingSquashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / readyGoLandingSquashDuration);
            float squash = Mathf.Sin(t * Mathf.PI) * readyGoLandingSquashAmount * (1f - t);
            target.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z);
            yield return null;
        }
        target.localScale = baseScale;
    }

    // ยุบตัวเบาๆ ก่อน (anticipation) แล้วหดหายไปพร้อมหมุนวนและเฟดอัลฟ่า
    // ให้ความรู้สึกถูกดูดหายไปแบบมีแรงส่ง แทนที่จะหดตรงๆ เข้าจุดศูนย์กลาง
    private IEnumerator ScalePopOut(Transform target, CanvasGroup canvasGroup)
    {
        Vector3 startScale = target.localScale;

        float punchDuration = 0.08f;
        float punchTime = 0f;
        while (punchTime < punchDuration)
        {
            punchTime += Time.deltaTime;
            float t = punchTime / punchDuration;
            float squash = Mathf.Sin(t * Mathf.PI) * 0.15f;
            target.localScale = new Vector3(startScale.x * (1f + squash), startScale.y * (1f - squash), startScale.z);
            yield return null;
        }

        float duration = 0.2f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = Mathf.Lerp(1f, 0f, t * t);
            target.localScale = startScale * scaleFactor;
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, readyGoPopOutSpin, t));
            if (canvasGroup != null) canvasGroup.alpha = 1f - t;
            yield return null;
        }
        target.localScale = Vector3.zero;
        target.localRotation = Quaternion.identity;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    // แสดง/ซ่อน continueIndicator และเริ่ม-หยุดแอนิเมชันเด้งของมัน
    private void SetIndicatorVisible(bool visible)
    {
        if (continueIndicator == null) return;

        if (visible)
        {
            continueIndicator.gameObject.SetActive(true);
            if (indicatorRoutine == null)
            {
                indicatorRoutine = StartCoroutine(IndicatorBounceLoop());
            }
        }
        else
        {
            if (indicatorRoutine != null)
            {
                StopCoroutine(indicatorRoutine);
                indicatorRoutine = null;
            }
            continueIndicator.localPosition = indicatorBasePos;
            continueIndicator.gameObject.SetActive(false);
        }
    }

    private IEnumerator IndicatorBounceLoop()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * indicatorBounceSpeed;
            float offset = Mathf.Abs(Mathf.Sin(t)) * indicatorBounceHeight;
            continueIndicator.localPosition = indicatorBasePos + Vector3.up * offset;
            yield return null;
        }
    }
}
