using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class InvestigationManager : MonoBehaviour
{
    [Header("ตั้งค่าการเกิด NPC สืบสวน")]
    public GameObject npcGroupPrefab;
    public Transform[] spawnPoints;
    [Tooltip("เวลารอต่ำสุดก่อนกลุ่มถัดไปจะเกิด (วินาที)")]
    [FormerlySerializedAs("spawnCooldown")]
    public float minSpawnCooldown = 15f;
    [Tooltip("เวลารอสูงสุดก่อนกลุ่มถัดไปจะเกิด (วินาที) ใส่น้อยกว่าค่าต่ำสุดจะถูกดันขึ้นมาเท่าค่าต่ำสุดให้เอง")]
    public float maxSpawnCooldown = 20f;

    [Header("เบาะแสที่ผู้เล่นหาได้แล้ว")]
    public List<string> collectedClues = new List<string>();

    [Header("เบาะแสที่หลุดไปแล้ว (ฟังไม่ทันจนกลุ่มหายไป)")]
    [Tooltip("เบาะแสในลิสต์นี้จะไม่ถูกสุ่มมาเกิดอีกตลอดด่าน ถือว่าเสียไปเลย")]
    public List<string> lostClues = new List<string>();

    private List<InvestigateGroup> activeGroups = new List<InvestigateGroup>();

    private List<Transform> occupiedPoints = new List<Transform>();
    private float spawnTimer;

    // เวลารอของรอบปัจจุบัน สุ่มใหม่ทุกครั้งที่เริ่มนับรอบใหม่
    // -1 = ยังไม่ได้สุ่ม (ใช้เป็นสัญญาณให้สุ่มครั้งแรกใน Update แทนการเขียน Awake เพิ่ม)
    private float currentSpawnCooldown = -1f;

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        if (currentStage == null) return;

        // ไม่หยุดเกิดกลุ่มตอนเก็บครบโควตาแล้ว ตราบใดที่ในกองยังเหลือเบาะแสให้หยิบ
        // requiredCluesToPass ใช้ตัดสินแพ้อย่างเดียว ไม่ใช่ตัวสั่งจบการสืบสวน
        // ตัวที่หยุดเกิดจริง ๆ คือ SpawnGroup ที่หา availableClues ไม่เจอแล้ว
        int maxGroups = Mathf.Min(1, currentStage.maxConcurrentGroups);

        if (activeGroups.Count >= maxGroups) return;

        if (currentSpawnCooldown < 0f)
        {
            currentSpawnCooldown = RollSpawnCooldown();
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnCooldown)
        {
            SpawnGroup(currentStage);
            spawnTimer = 0f;
            // สุ่มเวลารอของรอบถัดไปทันที จังหวะการเกิดของแต่ละกลุ่มจะได้ไม่ตายตัวเท่ากันทุกครั้ง
            currentSpawnCooldown = RollSpawnCooldown();
        }
    }

    // สุ่มเวลารอระหว่าง min - max
    // กันค่าติดลบและกันกรณีคนตั้งค่าใส่ max น้อยกว่า min ไว้ใน Inspector ไม่ให้ Random.Range พลิกช่วงเอง
    private float RollSpawnCooldown()
    {
        float min = Mathf.Max(0f, minSpawnCooldown);
        float max = Mathf.Max(min, maxSpawnCooldown);
        return Random.Range(min, max);
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
            // ตัดทั้งเบาะแสที่เก็บได้แล้ว เบาะแสที่หลุดไปแล้ว และเบาะแสที่กลุ่มปัจจุบันกำลังพูดอยู่
            // เบาะแสที่หลุดจะไม่กลับมาอีก กลุ่มถัดไปจึงเป็นเบาะแสใหม่เสมอ
            if (!collectedClues.Contains(clue.clueText)
                && !lostClues.Contains(clue.clueText)
                && !IsClueBeingDiscussed(clue.clueText))
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

            // ค่าเวลาทั้งสองตัวคิดที่นี่ที่เดียว ตัวกลุ่มแค่รับค่าไปใช้ จะได้ไม่มีเลขเวลาฝังอยู่ในโค้ดกลุ่มอีก
            // ตั้งที่ StageConfig ได้ทั้งด่าน หรือปล่อย 0 ให้ย้อนไปใช้ค่ารายชิ้น/พฤติกรรมเดิม
            float listenDuration = stage.listenDuration > 0f ? stage.listenDuration : selectedClue.listenDuration;
            float groupLifetime = stage.groupLifetime > 0f ? stage.groupLifetime : listenDuration + stage.groupExtraLifetime;

            group.Init(this, selectedClue.clueText, listenDuration, groupLifetime);

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

        // เช็คตรงนี้เพราะเป็นจังหวะเดียวที่เบาะแสของกลุ่มถูกสรุปผลแล้ว (เก็บได้ หรือหลุดไป)
        CheckInvestigationExhausted();
    }

    // ถ้าไม่มีกลุ่มไหนอยู่บนจอแล้ว และในกองก็ไม่เหลือเบาะแสให้หยิบมาเกิดอีก แปลว่าหมดเรื่องให้สืบแล้ว
    // สั่งปิดร้านทันที ไม่งั้นลูกค้าจะเกิดวนไปเรื่อยๆ ทั้งที่ผู้เล่นทำอะไรต่อไม่ได้แล้ว
    private void CheckInvestigationExhausted()
    {
        if (activeGroups.Count > 0) return;

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        if (currentStage == null) return;

        if (GetRemainingClueCount(currentStage) > 0) return;

        GameManager.Instance.NotifyNoCluesLeft();
    }

    // เรียกตอนกลุ่มหายไปโดยที่ผู้เล่นฟังไม่จบ เบาะแสนั้นจะถูกทิ้งถาวร
    // ไม่ถูกสุ่มกลับมาอีกตลอดด่าน กลุ่มถัดไปจะได้เบาะแสใหม่แทน
    public void DiscardClue(string clue)
    {
        if (string.IsNullOrEmpty(clue)) return;
        if (collectedClues.Contains(clue) || lostClues.Contains(clue)) return;

        lostClues.Add(clue);

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        if (currentStage == null) return;

        Debug.Log($"<color=orange>[เบาะแสหลุด]</color> {clue} | เหลือให้หาอีก {GetRemainingClueCount(currentStage)} ชิ้น");

        // ตัดจบตั้งแต่ตอนนี้ ถ้าเบาะแสที่เก็บได้ + ที่ยังเหลือในกอง รวมกันไม่ถึงโควตาแล้ว
        // ผู้เล่นจะเก็บยังไงก็ไม่ครบ ไม่ต้องปล่อยให้เล่นต่อจนจบวันแล้วค่อยไปแพ้ตอนปั๊มเลือกคนร้าย
        int stillReachable = collectedClues.Count + GetRemainingClueCount(currentStage);
        if (stillReachable < currentStage.requiredCluesToPass)
        {
            Debug.LogWarning($"<color=orange>[เบาะแสหลุด]</color> เบาะแสเหลือไม่พอถึงโควตาแล้ว ({stillReachable}/{currentStage.requiredCluesToPass})");
            GameManager.Instance.NotifyCluesUnreachable();
        }
    }

    // จำนวนเบาะแสในกองที่ยังหยิบมาเกิดได้อยู่ (ยังไม่ถูกเก็บและยังไม่หลุด)
    private int GetRemainingClueCount(StageConfig stage)
    {
        if (stage == null || stage.cluesPool == null) return 0;

        int count = 0;
        foreach (var clue in stage.cluesPool)
        {
            if (!collectedClues.Contains(clue.clueText) && !lostClues.Contains(clue.clueText))
                count++;
        }
        return count;
    }

    public void CollectClue(string clue)
    {
        if (!collectedClues.Contains(clue))
        {
            collectedClues.Add(clue);

            StageConfig currentStage = GameManager.Instance.GetCurrentStage();
            Debug.Log($"<color=yellow>[เบาะแส]</color> {collectedClues.Count}/{currentStage.requiredCluesToPass}");

            // ครบโควตาแล้วแค่ปักธงไว้เฉย ๆ ไม่ปิดร้าน ผู้เล่นยังเก็บเบาะแสส่วนเกินต่อได้จนกว่ากองจะหมด
            // ตัวปิดร้านจริงคือ CheckInvestigationExhausted ตอนไม่เหลือเบาะแสให้เกิดอีกแล้ว
            if (collectedClues.Count >= currentStage.requiredCluesToPass)
            {
                GameManager.Instance.NotifyCluesQuotaMet();
            }
        }
    }
}