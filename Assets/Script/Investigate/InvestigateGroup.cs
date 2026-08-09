using UnityEngine;
using System.Collections.Generic;

public class InvestigateGroup : MonoBehaviour, IInvestigatable
{
    [Header("Stratagem Minigame")]
    [SerializeField] private int stratagemLength = 5;
    [SerializeField] private float progressPerSuccess = 10f;
    [SerializeField] private float correctFlashDuration = 0.15f;

    [HideInInspector] public string clueDetail;
    [HideInInspector] public Transform assignedSpawnPoint;

    private float requiredListenTime;
    private float currentListenTime;
    private float lifeTimer;

    private bool isBeingListened = false;
    private bool isClueCollected = false;
    private bool isResettingAfterMistake = false;
    private InvestigationManager manager;
    private readonly List<StratagemDirection> currentStratagem = new List<StratagemDirection>();
    private int currentStratagemIndex = 0;
    private int feedbackFlashIndex = -1;
    private Color? feedbackFlashColor = null;
    private int feedbackFlashToken = 0;

    public void Init(InvestigationManager mgr, string text, float duration)
    {
        manager = mgr;
        clueDetail = text;
        requiredListenTime = duration;
        currentListenTime = 0f;
        currentStratagem.Clear();
        currentStratagemIndex = 0;
        lifeTimer = requiredListenTime + 10f;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInvestigateTimer(true);
            UIManager.Instance.UpdateInvestigateTimer(lifeTimer);
        }
    }

    private void Update()
    {
        if (lifeTimer > 0)
        {
            lifeTimer -= Time.deltaTime;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateInvestigateTimer(lifeTimer);
            }
        }
        else if (!isClueCollected)
        {
            RemoveGroup();
            return;
        }

        if (isBeingListened && !isClueCollected)
        {
            RefreshInvestigationUI();
        }
    }

    public void OnListenStart()
    {
        if (!isClueCollected)
        {
            isBeingListened = true;
            EnsureStratagemExists();
            UIManager.Instance.ShowListeningBar(true);
            UIManager.Instance.ShowBinocularsOverlay(true);
            UIManager.Instance.ShowInvestigateSequence(true);
            RefreshInvestigationUI();
        }
    }

    public void OnListening()
    {
        if (!isBeingListened || isClueCollected) return;

        RefreshInvestigationUI();
    }

    public void OnStratagemInput(StratagemDirection direction)
    {
        if (!isBeingListened || isClueCollected || isResettingAfterMistake)
            return;

        EnsureStratagemExists();

        if (currentStratagem.Count == 0)
            return;

        if (direction == currentStratagem[currentStratagemIndex])
        {
            HandleCorrectInputFeedback();
        }
        else
        {
            StartCoroutine(HandleWrongInputFeedback());
        }
    }

    public void OnListenEnd()
    {
        if (isBeingListened)
        {
            isBeingListened = false;
            UIManager.Instance.ShowListeningBar(false);
            UIManager.Instance.ShowBinocularsOverlay(false);
            UIManager.Instance.ShowInvestigateSequence(false);
        }
    }

    private void EnsureStratagemExists()
    {
        // If there is no stratagem or the index has already advanced past
        // the end (e.g. player completed the sequence but exited),
        // regenerate and reset related state so the UI isn't stuck showing
        // all-completed icons on next entry.
        if (currentStratagem.Count == 0 || currentStratagemIndex >= currentStratagem.Count)
        {
            GenerateNewStratagem();
            currentStratagemIndex = 0;
            feedbackFlashIndex = -1;
            feedbackFlashColor = null;
        }
    }

    private void GenerateNewStratagem()
    {
        currentStratagem.Clear();

        for (int i = 0; i < stratagemLength; i++)
        {
            currentStratagem.Add((StratagemDirection)Random.Range(0, 4));
        }

        currentStratagemIndex = 0;
    }

    private void RefreshInvestigationUI()
    {
        if (UIManager.Instance == null || isClueCollected)
            return;

        float progress = requiredListenTime <= 0f ? 0f : currentListenTime / requiredListenTime;
        UIManager.Instance.UpdateListeningBar(progress);
        UIManager.Instance.UpdateInvestigateSequence(currentStratagem, currentStratagemIndex, $"{currentStratagemIndex}/{currentStratagem.Count} | {Mathf.CeilToInt(currentListenTime)}s / {Mathf.CeilToInt(requiredListenTime)}s", feedbackFlashIndex, feedbackFlashColor);
    }

    private void HandleCorrectInputFeedback()
    {
        int flashedIndex = currentStratagemIndex;
        currentStratagemIndex++;
        PlayFeedbackFlash(flashedIndex, UIManager.Instance != null ? UIManager.Instance.flashCorrectColor : Color.green, correctFlashDuration);
        RefreshInvestigationUI();

        if (currentStratagemIndex >= currentStratagem.Count)
        {
            currentListenTime = Mathf.Min(requiredListenTime, currentListenTime + progressPerSuccess);

            if (currentListenTime >= requiredListenTime)
            {
                isClueCollected = true;
                isBeingListened = false;
                isResettingAfterMistake = false;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowListeningBar(false);
                    UIManager.Instance.ShowBinocularsOverlay(false);
                    UIManager.Instance.ShowInvestigateSequence(false);
                }

                manager.CollectClue(clueDetail);
                RemoveGroup();
                return;
            }

            StartCoroutine(AdvanceToNextStratagemAfterFlash(correctFlashDuration));
            return;
        }

        RefreshInvestigationUI();
    }

    private System.Collections.IEnumerator AdvanceToNextStratagemAfterFlash(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!isBeingListened || isClueCollected)
        {
            yield break;
        }

        feedbackFlashIndex = -1;
        feedbackFlashColor = null;
        GenerateNewStratagem();
        RefreshInvestigationUI();
    }

    private System.Collections.IEnumerator HandleWrongInputFeedback()
    {
        isResettingAfterMistake = true;
        PlayFeedbackFlash(currentStratagemIndex, UIManager.Instance != null ? UIManager.Instance.flashWrongColor : Color.red, 1f);

        yield return new WaitForSeconds(1f);

        GenerateNewStratagem();
        isResettingAfterMistake = false;
        feedbackFlashIndex = -1;
        feedbackFlashColor = null;
        RefreshInvestigationUI();
    }

    private void PlayFeedbackFlash(int flashIndex, Color flashColor, float duration)
    {
        feedbackFlashToken++;
        int token = feedbackFlashToken;

        feedbackFlashIndex = flashIndex;
        feedbackFlashColor = flashColor;

        RefreshInvestigationUI();
        StartCoroutine(ClearFeedbackFlashAfterDelay(token, duration));
    }

    private System.Collections.IEnumerator ClearFeedbackFlashAfterDelay(int token, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (token != feedbackFlashToken)
        {
            yield break;
        }

        feedbackFlashIndex = -1;
        feedbackFlashColor = null;
        RefreshInvestigationUI();
    }

    private void RemoveGroup()
    {
        if (isBeingListened)
        {
            UIManager.Instance.ShowListeningBar(false);
            UIManager.Instance.ShowBinocularsOverlay(false);
            UIManager.Instance.ShowInvestigateSequence(false);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInvestigateTimer(false);
        }

        manager.OnGroupLeft(this);
        Destroy(gameObject);
    }
}