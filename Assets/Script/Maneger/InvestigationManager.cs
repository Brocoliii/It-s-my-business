using UnityEngine;
using System.Collections.Generic;

public class InvestigationManager : MonoBehaviour
{
    [Header("ตั้งค่าการเกิด NPC สืบสวน")]
    public GameObject npcGroupPrefab;
    public Transform[] spawnPoints;
    public float spawnCooldown = 15f;

    [Header("เบาะแสที่ผู้เล่นหาได้แล้ว")]
    public List<string> collectedClues = new List<string>();

    private List<InvestigateGroup> activeGroups = new List<InvestigateGroup>();

    private List<Transform> occupiedPoints = new List<Transform>();
    private float spawnTimer;

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        if (currentStage == null || collectedClues.Count >= currentStage.requiredCluesToPass) return;

        int maxGroups = Mathf.Min(1, currentStage.maxConcurrentGroups);

        if (activeGroups.Count >= maxGroups) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnCooldown)
        {
            SpawnGroup(currentStage);
            spawnTimer = 0f;
        }
    }

    public void SpawnGroup(StageConfig stage)
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || stage.cluesPool.Count == 0) return;

        List<Transform> availablePoints = new List<Transform>();
        foreach (Transform pt in spawnPoints)
        {
            if (!occupiedPoints.Contains(pt)) availablePoints.Add(pt);
        }

        if (availablePoints.Count == 0) return; 

        List<ClueData1> availableClues = new List<ClueData1>();
        foreach (var clue in stage.cluesPool)
        {
            if (!collectedClues.Contains(clue.clueText) && !IsClueBeingDiscussed(clue.clueText))
            {
                availableClues.Add(clue);
            }
        }

        if (availableClues.Count == 0) return;

        Transform spot = availablePoints[Random.Range(0, availablePoints.Count)];
        GameObject go = Instantiate(npcGroupPrefab, spot.position, Quaternion.identity);
        InvestigateGroup group = go.GetComponent<InvestigateGroup>();

        if (group != null)
        {
            ClueData1 selectedClue = availableClues[Random.Range(0, availableClues.Count)];

            group.Init(this, selectedClue.clueText, selectedClue.listenDuration);

            group.assignedSpawnPoint = spot;
            occupiedPoints.Add(spot);
            activeGroups.Add(group);
        }
    }

    private bool IsClueBeingDiscussed(string text)
    {
        foreach (var g in activeGroups) if (g.clueDetail == text) return true;
        return false;
    }

    public void OnGroupLeft(InvestigateGroup group)
    {
        activeGroups.Remove(group);

        if (group.assignedSpawnPoint != null)
        {
            occupiedPoints.Remove(group.assignedSpawnPoint);
        }
    }

    public void CollectClue(string clue)
    {
        if (!collectedClues.Contains(clue))
        {
            collectedClues.Add(clue);

            StageConfig currentStage = GameManager.Instance.GetCurrentStage();
            Debug.Log($"<color=yellow>[เบาะแส]</color> {collectedClues.Count}/{currentStage.requiredCluesToPass}");

            if (collectedClues.Count >= currentStage.requiredCluesToPass)
            {
                GameManager.Instance.NotifyAllCluesCollected();
            }
        }
    }
}