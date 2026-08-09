using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SuspectCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image portraitImage;
    public TMP_Text nameText;
    public TMP_Text noteText;
    public Button selectButton;

    [Header("Selected Glow")]
    public Image glowImage;
    public Color glowColor = new Color(1f, 0.15f, 0.15f, 1f);
    public float glowPadding = 18f;
    public bool pulseGlow = true;
    public float pulseSpeed = 3f;
    [Range(0f, 1f)] public float minGlowAlpha = 0.35f;
    [Range(0f, 1f)] public float maxGlowAlpha = 1f;

    private string mySuspectName;
    private NotebookManager myManager;
    private bool isSelected;

    public string SuspectName { get { return mySuspectName; } }
    public bool IsSelected { get { return isSelected; } }

    public void SetupCard(SuspectData data, NotebookManager manager)
    {
        myManager = manager;
        mySuspectName = data.suspectName;

        if (portraitImage != null && data.suspectImage != null)
            portraitImage.sprite = data.suspectImage;

        if (nameText != null) nameText.text = data.suspectName;

        if (noteText != null)
        {
            noteText.gameObject.SetActive(false);
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnCardClicked);
        }

        if (glowImage == null)
        {
            glowImage = BuildGlowImage();
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (glowImage == null)
        {
            return;
        }

        glowImage.gameObject.SetActive(selected);

        Color c = glowColor;
        c.a = selected ? maxGlowAlpha : 0f;
        glowImage.color = c;
    }

    private void Update()
    {
        if (!isSelected || !pulseGlow || glowImage == null)
        {
            return;
        }

        // unscaledTime so the glow keeps pulsing while the notebook pauses the game
        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;

        Color c = glowColor;
        c.a = Mathf.Lerp(minGlowAlpha, maxGlowAlpha, t);
        glowImage.color = c;
    }

    private Image BuildGlowImage()
    {
        if (portraitImage == null)
        {
            return null;
        }

        RectTransform source = portraitImage.rectTransform;

        GameObject glowObj = new GameObject("SelectionGlow", typeof(RectTransform), typeof(Image));
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.SetParent(source.parent, false);
        glowRect.SetSiblingIndex(source.GetSiblingIndex()); // render behind the portrait

        glowRect.anchorMin = source.anchorMin;
        glowRect.anchorMax = source.anchorMax;
        glowRect.pivot = source.pivot;
        glowRect.anchoredPosition = source.anchoredPosition;
        glowRect.sizeDelta = source.sizeDelta + Vector2.one * glowPadding;
        glowRect.localRotation = source.localRotation;
        glowRect.localScale = source.localScale;

        Image glow = glowObj.GetComponent<Image>();
        glow.sprite = portraitImage.sprite;
        glow.type = portraitImage.type;
        glow.preserveAspect = portraitImage.preserveAspect;
        glow.raycastTarget = false;
        glow.color = glowColor;

        return glow;
    }

    private void OnCardClicked()
    {
        myManager.ShowSuspectDetails(mySuspectName);
    }
}
