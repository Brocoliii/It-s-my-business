using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// สลับแผงย่อยในหน้าตั้งค่าด้วยปุ่มลูกศร หรือปุ่มเลือกแผงโดยตรง
// ใส่สคริปต์นี้ไว้ที่ตัวแม่ของแผงตั้งค่า แล้วเรียงแผงใน panels ตามลำดับที่ต้องการ
// ถ้าต้องการให้หน้า Audio แสดงปุ่มไป Camera และ Resolution ให้ใส่ปุ่มทั้งสอง
// ใน panelButtons ตามลำดับเดียวกับ panels ระบบจะซ่อนปุ่มของแผงที่กำลังเปิดอยู่
public class SettingsPanelCarousel : MonoBehaviour
{
    [Header("แผงตั้งค่าเรียงตามลำดับ")]
    [Tooltip("ลากแผงย่อยมาเรียงจากซ้ายไปขวาในลำดับที่ต้องการแสดง")]
    [SerializeField] private GameObject[] panels;

    [Header("ปุ่มลูกศร")]
    [Tooltip("ปุ่มลูกศรซ้ายสำหรับไปแผงก่อนหน้า")]
    [SerializeField] private Button previousButton;
    [Tooltip("ปุ่มลูกศรขวาสำหรับไปแผงถัดไป")]
    [SerializeField] private Button nextButton;
    [Tooltip("เปิดเพื่อให้กดจากแผงสุดท้ายวนกลับไปแผงแรกได้")]
    [SerializeField] private bool wrapPanels = true;

    [Header("ปุ่มเลือกแผงโดยตรง (ไม่มีก็ได้)")]
    [Tooltip("ปุ่มแต่ละปุ่มจะตรงกับแผงตามลำดับใน panels ปุ่มของแผงที่กำลังเปิดจะถูกซ่อนอัตโนมัติ")]
    [SerializeField] private Button[] panelButtons;

    [Header("แอนิเมชันตอนเปิดแผง")]
    [Tooltip("เปิดใช้เอฟเฟกต์ขยายและจางเข้าทุกครั้งที่แผงตั้งค่าปรากฏ")]
    [SerializeField] private bool playAppearAnimation = true;
    [Tooltip("ระยะเวลาของแอนิเมชันเป็นวินาที")]
    [SerializeField] private float appearAnimationDuration = 0.2f;
    [Tooltip("ขนาดเริ่มต้นของแผง เช่น 0.9 = เริ่มเล็กกว่าขนาดปกติ 10%")]
    [SerializeField] private float appearAnimationStartScale = 0.9f;
    [Tooltip("ใส่ CanvasGroup ของแผงย่อยที่ต้องการให้จางเข้า ถ้าเว้นว่างระบบจะค้นหาหรือสร้างให้เอง")]
    [SerializeField] private CanvasGroup appearCanvasGroup;

    private int currentPanelIndex;
    private Coroutine highlightRefresh;
    private Coroutine appearAnimation;
    private Transform appearTarget;
    private Vector3 appearTargetScale;

    private void Awake()
    {
        DetachArrowFromSettingsPanels(previousButton);
        DetachArrowFromSettingsPanels(nextButton);
        DetachPanelButtonsFromSettingsPanels();

        if (previousButton != null) previousButton.onClick.AddListener(ShowPreviousPanel);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextPanel);

