using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class SuspectData
{
    public string suspectName;
    public Sprite suspectImage;
    public string shortNote; 
}

public class NotebookManager : MonoBehaviour
{
    [Header("ฝั่งขวา: รายการเบาะแส")]
    public Transform clueContentContainer; 
    public GameObject clueTextPrefab;      

    [Header("ฝั่งซ้าย: ผู้ต้องสงสัย")]
    public Transform suspectGridContainer; 
    public GameObject suspectCardPrefab;   

    [Header("ฐานข้อมูลผู้ต้องสงสัย")]
    public List<SuspectData> allSuspects;

    public void OpenNotebook()
    {
        gameObject.SetActive(true);

        foreach (Transform child in clueContentContainer) Destroy(child.gameObject);
        foreach (Transform child in suspectGridContainer) Destroy(child.gameObject);

        InvestigationManager invMgr = FindObjectOfType<InvestigationManager>();
        if (invMgr != null)
        {
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

        foreach (SuspectData suspect in allSuspects)
        {
            GameObject newCard = Instantiate(suspectCardPrefab, suspectGridContainer);
            SuspectCardUI cardUI = newCard.GetComponent<SuspectCardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(suspect, this);
            }
        }
    }

    public void SelectCulprit(string chosenName)
    {
        Debug.Log($"<color=orange>ผู้เล่นชี้ตัว: {chosenName}!</color>");

        GameManager.Instance.SubmitVerdict(chosenName);

        gameObject.SetActive(false);
    }
}