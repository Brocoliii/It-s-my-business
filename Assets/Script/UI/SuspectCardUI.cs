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

    private string mySuspectName;
    private NotebookManager myManager;

    public void SetupCard(SuspectData data, NotebookManager manager)
    {
        myManager = manager;
        mySuspectName = data.suspectName;

        if (portraitImage != null && data.suspectImage != null)
            portraitImage.sprite = data.suspectImage;

        if (nameText != null) nameText.text = data.suspectName;
        if (noteText != null) noteText.text = data.shortNote;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnCardClicked);
        }
    }

    private void OnCardClicked()
    {
        myManager.SelectCulprit(mySuspectName);
    }
}