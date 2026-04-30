using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StageConfig
{
    [Tooltip("ชื่อด่านเอาไว้ดูง่ายๆ เช่น 'ด่าน 1: วันแรกของร้าน'")]
    public string stageName = "วันที่ 1";

    [Header(" ตั้งค่าเมนูอาหาร")]

    [Tooltip("รายชื่อวัตถุดิบ (Data) ที่ลูกค้าสามารถสั่งได้ในด่านนี้")]
    public List<FoodData> availableMenu;

    [Tooltip("ระดับความเผ็ดสูงสุดที่ด่านนี้จะสุ่ม (เช่น ใส่ 0 = ไม่เผ็ดเลย, ใส่ 2 = สุ่มตั้งแต่ 0 ถึง 2)")]
    public int maxSpicyLevel = 0;

    [Tooltip("ด่านนี้มีระบบทาซอสไหม? (ติ๊กถูก = ลูกค้ามีโอกาสสั่งแบบทาซอส)")]
    public bool allowSauce = false;

    [Header(" ตั้งค่าการสืบสวน")]

    [Tooltip("จำนวนกลุ่มคนที่จะโผล่มาคุยพร้อมกันได้สูงสุดในหน้าจอ")]
    public int maxConcurrentGroups = 1;

    [Tooltip("เป้าหมายของด่าน: ผู้เล่นต้องแอบฟังให้ได้กี่เบาะแสถึงจะมีสิทธิ์ชนะ")]
    public int requiredCluesToPass = 2;

    [Tooltip("คลังประโยคเบาะแสทั้งหมดของด่านนี้ ")]
    public List<ClueData1> cluesPool;

    [Header("เงื่อนไขแพ้-ชนะ (Win/Lose)")]

    [Tooltip("โควต้าความผิดพลาด")]
    public int maxMistakesAllowed = 3;

    [Tooltip("ชื่อคนร้ายตัวจริงของด่านนี้ (เอาไว้เช็คตอนจบวันว่าผู้เล่นเลือกจับคนร้ายถูกไหม)")]
    public string correctCulpritName = "ลุงหนวด";

    [Header("เนื้อเรื่องก่อนเริ่มด่าน")]
    [TextArea(2, 4)]
    public string introMessage = "ยินดีต้อนรับสู่วันแรกของการทำงาน!";


}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Intro, Playing, CulpritSelection, Win, Lose }
    public GameState CurrentState { get; private set; }

    public List<StageConfig> stages;
    public int currentStageIndex = 0;

    [HideInInspector] public int currentMistakes = 0;
    [HideInInspector] public bool allCluesCollected = false;
    private bool isEndingSequenceStarted = false;
    private InvestigationManager investigationManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        investigationManager = FindObjectOfType<InvestigationManager>();
        ChangeState(GameState.Intro);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"<color=white>สถานะเกมเปลี่ยนเป็น: {newState}</color>");
        if (newState == GameState.Playing)
        {
            CustomerManager cm = FindObjectOfType<CustomerManager>();
            if (cm != null) cm.StartSpawningCustomers();
            Debug.Log("<color=green>เริ่มเปิดร้าน ลูกค้ากำลังมา!</color>");
        }
        else if (newState == GameState.Lose) Debug.LogError("💀 จบเห่! คุณทำพลาดเกินกำหนด หรือสืบสวนล้มเหลว");
        else if (newState == GameState.Win) Debug.Log("<color=green>🎉 ยินดีด้วย! คุณจับคนร้ายได้และผ่านด่าน!</color>");
    }

    public void AddMistake()
    {
        currentMistakes++;
        StageConfig config = GetCurrentStage();
        Debug.Log($"<color=red>พลาดแล้ว!</color> ({currentMistakes}/{config.maxMistakesAllowed})");

        if (currentMistakes > config.maxMistakesAllowed)
        {
            ChangeState(GameState.Lose);
        }
    }

    public void SubmitVerdict(string chosenName)
    {
        if (CurrentState != GameState.Playing && CurrentState != GameState.CulpritSelection) return;

        StageConfig config = GetCurrentStage();
        int collectedClues = investigationManager.collectedClues.Count;

        
        if (collectedClues >= config.requiredCluesToPass && chosenName == config.correctCulpritName)
        {
            ChangeState(GameState.Win);
        }
        else
        {
            if (collectedClues < config.requiredCluesToPass)
                Debug.Log("แพ้เพราะ: รวบรวมเบาะแสไม่ครบ!");
            else
                Debug.Log("แพ้เพราะ: จับคนผิดตัว!");

            ChangeState(GameState.Lose);
        }
    }

    public StageConfig GetCurrentStage() => stages[currentStageIndex];

    public void NotifyAllCluesCollected()
    {
        allCluesCollected = true;
        Debug.Log("<color=yellow>[ระบบ]</color> เก็บเบาะแสครบแล้ว! จะไม่มีลูกค้าใหม่เข้ามาเพิ่ม");
    }

    public void StartEndOfDaySequence()
    {
        if (isEndingSequenceStarted) return;
        isEndingSequenceStarted = true;
        StartCoroutine(EndOfDayCoroutine());
    }

    
    private System.Collections.IEnumerator EndOfDayCoroutine()
    {
        Debug.Log("<color=white>ลูกค้าหมดแล้ว... จะจบวันใน 5 วินาที</color>");
        yield return new WaitForSeconds(5f);

        ChangeState(GameState.CulpritSelection);

        NotebookManager notebook = FindObjectOfType<NotebookManager>(true);
        if (notebook != null)
        {
            notebook.OpenNotebook();
        }
        else
        {
            Debug.LogError("หาสมุดหลักฐานไม่เจอ! ");
        }
    }
}