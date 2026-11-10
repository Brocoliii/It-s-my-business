using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialBookUI : MonoBehaviour
{
    public static TutorialBookUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("UI")]
    [Tooltip(" Book Panel")]
    public GameObject bookPanel;

    [Header("Pages")]
    [Tooltip("How many")]
    public GameObject[] pages;

    [Header("Control")]
    [Tooltip("next")]
    public Button nextButton;
    [Tooltip("Prev/Back")]
    public Button prevButton;
    [Tooltip("clsoe")]
    public Button closeButton;

    [Header("Animation")]
    [SerializeField] private float openCloseDuration = 0.25f;
    [SerializeField, Range(0.5f, 1f)] private float closedScale = 0.9f;
    [SerializeField] private AnimationCurve openCloseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float pageTransitionDuration = 0.2f;
    [SerializeField, Range(0.9f, 1f)] private float pageStartScale = 0.96f;

    private int currentPageIndex = 0;
    private CanvasGroup bookPanelCanvasGroup;
    private Vector3 bookPanelBaseScale = Vector3.one;
    private Coroutine bookAnimation;
    private Coroutine pageAnimation;
    private float timeScaleBeforeBook;
    private float bookPanelAlpha = 1f;
    private bool bookPanelInteractable = true;
    private bool bookPanelBlocksRaycasts = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        IsOpen = false;

        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (closeButton != null) closeButton.onClick.AddListener(CloseBook);

        if (bookPanel != null)
        {
            bookPanelCanvasGroup = bookPanel.GetComponent<CanvasGroup>();
            if (bookPanelCanvasGroup == null) bookPanelCanvasGroup = bookPanel.AddComponent<CanvasGroup>();
            bookPanelBaseScale = bookPanel.transform.localScale;
        }

        if (bookPanel != null) bookPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
        if (prevButton != null) prevButton.onClick.RemoveListener(PrevPage);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseBook);

        if (Instance == this)
        {
            IsOpen = false;
            Instance = null;
        }
    }

    public void OpenBook()
    {
        if (bookPanel == null || IsOpen) return;

        IsOpen = true;
        InputManager inputManager = FindObjectOfType<InputManager>();
        if (inputManager != null) inputManager.CancelActiveInteraction();

        bookPanel.SetActive(true);
        timeScaleBeforeBook = Time.timeScale;
        Time.timeScale = 0f;

        currentPageIndex = 0;
        UpdatePageDisplay();
        StartBookAnimation(true);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundIds.UiNotebookOpen);
        }
    }

    public void NextPage()
    {
        ChangePage(currentPageIndex + 1);
    }

    public void PrevPage()
    {
        ChangePage(currentPageIndex - 1);
    }

    public void CloseBook()
    {
        if (bookPanel == null || !IsOpen) return;

        StartBookAnimation(false);

   
    }

    public void SetVisibleForPause(bool visible)
    {
        if (!IsOpen || bookPanel == null || bookPanelCanvasGroup == null) return;

        if (!visible)
        {
            bookPanelAlpha = bookPanelCanvasGroup.alpha;
            bookPanelInteractable = bookPanelCanvasGroup.interactable;
            bookPanelBlocksRaycasts = bookPanelCanvasGroup.blocksRaycasts;

            bookPanelCanvasGroup.alpha = 0f;
            bookPanelCanvasGroup.interactable = false;
            bookPanelCanvasGroup.blocksRaycasts = false;
            return;
        }

        bookPanelCanvasGroup.alpha = bookPanelAlpha;
        bookPanelCanvasGroup.interactable = bookPanelInteractable;
        bookPanelCanvasGroup.blocksRaycasts = bookPanelBlocksRaycasts;
    }

    private void UpdatePageDisplay()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == currentPageIndex);
            }
        }

        if (prevButton != null) prevButton.interactable = (currentPageIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentPageIndex < pages.Length - 1);

        ClearNavigationSelection();
    }

    private void ChangePage(int requestedPageIndex)
    {
        if (pageAnimation != null || pages == null ||
            requestedPageIndex < 0 || requestedPageIndex >= pages.Length ||
            requestedPageIndex == currentPageIndex)
            return;

        pageAnimation = StartCoroutine(AnimatePageChange(requestedPageIndex));
    }

    private IEnumerator AnimatePageChange(int nextPageIndex)
    {
        ButtonNavigationInteractable(false);

        GameObject outgoingPage = currentPageIndex >= 0 && currentPageIndex < pages.Length
            ? pages[currentPageIndex]
            : null;
        GameObject incomingPage = pages[nextPageIndex];

        CanvasGroup outgoingGroup = GetOrAddCanvasGroup(outgoingPage);
        CanvasGroup incomingGroup = GetOrAddCanvasGroup(incomingPage);
        Vector3 outgoingScale = outgoingPage != null ? outgoingPage.transform.localScale : Vector3.one;
        Vector3 incomingScale = incomingPage != null ? incomingPage.transform.localScale : Vector3.one;

        if (incomingPage != null)
        {
            incomingPage.SetActive(true);
            incomingGroup.alpha = 0f;
            incomingPage.transform.localScale = incomingScale * pageStartScale;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0f, pageTransitionDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            t = openCloseCurve.Evaluate(t);

            if (outgoingPage != null)
            {
                outgoingGroup.alpha = 1f - t;
                outgoingPage.transform.localScale = Vector3.LerpUnclamped(
                    outgoingScale, outgoingScale * pageStartScale, t);
            }

            if (incomingPage != null)
            {
                incomingGroup.alpha = t;
                incomingPage.transform.localScale = Vector3.LerpUnclamped(
                    incomingScale * pageStartScale, incomingScale, t);
            }

            yield return null;
        }

        currentPageIndex = nextPageIndex;
        UpdatePageDisplay();
        ResetPageVisualState(outgoingPage, outgoingScale);
        ResetPageVisualState(incomingPage, incomingScale);
        ButtonNavigationInteractable(true);
        pageAnimation = null;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundIds.UiPageTurn);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject page)
    {
        if (page == null)
            return null;

        CanvasGroup canvasGroup = page.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = page.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    private void ResetPageVisualState(GameObject page, Vector3 scale)
    {
        if (page == null)
            return;

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(page);
        canvasGroup.alpha = 1f;
        page.transform.localScale = scale;
    }

    private void ButtonNavigationInteractable(bool interactable)
    {
        if (prevButton != null)
            prevButton.interactable = interactable && currentPageIndex > 0;
        if (nextButton != null)
            nextButton.interactable = interactable && pages != null && currentPageIndex < pages.Length - 1;
        if (closeButton != null)
            closeButton.interactable = interactable;
    }

    private void StartBookAnimation(bool opening)
    {
        if (bookAnimation != null)
            StopCoroutine(bookAnimation);

        bookAnimation = StartCoroutine(AnimateBook(opening));
    }

    private IEnumerator AnimateBook(bool opening)
    {
        if (bookPanelCanvasGroup == null)
            yield break;

        bookPanelCanvasGroup.interactable = false;
        bookPanelCanvasGroup.blocksRaycasts = false;

        float startAlpha = opening ? 0f : bookPanelCanvasGroup.alpha;
        float endAlpha = opening ? 1f : 0f;
        Vector3 startScale = opening
            ? bookPanelBaseScale * closedScale
            : bookPanel.transform.localScale;
        Vector3 endScale = opening
            ? bookPanelBaseScale
            : bookPanelBaseScale * closedScale;

        bookPanelCanvasGroup.alpha = startAlpha;
        bookPanel.transform.localScale = startScale;

        float elapsed = 0f;
        float duration = Mathf.Max(0f, openCloseDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            t = openCloseCurve.Evaluate(t);
            bookPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            bookPanel.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
            yield return null;
        }

        bookPanelCanvasGroup.alpha = endAlpha;
        bookPanel.transform.localScale = endScale;

        if (opening)
        {
            bookPanelCanvasGroup.interactable = true;
            bookPanelCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            bookPanel.SetActive(false);
            IsOpen = false;
            Time.timeScale = timeScaleBeforeBook;
        }

        bookAnimation = null;
    }

    private void ClearNavigationSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}