using UnityEngine;
using TMPro; // �Ӥѭ: ��ͧ��������� TextMeshPro
using System.Collections;

public class UIManager : MonoBehaviour
{
    // ���� Singleton ����������������¡�� UI ������
    public static UIManager Instance { get; private set; }

    [Header("˹�Ҩ���ѡ (HUD)")]
    public TextMeshProUGUI clueCounterText; // ��ͤ����͡�ӹǹ�����
    public TextMeshProUGUI centerWarningText; // ���˹ѧ�����͹��ҧ�� (�� ���ҹѺ�����ѧ)

    [Header("�к� UI �ͺ�ѧ")]
    public Canvas listeningCanvas;
    public UnityEngine.UI.Image listeningFill;

    [Header("Binoculars Overlay")]
    public GameObject binocularsOverlay;

    [Header("˹�ҵ�ҧ��ͻ�ѻ")]
    public GameObject endOfDayPanel; // ˹�Ҩͷֺ� �����駢���ҵ͹���ѹ

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
    public void StartCountdownDisplay(int seconds)
    {
        StartCoroutine(CountdownRoutine(seconds));
    }

    private IEnumerator CountdownRoutine(int seconds)
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

        // ��˹�Ҩͨ��ѹ�����
        if (endOfDayPanel != null) endOfDayPanel.SetActive(true);


    }

    public void ShowListeningBar(bool isVisible)
    {
        if (listeningCanvas != null)
        {
            listeningCanvas.enabled = isVisible;
        }

        // ����к�����Դ ���������ʹ��Ѻ�� 0 ����
        if (isVisible && listeningFill != null)
        {
            listeningFill.fillAmount = 0f;
        }
    }

    public void UpdateListeningBar(float progress)
    {
        if (listeningFill != null)
        {
            listeningFill.fillAmount = Mathf.Clamp01(progress);
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
