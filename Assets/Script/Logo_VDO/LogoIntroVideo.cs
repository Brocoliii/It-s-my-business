using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class LogoIntroVideo : MonoBehaviour
{
    private static bool hasPlayedThisLaunch;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject introRoot;
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private ScreenFader screenFader;

    [Header("Options")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private UnityEvent onIntroFinished;

    private bool hasFinished;
    private bool isFinishing;

    public bool IsPlaying => !hasFinished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLaunchState()
    {
        hasPlayedThisLaunch = false;
    }

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>();
        }

        if (hasPlayedThisLaunch)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.enabled = false;
            }

            ShowMainMenuWithoutIntro();
            return;
        }

        hasPlayedThisLaunch = true;

        if (screenFader == null)
        {
            screenFader = ScreenFader.FindOrCreate();
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[LogoIntroVideo] No VideoPlayer is assigned or found.", this);
            FinishIntro();
            return;
        }

        videoPlayer.playOnAwake = false;
        SoundManager.Instance.RegisterVideoPlayer(videoPlayer);
        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(false);
        }

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
    }

    private void ShowMainMenuWithoutIntro()
    {
        hasFinished = true;

        if (introRoot != null && !IsPartOfIntroController(introRoot))
        {
            introRoot.SetActive(false);
        }

        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(true);
        }
    }

    private void Start()
    {
        if (hasFinished) return;

        if (videoPlayer.clip == null)
        {
            Debug.LogError("[LogoIntroVideo] No video clip is assigned.", this);
            FinishIntro();
            return;
        }

        videoPlayer.prepareCompleted += PlayPreparedVideo;
        videoPlayer.Prepare();
    }

    private void PlayPreparedVideo(VideoPlayer preparedPlayer)
    {
        preparedPlayer.prepareCompleted -= PlayPreparedVideo;

        if (!hasFinished)
        {
            SoundManager.Instance.RegisterVideoPlayer(preparedPlayer);
            preparedPlayer.Play();
        }
    }

    private void OnVideoFinished(VideoPlayer finishedPlayer)
    {
        FinishIntro();
    }

    private void OnVideoError(VideoPlayer failedPlayer, string message)
    {
        Debug.LogError($"[LogoIntroVideo] Video playback failed: {message}", this);
        FinishIntro();
    }

    public void SkipIntro()
    {
        if (!allowSkip || hasFinished) return;

        FinishIntro();
    }

    private void FinishIntro()
    {
        if (hasFinished || isFinishing) return;

        isFinishing = true;

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        StartCoroutine(FadeToMainMenu());
    }

    private System.Collections.IEnumerator FadeToMainMenu()
    {
        if (screenFader != null)
        {
            yield return screenFader.FadeToBlack();
        }

        if (videoPlayer != null)
        {
            videoPlayer.enabled = false;
        }

        if (introRoot != null && !IsPartOfIntroController(introRoot))
        {
            introRoot.SetActive(false);
        }

        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(true);
        }

        hasFinished = true;
        isFinishing = false;

        if (screenFader != null)
        {
            yield return screenFader.FadeFromBlack();
        }

        onIntroFinished?.Invoke();
    }

    private bool IsPartOfIntroController(GameObject root)
    {
        return root == gameObject || transform.IsChildOf(root.transform);
    }

    private void OnDestroy()
    {
        if (videoPlayer == null) return;

        if (SoundManager.Exists)
        {
            SoundManager.Instance.UnregisterVideoPlayer(videoPlayer);
        }

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.prepareCompleted -= PlayPreparedVideo;
    }
}
