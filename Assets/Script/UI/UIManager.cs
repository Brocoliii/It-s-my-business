using UnityEngine;
using UnityEngine.UI;
using TMPro; // �Ӥѭ: ��ͧ��������� TextMeshPro
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    // ���� Singleton ����������������¡�� UI ������
    public static UIManager Instance { get; private set; }

    [Header("˹�Ҩ���ѡ (HUD)")]
    public TextMeshProUGUI clueCounterText; // ��ͤ����͡�ӹǹ�����
    public TextMeshProUGUI centerWarningText; // ���˹ѧ�����͹��ҧ�� (�� ���ҹѺ�����ѧ)
    public TextMeshProUGUI investigateTimerText; // shows remaining time for an investigate group

    [Header("�к� UI �ͺ�ѧ")]
    public Canvas listeningCanvas;
    public Image listeningFill;
    public TextMeshProUGUI investigateSequenceText;
    public TextMeshProUGUI investigateStatusText;

    [Header("Investigate Sequence Icons")]
    public RectTransform investigateSequenceIconContainer;
    public Image investigateSequenceIconPrefab;
    public Sprite upIcon;
    public Sprite downIcon;
    public Sprite leftIcon;
    public Sprite rightIcon;
    public Color pendingIconColor = Color.white;
    public Color completedIconColor = Color.green;
    public Color flashCorrectColor = Color.green;
    public Color flashWrongColor = Color.red;

    private readonly List<Image> sequenceIconPool = new List<Image>();

    [Header("Binoculars Overlay")]
    public GameObject binocularsOverlay;

    [Header("˹�ҵ�ҧ��ͻ�ѻ")]
    public GameObject endOfDayPanel; // ˹�Ҩͷֺ� �����駢���ҵ͹���ѹ
    public NotebookManager notebookManager; // ���Դ˹�ҵ�ҧ����辽�����˹�Ҩͨ��ѹ

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // �Դ��ͤ�����͹ ���˹�Ҩͨ��ѹ����͹�͹�������
        if (centerWarningText != null) centerWarningText.gameObject.SetActive(false);
        if (endOfDayPanel != null) endOfDayPanel.SetActive(false);
        ShowListeningBar(false);
        ShowInvestigateSequence(false);
        ShowBinocularsOverlay(false);
    }

    // �ѧ��ѹ����Ѻ�ѻവ��ʹ�����
    public void UpdateClueText(int current, int total)
    {
        if (clueCounterText != null)
        {
            clueCounterText.text = $"�����: {current} / {total}";
        }
    }

    // �ѧ��ѹ�Ѻ�����ѧ��ҧ�� (���¡�ҡ GameManager)
    public Coroutine StartCountdownDisplay(int seconds)
    {
        return StartCoroutine(CountdownRoutine(seconds));
    }

    private IEnumerator CountdownRoutine(int seconds)
    {
        if (centerWarningText != null)
        {
            centerWarningText.gameObject.SetActive(true);

            for (int i = seconds; i > 0; i--)
            {
                centerWarningText.text = $"���ѹ�... {i}";
                yield return new WaitForSeconds(1f);
            }

            centerWarningText.text = "�������!";
            yield return new WaitForSeconds(1f);

            centerWarningText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }

        ShowEndOfDayPanel(true);
    }

    // ��˹�Ҩͨ��ѹ ��������˹�ҵ�ҧ����辽������¡ѹ
    public void ShowEndOfDayPanel(bool isVisible)
    {
        if (endOfDayPanel != null) endOfDayPanel.SetActive(isVisible);

        if (!isVisible) return;

        NotebookManager notebook = notebookManager;
        if (notebook == null)
        {
            notebook = FindObjectOfType<NotebookManager>(true);
        }

        if (notebook != null)
        {
            notebook.OpenClueWindow();
        }
        else
        {
            Debug.LogWarning("[UIManager] ��辽�����������˹�ҵ�ҧ����辽�͹��ѹ");
        }
    }

    public void ShowListeningBar(bool isVisible)
    {
        if (listeningCanvas != null)
        {
            listeningCanvas.enabled = isVisible;
        }

        if (!isVisible)
        {
            ShowInvestigateSequence(false);
        }
    }

    // Investigate group timer display
    public void ShowInvestigateTimer(bool isVisible)
    {
        if (investigateTimerText != null)
        {
            investigateTimerText.gameObject.SetActive(isVisible);
        }
    }

    public void UpdateInvestigateTimer(float secondsRemaining)
    {
        if (investigateTimerText != null)
        {
            int sec = Mathf.CeilToInt(secondsRemaining);
            investigateTimerText.text = $"Disappears in: {sec}s";
        }
    }

    public void UpdateListeningBar(float progress)
    {
        if (listeningFill != null)
        {
            listeningFill.fillAmount = Mathf.Clamp01(progress);
        }
    }

    public void ShowInvestigateSequence(bool isVisible)
    {
        if (investigateSequenceText != null)
        {
            investigateSequenceText.gameObject.SetActive(isVisible);
        }

        if (investigateStatusText != null)
        {
            investigateStatusText.gameObject.SetActive(isVisible);
        }

        if (investigateSequenceIconContainer != null)
        {
            investigateSequenceIconContainer.gameObject.SetActive(isVisible);
        }

        if (!isVisible)
        {
            for (int i = 0; i < sequenceIconPool.Count; i++)
            {
                if (sequenceIconPool[i] != null)
                {
                    sequenceIconPool[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void UpdateInvestigateSequence(List<StratagemDirection> sequence, int currentIndex, string status, int flashIndex = -1, Color? flashColor = null)
    {
        if (sequence == null)
        {
            return;
        }

        if (HasSequenceSprites())
        {
            UpdateInvestigateSequenceIcons(sequence, currentIndex, flashIndex, flashColor);
        }
        else if (investigateSequenceText != null)
        {
            investigateSequenceText.text = BuildFallbackSequenceText(sequence);
            investigateSequenceText.gameObject.SetActive(true);
        }

        if (investigateSequenceText != null)
        {
            investigateSequenceText.gameObject.SetActive(!HasSequenceSprites());
        }

        if (investigateStatusText != null)
        {
            investigateStatusText.text = status;
        }
    }

    public void UpdateInvestigateSequence(string sequence, string status)
    {
        if (investigateSequenceText != null)
        {
            investigateSequenceText.text = sequence;
            investigateSequenceText.gameObject.SetActive(true);
        }

        if (investigateStatusText != null)
        {
            investigateStatusText.text = status;
        }

        if (investigateSequenceIconContainer != null)
        {
            investigateSequenceIconContainer.gameObject.SetActive(false);
        }
    }

    private void UpdateInvestigateSequenceIcons(List<StratagemDirection> sequence, int currentIndex, int flashIndex, Color? flashColor)
    {
        RectTransform container = GetSequenceIconContainer();
        if (container == null)
        {
            return;
        }

        EnsureSequenceIconCount(container, sequence.Count);

        for (int i = 0; i < sequence.Count; i++)
        {
            Image icon = sequenceIconPool[i];
            if (icon == null)
            {
                continue;
            }

            icon.gameObject.SetActive(true);
            icon.sprite = GetSpriteForDirection(sequence[i]);
            icon.color = ResolveSequenceIconColor(i, currentIndex, flashIndex, flashColor);
            icon.preserveAspect = true;
        }

        for (int i = sequence.Count; i < sequenceIconPool.Count; i++)
        {
            if (sequenceIconPool[i] != null)
            {
                sequenceIconPool[i].gameObject.SetActive(false);
            }
        }

        if (investigateSequenceText != null)
        {
            investigateSequenceText.gameObject.SetActive(false);
        }
    }

    private void EnsureSequenceIconCount(RectTransform container, int desiredCount)
    {
        while (sequenceIconPool.Count < desiredCount)
        {
            sequenceIconPool.Add(CreateSequenceIcon(container));
        }
    }

    private Image CreateSequenceIcon(RectTransform container)
    {
        Image icon;

        if (investigateSequenceIconPrefab != null)
        {
            icon = Instantiate(investigateSequenceIconPrefab, container);
        }
        else
        {
            GameObject iconObject = new GameObject("StratagemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(container, false);
            icon = iconObject.GetComponent<Image>();
        }

        return icon;
    }

    private RectTransform GetSequenceIconContainer()
    {
        if (investigateSequenceIconContainer != null)
        {
            return investigateSequenceIconContainer;
        }

        if (investigateSequenceText != null)
        {
            return investigateSequenceText.transform.parent as RectTransform;
        }

        return null;
    }

    private bool HasSequenceSprites()
    {
        return upIcon != null && downIcon != null && leftIcon != null && rightIcon != null;
    }

    private Sprite GetSpriteForDirection(StratagemDirection direction)
    {
        switch (direction)
        {
            case StratagemDirection.Up:
                return upIcon;
            case StratagemDirection.Down:
                return downIcon;
            case StratagemDirection.Left:
                return leftIcon;
            case StratagemDirection.Right:
                return rightIcon;
            default:
                return null;
        }
    }

    private Color ResolveSequenceIconColor(int iconIndex, int currentIndex, int flashIndex, Color? flashColor)
    {
        if (flashColor.HasValue && iconIndex == flashIndex)
        {
            return flashColor.Value;
        }

        if (iconIndex < currentIndex)
        {
            return completedIconColor;
        }

        return pendingIconColor;
    }

    private string BuildFallbackSequenceText(List<StratagemDirection> sequence)
    {
        string result = string.Empty;

        for (int i = 0; i < sequence.Count; i++)
        {
            result += DirectionToKey(sequence[i]);

            if (i < sequence.Count - 1)
            {
                result += "  >  ";
            }
        }

        return result;
    }

    private string DirectionToKey(StratagemDirection direction)
    {
        switch (direction)
        {
            case StratagemDirection.Up:
                return "W";
            case StratagemDirection.Down:
                return "S";
            case StratagemDirection.Left:
                return "A";
            case StratagemDirection.Right:
                return "D";
            default:
                return "?";
        }
    }

    public void ShowBinocularsOverlay(bool isVisible)
    {
        if (binocularsOverlay != null)
        {
            binocularsOverlay.SetActive(isVisible);
        }
    }
}
