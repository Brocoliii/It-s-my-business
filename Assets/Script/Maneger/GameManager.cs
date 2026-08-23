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

    [Header(" ตั้งค่าเวลาของกลุ่มสืบสวน")]

    [Tooltip("เวลาแอบฟังที่ต้องใช้ต่อ 1 เบาะแสของทั้งด่าน (วินาที)\nใส่มากกว่า 0 = บังคับใช้ค่านี้กับทุกเบาะแสในด่าน\nใส่ 0 = ใช้ค่า Listen Duration ที่ตั้งไว้รายชิ้นในคลังเบาะแสด้านบนแทน")]
    public float listenDuration = 0f;

    [Tooltip("เวลาที่กลุ่มยืนอยู่บนจอก่อนจะหายไป (วินาที) นับตั้งแต่กลุ่มโผล่มา\nใส่มากกว่า 0 = ใช้ค่านี้ตรง ๆ\nใส่ 0 = ใช้เวลาแอบฟัง + Group Extra Lifetime ด้านล่าง (พฤติกรรมเดิม)")]
    public float groupLifetime = 0f;

    [Tooltip("เวลาเผื่อที่บวกเพิ่มจากเวลาแอบฟัง ใช้เฉพาะตอน Group Lifetime = 0")]
    public float groupExtraLifetime = 10f;

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

    [Header("ข้อความบนหน้าจอแพ้")]
    [Tooltip("แพ้เพราะทำพลาดเกินโควตา (Max Mistakes Allowed)")]
    [TextArea(2, 3)]
    public string loseMessageTooManyMistakes = "ทำพลาดเกินที่ลูกค้าจะทนได้ ร้านโดนสั่งปิดแล้ว!";
    [Tooltip("แพ้เพราะเก็บเบาะแสไม่ถึงเป้า (Required Clues To Pass)")]
    [TextArea(2, 3)]
    public string loseMessageNotEnoughClues = "เบาะแสไม่พอที่จะชี้ตัวคนร้าย!";
    [Tooltip("แพ้เพราะปั๊มจับผิดคน")]
    [TextArea(2, 3)]
    public string loseMessageWrongCulprit = "จับผิดคน คนร้ายตัวจริงลอยนวลไปแล้ว!";

    [HideInInspector] public int currentMistakes = 0;
    // ผ่านโควตาเบาะแสของด่านแล้วหรือยัง (เก็บได้ >= requiredCluesToPass)
    // เป็นแค่สถานะ "มีสิทธิ์ชนะแล้ว" ไม่ได้แปลว่าการสืบสวนจบ
    [HideInInspector] public bool allCluesCollected = false;

    // ธงปิดร้าน = ไม่มีลูกค้าใหม่เข้ามาอีก
    // ตั้งตอนเบาะแสในด่านหมดกองแล้วเท่านั้น (เก็บได้บ้าง หลุดไปบ้าง จนไม่เหลือให้เกิดอีก)
    // ไม่เกี่ยวกับ allCluesCollected เพราะเก็บครบโควตาแล้วก็ยังสืบต่อได้ถ้ากองยังไม่หมด
    [HideInInspector] public bool stopSpawningCustomers = false;
    private bool isEndingSequenceStarted = false;
    private InvestigationManager investigationManager;

    // สาเหตุที่แพ้ของรอบนี้ ตั้งไว้ก่อนเรียก ChangeState(Lose) เพื่อให้หน้าจอแพ้เอาไปโชว์ได้
    private string pendingLoseReason;

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
        else if (newState == GameState.Lose)
        {
            Debug.LogError("💀 จบเห่! คุณทำพลาดเกินกำหนด หรือสืบสวนล้มเหลว");

            // เด้งหน้าจอแพ้ตรงนี้ที่เดียว จะได้ครอบทุกทางที่เข้าสถานะ Lose (รวมปุ่ม Debug ด้วย)
            if (UIManager.Instance != null) UIManager.Instance.ShowLosePanel(pendingLoseReason);
        }
        else if (newState == GameState.Win) Debug.Log("<color=green>🎉 ยินดีด้วย! คุณจับคนร้ายได้และผ่านด่าน!</color>");
    }

    // ทางเดียวที่ควรใช้เข้าสถานะแพ้ เพราะต้องแนบสาเหตุไปให้หน้าจอแพ้โชว์ด้วย
    public void LoseGame(string reason)
    {
        // กันสั่งซ้ำ: แพ้ไปแล้วลูกค้าที่ยังค้างอยู่บนจออาจเดินจากไปแล้วนับพลาดเพิ่มได้อีก
        if (CurrentState == GameState.Lose || CurrentState == GameState.Win) return;

        pendingLoseReason = reason;
        ChangeState(GameState.Lose);
    }

    public void AddMistake()
    {
        // จบเกมไปแล้วไม่ต้องนับพลาดต่อ (ลูกค้าที่ค้างอยู่บนจอยังเดินจากไปทีหลังได้)
        if (CurrentState == GameState.Lose || CurrentState == GameState.Win) return;

        currentMistakes++;
        StageConfig config = GetCurrentStage();
        Debug.Log($"<color=red>พลาดแล้ว!</color> ({currentMistakes}/{config.maxMistakesAllowed})");

        if (currentMistakes > config.maxMistakesAllowed)
        {
            LoseGame(loseMessageTooManyMistakes);
        }
    }

    public void SubmitVerdict(string chosenName)
    {
        if (CurrentState != GameState.Playing && CurrentState != GameState.CulpritSelection) return;

        StageConfig config = GetCurrentStage();

        // เผื่อฉากไหนไม่มี InvestigationManager วางไว้ ให้ถือว่าเก็บเบาะแสไม่ได้เลย แทนที่จะพังทั้งการตัดสิน
        int collectedClues = investigationManager != null ? investigationManager.collectedClues.Count : 0;


        if (collectedClues >= config.requiredCluesToPass && chosenName == config.correctCulpritName)
        {
            ChangeState(GameState.Win);
        }
        else if (collectedClues < config.requiredCluesToPass)
        {
            Debug.Log($"แพ้เพราะ: รวบรวมเบาะแสไม่ครบ! ({collectedClues}/{config.requiredCluesToPass})");
            LoseGame(loseMessageNotEnoughClues);
        }
        else
        {
            Debug.Log("แพ้เพราะ: จับคนผิดตัว!");
            LoseGame(loseMessageWrongCulprit);
        }
    }

    public StageConfig GetCurrentStage() => stages[currentStageIndex];

    // เรียกตอนเก็บเบาะแสถึงโควตาของด่านแล้ว (requiredCluesToPass)
    // แค่ปักธงว่า "ผ่านเงื่อนไขเบาะแสแล้ว" เท่านั้น ไม่ปิดร้าน
    // โควตานี้มีไว้ตัดสินแพ้อย่างเดียว (SubmitVerdict / NotifyCluesUnreachable)
    // กลุ่มสืบสวนกับลูกค้ายังมาต่อจนกว่าเบาะแสในกองจะหมด แล้วค่อยปิดร้านผ่าน NotifyNoCluesLeft
    public void NotifyCluesQuotaMet()
    {
        if (allCluesCollected) return;

        allCluesCollected = true;
        Debug.Log("<color=yellow>[ระบบ]</color> เก็บเบาะแสถึงโควตาแล้ว! แต่ยังสืบต่อได้จนกว่าเบาะแสจะหมดกอง");
    }

    // เรียกตอนเบาะแสในด่านหมดกองแล้ว (ไม่เหลือให้กลุ่มสืบสวนหยิบไปเกิดอีก)
    // ถึงจะเก็บไม่ครบโควตาก็ต้องปิดร้าน ไม่งั้นลูกค้าจะเกิดวนไปเรื่อยๆ โดยที่ผู้เล่นทำอะไรต่อไม่ได้แล้ว
    public void NotifyNoCluesLeft()
    {
        if (stopSpawningCustomers) return;

        Debug.Log("<color=yellow>[ระบบ]</color> เบาะแสในด่านหมดแล้ว! จะไม่มีลูกค้าใหม่เข้ามาเพิ่ม");
        CloseShop();
    }

    // เรียกตอนเบาะแสที่ยังเหลือในกอง + ที่เก็บได้แล้ว รวมกันไม่ถึงโควตาของด่านแล้ว
    // (เช่น ด่านมี 4 ต้องได้ 2 แต่ผู้เล่นปล่อยหลุดไป 3 = เหลือ 1 ยังไงก็ไม่ถึง)
    // ตัดจบให้แพ้ตรงนี้เลย ไม่ต้องรอให้เล่นจนจบวันแล้วค่อยไปแพ้ตอน SubmitVerdict
    public void NotifyCluesUnreachable()
    {
        if (CurrentState != GameState.Playing) return;

        Debug.Log("<color=orange>[ระบบ]</color> เบาะแสที่เหลือไม่พอถึงโควตาแล้ว ด่านนี้ผ่านไม่ได้อีกต่อไป");

        // ปิดร้านก่อนแพ้ ลูกค้าที่ยังค้างคิวเกิดอยู่จะได้ไม่โผล่มาทับหน้าจอแพ้
        stopSpawningCustomers = true;

        LoseGame(loseMessageNotEnoughClues);
    }

    private void CloseShop()
    {
        stopSpawningCustomers = true;

        // ถ้าตอนปิดร้านไม่มีลูกค้าค้างอยู่บนจอเลย ต้องสั่งจบวันเองตรงนี้
        // เพราะ OnCustomerLeft จะไม่ถูกเรียกอีกแล้ว (คิวเกิดที่ค้างอยู่จะโดน SpawnCustomer เมินทิ้ง)
        if (CustomerManager.Instance != null) CustomerManager.Instance.NotifyShopClosed();
    }

    public void StartEndOfDaySequence()
    {
        // แพ้กลางวันไปแล้ว (ทำพลาดครบโควตา) ห้ามเดินต่อไปหน้าจบวัน/สมุดเลือกคนร้าย
        // ลูกค้าคนสุดท้ายที่ทำให้แพ้จะเรียก OnCustomerLeft ต่อทันที ถ้าไม่กันไว้สมุดจะเด้งขึ้นมาทับหน้าจอแพ้
        if (CurrentState != GameState.Playing) return;

        if (isEndingSequenceStarted) return;
        isEndingSequenceStarted = true;
        StartCoroutine(EndOfDayCoroutine());
    }


    private System.Collections.IEnumerator EndOfDayCoroutine()
    {
        Debug.Log("<color=white>ลูกค้าหมดแล้ว... จะจบวันใน 5 วินาที</color>");

        // นับถอยหลังบนจอ แล้วค่อยเด้งหน้าจอจบวัน (พร้อมหน้าต่างเบาะแส)
        if (UIManager.Instance != null)
        {
            yield return UIManager.Instance.StartCountdownDisplay(5);
        }
        else
        {
            yield return new WaitForSeconds(5f);
        }

        ChangeState(GameState.CulpritSelection);

        // ปิดจอด้วยม่านก่อน แล้วค่อยเปิดสมุดหลักฐาน ให้ดูเป็นการเปลี่ยนฉาก
        SceneShutterTransition shutter = FindObjectOfType<SceneShutterTransition>(true);
        if (shutter != null)
        {
            yield return shutter.PlayCloseSequence();
        }

        // ม่าน (พร้อมป้าย DAY COMPLETE!) ปิดจอเสร็จแล้วค่อยเด้งหน้าจอจบวัน
        // ห้ามเปิดตั้งแต่ตอนนับถอยหลังจบ ไม่งั้นสมุดจะโผล่มาทับป้ายกับม่านที่ยังเล่นค้างอยู่
        if (UIManager.Instance != null) UIManager.Instance.ShowEndOfDayPanel(true);

        // ShowEndOfDayPanel เปิดสมุดต่อให้อยู่แล้ว ตรงนี้เป็นทางสำรองเผื่อฉากไม่มี UIManager
        // (เรียกซ้ำไม่เป็นไร OpenNotebook มีธง isOpen กันเปิดซ้ำอยู่)
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