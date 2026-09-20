using UnityEngine;
using UnityEngine.UI;

public class TutorialBookUI : MonoBehaviour
{
    public static TutorialBookUI Instance { get; private set; }

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

    private int currentPageIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (closeButton != null) closeButton.onClick.AddListener(CloseBook);

        if (bookPanel != null) bookPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
        if (prevButton != null) prevButton.onClick.RemoveListener(PrevPage);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseBook);
    }

    public void OpenBook()
    {
        if (bookPanel == null) return;

        bookPanel.SetActive(true);
        Time.timeScale = 0f; 

        currentPageIndex = 0;
        UpdatePageDisplay();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundIds.UiNotebookOpen);
        }
    }

    public void NextPage()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            UpdatePageDisplay();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundIds.UiPageTurn); 
            }
        }
    }

    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePageDisplay();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundIds.UiPageTurn); 
            }
        }
    }

    public void CloseBook()
    {
        if (bookPanel == null) return;

        bookPanel.SetActive(false);
        Time.timeScale = 1f; 

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundIds.UiNotebookClose);
        }
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
    }
}