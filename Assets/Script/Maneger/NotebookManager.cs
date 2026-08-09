using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class SuspectData
{
    public string suspectName;
    public Sprite suspectImage;
    public string shortNote;
    public string suspectDetail;
}

public class NotebookManager : MonoBehaviour
{
    [Header("��觢��: ��¡�������")]
    public Transform clueContentContainer; 
    public GameObject clueTextPrefab;      

    [Header("��觫���: ����ͧʧ���")]
    public Transform suspectGridContainer; 
    public GameObject suspectCardPrefab;   

    [Header("��ͧʧ���: ���͡���")]
    public GameObject clueTabRoot;
    public GameObject suspectListTabRoot;
    public GameObject suspectDetailTabRoot;
    public Image selectedSuspectImage;
    public TMP_Text selectedSuspectNameText;
    public TMP_Text selectedSuspectDetailText;
    [Tooltip("ตราปั๊มที่ต้องลากไปปั๊มเพื่อยืนยัน (ใช้แทนปุ่มยืนยันแบบเดิม)")]
    public StampConfirmUI confirmStamp;
    public Button backToSuspectListButton;

    [Header("�ҹ�����ż���ͧʧ���")]
    public List<SuspectData> allSuspects;

    private SuspectData selectedSuspect;
    private readonly List<SuspectCardUI> spawnedCards = new List<SuspectCardUI>();

    public void OpenNotebook()
    {
        gameObject.SetActive(true);
        selectedSuspect = null;

        if (confirmStamp != null) confirmStamp.ResetStamp();

        PopulateClueList();
        PopulateSuspectCards();

        // โชว์เบาะแสค้างไว้ข้างๆ ตอนเลือกผู้ต้องสงสัย
        ShowClueTab(true);
        ShowSuspectListTab(true);
        ShowSuspectDetailTab(false);
    }

    // เปิดเฉพาะหน้าเบาะแส ใช้ตอนหน้าจอจบวันโผล่ขึ้นมา
    public void OpenClueWindow()
    {
        gameObject.SetActive(true);
        selectedSuspect = null;

        PopulateClueList();

        ShowClueTab(true);
        ShowSuspectListTab(false);
        ShowSuspectDetailTab(false);
    }

    private void PopulateClueList()
    {
        if (clueContentContainer == null || clueTextPrefab == null)
        {
            return;
        }

        foreach (Transform child in clueContentContainer) Destroy(child.gameObject);

        InvestigationManager invMgr = FindObjectOfType<InvestigationManager>();
        if (invMgr == null)
        {
            return;
        }

        for (int i = 0; i < invMgr.collectedClues.Count; i++)
        {
            GameObject newClue = Instantiate(clueTextPrefab, clueContentContainer);
            TMP_Text clueText = newClue.GetComponent<TMP_Text>();
            if (clueText != null)
            {
                clueText.text = $"- {invMgr.collectedClues[i]}";
            }
        }
    }

    private void PopulateSuspectCards()
    {
        if (suspectGridContainer == null || suspectCardPrefab == null)
        {
            return;
        }

        foreach (Transform child in suspectGridContainer) Destroy(child.gameObject);
        spawnedCards.Clear();

        foreach (SuspectData suspect in allSuspects)
        {
            GameObject newCard = Instantiate(suspectCardPrefab, suspectGridContainer);
            SuspectCardUI cardUI = newCard.GetComponent<SuspectCardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(suspect, this);
                spawnedCards.Add(cardUI);
            }
        }
    }

    public void ShowSuspectDetails(string suspectName)
    {
        SuspectData suspect = FindSuspect(suspectName);
        if (suspect == null)
        {
            return;
        }

        selectedSuspect = suspect;
        UpdateSelectedSuspectView();
        HighlightSuspectCard(suspect.suspectName);

        ShowSuspectListTab(false);
        ShowSuspectDetailTab(true);
    }

    public void BackToSuspectList()
    {
        selectedSuspect = null;

        if (confirmStamp != null) confirmStamp.ResetStamp();

        ShowSuspectDetailTab(false);
        ShowSuspectListTab(true);
    }

    public void ConfirmSelectedSuspect()
    {
        if (selectedSuspect == null)
        {
            return;
        }

        SelectCulprit(selectedSuspect.suspectName);
    }

    public void SelectCulprit(string chosenName)
    {
        Debug.Log($"<color=orange>�����蹪����: {chosenName}!</color>");

        GameManager.Instance.SubmitVerdict(chosenName);

        gameObject.SetActive(false);
    }

    private void HighlightSuspectCard(string suspectName)
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] == null)
            {
                continue;
            }

            spawnedCards[i].SetSelected(spawnedCards[i].SuspectName == suspectName);
        }
    }

    private SuspectData FindSuspect(string suspectName)
    {
        for (int i = 0; i < allSuspects.Count; i++)
        {
            if (allSuspects[i] != null && allSuspects[i].suspectName == suspectName)
            {
                return allSuspects[i];
            }
        }

        return null;
    }

    private void UpdateSelectedSuspectView()
    {
        if (selectedSuspect == null)
        {
            return;
        }

        if (selectedSuspectImage != null)
        {
            selectedSuspectImage.sprite = selectedSuspect.suspectImage;
            selectedSuspectImage.enabled = selectedSuspect.suspectImage != null;
        }

        if (selectedSuspectNameText != null)
        {
            selectedSuspectNameText.text = selectedSuspect.suspectName;
        }

        if (selectedSuspectDetailText != null)
        {
            selectedSuspectDetailText.text = string.IsNullOrWhiteSpace(selectedSuspect.suspectDetail)
                ? selectedSuspect.shortNote
                : selectedSuspect.suspectDetail;
        }

        if (confirmStamp != null)
        {
            confirmStamp.onStampConfirmed.RemoveListener(ConfirmSelectedSuspect);
            confirmStamp.onStampConfirmed.AddListener(ConfirmSelectedSuspect);

            // ผู้ต้องสงสัยคนใหม่ = เอาตราปั๊มกลับไปวางที่เดิม รอปั๊มใหม่
            confirmStamp.ResetStamp();
        }

        if (backToSuspectListButton != null)
        {
            backToSuspectListButton.onClick.RemoveAllListeners();
            backToSuspectListButton.onClick.AddListener(BackToSuspectList);
        }
    }

    private void ShowClueTab(bool isVisible)
    {
        if (clueTabRoot != null)
        {
            clueTabRoot.SetActive(isVisible);
        }
    }

    private void ShowSuspectListTab(bool isVisible)
    {
        if (suspectListTabRoot != null)
        {
            suspectListTabRoot.SetActive(isVisible);
        }
    }

    private void ShowSuspectDetailTab(bool isVisible)
    {
        if (suspectDetailTabRoot != null)
        {
            suspectDetailTabRoot.SetActive(isVisible);
        }
    }
}