        if (panelButtons != null)
        {
            for (int i = 0; i < panelButtons.Length; i++)
            {
                int panelIndex = i;
                if (panelButtons[i] != null)
                {
                    panelButtons[i].onClick.AddListener(() => ShowPanelByIndex(panelIndex));
                }
            }
        }
    }

    private void OnEnable()
    {
        SetNavigationVisible(true);
        ShowPanel(0);
        PlayAppearAnimation();
        QueueNavigationHighlight();
    }

    private void OnDisable()
    {
        if (appearAnimation != null)
        {
            StopCoroutine(appearAnimation);
            appearAnimation = null;
        }

        if (appearTarget != null)
        {
            appearTarget.localScale = appearTargetScale;
        }

        if (appearCanvasGroup != null)
        {
            appearCanvasGroup.alpha = 1f;
        }

        SetNavigationVisible(false);
    }

    public void SetNavigationVisible(bool isVisible)
    {
        SetArrowVisibility(isVisible);
        SetPanelButtonVisibility(isVisible);

        if (isVisible)
        {
            QueueNavigationHighlight();
        }
    }

    public void ShowNextPanel()
    {
        ShowPanel(currentPanelIndex + 1);
        QueueNavigationHighlight();
    }

    public void ShowPreviousPanel()
    {
        ShowPanel(currentPanelIndex - 1);
        QueueNavigationHighlight();
    }

    public void ShowPanelByIndex(int panelIndex)
    {
        ShowPanel(panelIndex);
        QueueNavigationHighlight();
    }

    private void ShowPanel(int requestedIndex)
    {
        if (panels == null || panels.Length == 0)
        {
            if (previousButton != null) previousButton.interactable = false;
            if (nextButton != null) nextButton.interactable = false;
            SetPanelButtonVisibility(false);
            return;
        }

        // Move controls out before disabling the current page. Otherwise a
        // button nested in that page is disabled with its parent.
        DetachArrowFromSettingsPanels(previousButton);
        DetachArrowFromSettingsPanels(nextButton);

        int nextIndex = requestedIndex;
        if (wrapPanels)
        {
            nextIndex = (requestedIndex % panels.Length + panels.Length) % panels.Length;
        }
        else
        {
            nextIndex = Mathf.Clamp(requestedIndex, 0, panels.Length - 1);
        }

        currentPanelIndex = nextIndex;

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null) panels[i].SetActive(i == currentPanelIndex);
        }

        if (wrapPanels)
        {
            if (previousButton != null) previousButton.interactable = panels.Length > 1;
            if (nextButton != null) nextButton.interactable = panels.Length > 1;
        }
        else
        {
            if (previousButton != null) previousButton.interactable = currentPanelIndex > 0;
            if (nextButton != null) nextButton.interactable = currentPanelIndex < panels.Length - 1;
        }

        UpdatePanelButtons();
    }

    private void UpdatePanelButtons()
    {
        if (panelButtons == null) return;

        for (int i = 0; i < panelButtons.Length; i++)
        {
            Button panelButton = panelButtons[i];
            if (panelButton == null) continue;

            bool isValidPanel = panels != null && i < panels.Length && panels[i] != null;
            panelButton.gameObject.SetActive(isValidPanel && i != currentPanelIndex);
            panelButton.interactable = isValidPanel;
        }
    }

    private void SetPanelButtonVisibility(bool isVisible)
    {
        if (panelButtons == null) return;

        for (int i = 0; i < panelButtons.Length; i++)
        {
            if (panelButtons[i] != null)
            {
                panelButtons[i].gameObject.SetActive(isVisible &&
                    panels != null &&
                    i < panels.Length &&
                    panels[i] != null &&
                    i != currentPanelIndex);
            }
        }
    }

    private void SetArrowVisibility(bool isVisible)
    {
        if (previousButton != null && previousButton.gameObject != gameObject)
        {
            previousButton.gameObject.SetActive(isVisible);
        }

        if (nextButton != null && nextButton.gameObject != gameObject)
        {
            nextButton.gameObject.SetActive(isVisible);
        }
    }

    private void QueueNavigationHighlight()
    {
        if (highlightRefresh != null)
        {
            StopCoroutine(highlightRefresh);
        }

        highlightRefresh = StartCoroutine(ClearNavigationSelection());
    }

    private void PlayAppearAnimation()
    {
        if (!playAppearAnimation)
        {
            return;
        }

        if (appearAnimation != null)
        {
            StopCoroutine(appearAnimation);
        }

        if (panels == null || panels.Length == 0 || panels[currentPanelIndex] == null)
        {
            return;
        }

        appearTarget = panels[currentPanelIndex].transform;
        appearTargetScale = appearTarget.localScale;
        if (appearCanvasGroup == null || appearCanvasGroup.gameObject != panels[currentPanelIndex])
        {
            appearCanvasGroup = appearTarget.GetComponent<CanvasGroup>();
        }

        if (appearCanvasGroup == null)
        {
            appearCanvasGroup = appearTarget.gameObject.AddComponent<CanvasGroup>();
        }

        appearAnimation = StartCoroutine(AnimateAppear());
    }

    private IEnumerator AnimateAppear()
    {
        float duration = Mathf.Max(0f, appearAnimationDuration);
        float startScale = Mathf.Max(0f, appearAnimationStartScale);

        appearTarget.localScale = appearTargetScale * startScale;
        appearCanvasGroup.alpha = 0f;
        appearCanvasGroup.interactable = true;
        appearCanvasGroup.blocksRaycasts = true;

        if (duration <= 0f)
        {
            appearTarget.localScale = appearTargetScale;
            appearCanvasGroup.alpha = 1f;
            appearAnimation = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            appearTarget.localScale = Vector3.LerpUnclamped(
                appearTargetScale * startScale,
                appearTargetScale,
                easedProgress);
            appearCanvasGroup.alpha = easedProgress;
            yield return null;
        }

        appearTarget.localScale = appearTargetScale;
        appearCanvasGroup.alpha = 1f;
        appearAnimation = null;
    }

    private IEnumerator ClearNavigationSelection()
    {
        yield return null;

        if (EventSystem.current == null)
        {
            yield break;
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void DetachArrowFromSettingsPanels(Button arrowButton)
    {
        if (arrowButton == null || arrowButton.transform == transform || panels == null)
        {
            return;
        }

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null || !arrowButton.transform.IsChildOf(panels[i].transform))
            {
                continue;
            }

            // A page is disabled when switching, so navigation controls cannot be
            // children of a page. Preserve the screen position while moving them
            // to the carousel root.
            arrowButton.transform.SetParent(transform, true);
            Debug.LogWarning(
                "[SettingsPanelCarousel] An arrow button was inside a settings panel and was moved to the carousel root.",
                arrowButton);
            return;
        }
    }

    private void DetachPanelButtonsFromSettingsPanels()
    {
        if (panelButtons == null) return;

        for (int i = 0; i < panelButtons.Length; i++)
        {
            DetachArrowFromSettingsPanels(panelButtons[i]);
        }
    }

    private void OnDestroy()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(ShowPreviousPanel);
        if (nextButton != null) nextButton.onClick.RemoveListener(ShowNextPanel);
    }
}
