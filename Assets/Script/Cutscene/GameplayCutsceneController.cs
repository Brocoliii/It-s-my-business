using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GameplayCutsceneController : MonoBehaviour
{
    [Header("Cutscene")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Video played when first entering the gameplay scene. Assign this to VideoPlayer.clip.")]
    [SerializeField] private UnityEngine.Video.VideoClip firstEntryVideoClip;
    [Tooltip("Different video played after the player selects the correct suspect.")]
    [SerializeField] private UnityEngine.Video.VideoClip endingVideoClip;
    [Tooltip("Optional object containing the video RawImage or other cutscene visuals.")]
    [SerializeField] private GameObject videoVisual;
    [SerializeField] private TMP_Text continuePrompt;
    [SerializeField] private float promptBlinkSpeed = 1.5f;
    [SerializeField] private bool playOnStart = true;

    private bool waitingForContinue;
    private bool hasContinued;
    private bool isPlaying;
    private bool timeScaleWasChanged;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[GameplayCutsceneController] No VideoPlayer is assigned or found.", this);
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        SoundManager.Instance.RegisterVideoPlayer(videoPlayer);
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;

        if (continuePrompt == null)
        {
            continuePrompt = CreateFallbackContinuePrompt();
        }

        SetContinuePromptVisible(false);
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartCoroutine(PlayCutsceneAndWait(ResolveFirstEntryVideo()));
        }
    }

    public IEnumerator PlayCutsceneAndWait()
    {
        return PlayCutsceneAndWait(ResolveFirstEntryVideo());
    }

    public IEnumerator PlayEndingCutsceneAndWait()
    {
        return PlayCutsceneAndWait(endingVideoClip);
    }

    private UnityEngine.Video.VideoClip ResolveFirstEntryVideo()
    {
        return firstEntryVideoClip != null
            ? firstEntryVideoClip
            : videoPlayer != null ? videoPlayer.clip : null;
    }

    private IEnumerator PlayCutsceneAndWait(UnityEngine.Video.VideoClip clip)
    {
        if (videoPlayer == null || isPlaying)
        {
            yield break;
        }

        if (clip == null)
        {
            Debug.LogError("[GameplayCutsceneController] No video clip is assigned.", this);
            yield break;
        }

        isPlaying = true;
        videoPlayer.clip = clip;
        waitingForContinue = false;
        hasContinued = false;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        timeScaleWasChanged = true;
        SetContinuePromptVisible(false);

        // Keep the old clip's last rendered frame hidden while the new clip prepares.
        // Otherwise the first-entry video can flash for a frame before the ending video.
        if (videoVisual != null)
        {
            videoVisual.SetActive(false);
        }

        videoPlayer.enabled = true;
        videoPlayer.Stop();
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared && !hasContinued)
        {
            yield return null;
        }

        if (!hasContinued)
        {
            SoundManager.Instance.RegisterVideoPlayer(videoPlayer);
            videoPlayer.Play();

            // VideoPlayer renders asynchronously. Wait one frame before revealing
            // the surface so it cannot expose the previous clip's frame.
            yield return null;

            if (videoVisual != null)
            {
                videoVisual.SetActive(true);
            }
        }

        while (!hasContinued)
        {
            yield return null;
        }

        isPlaying = false;
    }

    private void Update()
    {
        if (waitingForContinue &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SoundManager.Instance.PlaySFX(SoundIds.UiSpaceContinue);
            ContinueToGameplay();
        }
    }

    private void OnVideoFinished(VideoPlayer finishedPlayer)
    {
        FinishVideo();
    }

    private void OnVideoError(VideoPlayer failedPlayer, string message)
    {
        Debug.LogError($"[GameplayCutsceneController] Video playback failed: {message}", this);
        FinishVideo();
    }

    private void FinishVideo()
    {
        if (waitingForContinue || hasContinued) return;

        if (videoPlayer != null)
        {
            videoPlayer.Pause();
        }

        waitingForContinue = true;
        SetContinuePromptVisible(true);
    }

    public void ContinueToGameplay()
    {
        if (!waitingForContinue || hasContinued) return;

        CompleteCutscene();
    }

    public void SkipCutsceneForDebug()
    {
        if (hasContinued || (!isPlaying && !waitingForContinue)) return;

        CompleteCutscene();
    }

    private void CompleteCutscene()
    {
        hasContinued = true;
        waitingForContinue = false;
        SetContinuePromptVisible(false);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.enabled = false;
        }

        if (videoVisual != null)
        {
            videoVisual.SetActive(false);
        }

        if (timeScaleWasChanged)
        {
            Time.timeScale = previousTimeScale;
            timeScaleWasChanged = false;
        }

        if (promptBlinkCoroutine != null)
        {
            StopCoroutine(promptBlinkCoroutine);
            promptBlinkCoroutine = null;
        }
    }

    private Coroutine promptBlinkCoroutine;

    private void SetContinuePromptVisible(bool visible)
    {
        if (continuePrompt == null) return;

        if (promptBlinkCoroutine != null)
        {
            StopCoroutine(promptBlinkCoroutine);
            promptBlinkCoroutine = null;
        }

        continuePrompt.gameObject.SetActive(visible);

        if (visible)
        {
            promptBlinkCoroutine = StartCoroutine(BlinkContinuePrompt());
        }
    }

    private IEnumerator BlinkContinuePrompt()
    {
        Color promptColor = continuePrompt.color;

        while (waitingForContinue)
        {
            float alpha = Mathf.Lerp(0.25f, 1f, (Mathf.Sin(Time.unscaledTime * promptBlinkSpeed * Mathf.PI) + 1f) * 0.5f);
            promptColor.a = alpha;
            continuePrompt.color = promptColor;
            yield return null;
        }

        promptColor.a = 1f;
        continuePrompt.color = promptColor;
        promptBlinkCoroutine = null;
    }

    private TMP_Text CreateFallbackContinuePrompt()
    {
        GameObject canvasObject = new GameObject(
            "GameplayCutscenePromptCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        GameObject textObject = new GameObject("ContinuePrompt", typeof(RectTransform));
        textObject.transform.SetParent(canvasObject.transform, false);

        TextMeshProUGUI prompt = textObject.AddComponent<TextMeshProUGUI>();
        RectTransform promptRect = prompt.rectTransform;
        promptRect.anchorMin = new Vector2(0.5f, 0.1f);
        promptRect.anchorMax = new Vector2(0.5f, 0.1f);
        promptRect.anchoredPosition = Vector2.zero;
        promptRect.sizeDelta = new Vector2(900f, 80f);
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.fontSize = 32f;
        prompt.color = Color.white;

        return prompt;
    }

    private void OnDestroy()
    {
        if (timeScaleWasChanged)
        {
            Time.timeScale = previousTimeScale;
        }

        if (videoPlayer == null) return;

        if (SoundManager.Exists)
        {
            SoundManager.Instance.UnregisterVideoPlayer(videoPlayer);
        }

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
    }
}
