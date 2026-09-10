using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Tooltip("โควต้าความผิดพลาด (พลาดครบจำนวนนี้แพ้ทันที เช่น ใส่ 3 = พลาดครั้งที่ 3 จบเกม)")]
    public int maxMistakesAllowed = 3;

    [Tooltip("ชื่อคนร้ายตัวจริงของด่านนี้ (เอาไว้เช็คตอนจบวันว่าผู้เล่นเลือกจับคนร้ายถูกไหม)")]
    public string correctCulpritName = "ลุงหนวด";

    [Header("เนื้อเรื่องก่อนเริ่มด่าน")]
    [TextArea(2, 4)]
    public string introMessage = "ยินดีต้อนรับสู่วันแรกของการทำงาน!";

    [Header("ผ่านด่านนี้แล้วไปฉากไหนต่อ")]

    [Tooltip("ชื่อฉาก (Scene) ด่านถัดไปที่จะโหลดตอนผู้เล่นชนะด่านนี้ เช่นด่าน 1 ให้ใส่ 'Lv2'\nต้องเพิ่มฉากนั้นไว้ใน File > Build Settings ด้วย\nปล่อยว่าง = ชนะแล้วอยู่ฉากเดิม (เช่นด่านสุดท้าย)")]
    public string nextSceneName = "";

    [Tooltip("หน่วงกี่วินาทีหลังชนะ ก่อนจะเปลี่ยนไปฉากถัดไป (เผื่อเวลาให้ผู้เล่นเห็นผลตัดสินก่อน)")]
    public float nextSceneDelay = 2f;


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
    // จำนวนออเดอร์ที่เสิร์ฟได้ถูกใจลูกค้าในด่านนี้ ใช้โชว์บนแผงสรุปผลตอนจบด่าน (StageResultPanel)
    [HideInInspector] public int completedOrders = 0;
    // ผ่านโควตาเบาะแสของด่านแล้วหรือยัง (เก็บได้ >= requiredCluesToPass)
    // เป็นแค่สถานะ "มีสิทธิ์ชนะแล้ว" ไม่ได้แปลว่าการสืบสวนจบ
    [HideInInspector] public bool allCluesCollected = false;

    // ธงปิดร้าน = ไม่มีลูกค้าใหม่เข้ามาอีก
    // ตั้งตอนเบาะแสในด่านหมดกองแล้วเท่านั้น (เก็บได้บ้าง หลุดไปบ้าง จนไม่เหลือให้เกิดอีก)
    // ไม่เกี่ยวกับ allCluesCollected เพราะเก็บครบโควตาแล้วก็ยังสืบต่อได้ถ้ากองยังไม่หมด
    [HideInInspector] public bool stopSpawningCustomers = false;
    private bool isEndingSequenceStarted = false;
    // กันสั่งโหลดฉากถัดไปซ้ำ ถ้าเข้าสถานะ Win มากกว่าหนึ่งครั้ง
    private bool isLoadingNextScene = false;
    private InvestigationManager investigationManager;

    // สาเหตุที่แพ้ของรอบนี้ ตั้งไว้ก่อนเรียก ChangeState(Lose) เพื่อให้หน้าจอแพ้เอาไปโชว์ได้
    private string pendingLoseReason;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // ถ้าผู้เล่นเพิ่งกดโหลดเซฟมา ให้เอาเลขด่านที่เซฟไว้มาใส่ตรงนี้
        // ต้องทำใน Awake เท่านั้น เพราะ Start ข้างล่างสั่งเริ่มด่านไปแล้ว
        // (ทางปกติของเกมนี้ที่แยกด่านเป็นคนละฉาก ตัวนี้จะไม่แตะอะไรเลย ฉากปลายทางตั้งค่าของตัวเองไว้ถูกอยู่แล้ว)
        SaveManager.ApplyPendingLoad(this);
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

            // เสียงตอนแพ้: ตัดเสียงประกอบที่ค้างอยู่ทิ้งก่อน (เสียงเตาย่าง เสียงลูกค้า ฯลฯ)
            // ไม่งั้นมันจะดังคลอไปกับหน้าจอแพ้ ซึ่งฟังแล้วเหมือนเกมยังเล่นอยู่
            SoundManager.Instance.StopAllSFX();
            SoundManager.Instance.PlaySFX(SoundIds.StageLose);
            SoundManager.Instance.PlayBGM(SoundIds.BgmLose);

            // เด้งหน้าจอแพ้ตรงนี้ที่เดียว จะได้ครอบทุกทางที่เข้าสถานะ Lose (รวมปุ่ม Debug ด้วย)
            // ถ้าฉากมีแผงสรุปผลที่เปิด Show On Lose ไว้ ให้แผงนั้นรับงานไปแทนหน้าจอแพ้เดิม
            if (!StageResultPanel.TryShowResult(false, pendingLoseReason))
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowLosePanel(pendingLoseReason);
            }
        }
        else if (newState == GameState.Win)
        {
            Debug.Log("<color=green>🎉 ยินดีด้วย! คุณจับคนร้ายได้และผ่านด่าน!</color>");

            SoundManager.Instance.StopAllSFX();
            SoundManager.Instance.PlaySFX(SoundIds.StageWin);
            SoundManager.Instance.PlayBGM(SoundIds.BgmWin);

            // เซฟอัตโนมัติตรงนี้ที่เดียว จะได้ครอบทุกทางที่เข้าสถานะ Win (รวมปุ่ม Debug ด้วย)
            // เขียนก่อนเปิดแผงสรุปผล ผู้เล่นจะได้เห็นช่องอัตโนมัติที่เพิ่งเขียนไปตอนกด SAVE ทันที
            // ด่านสุดท้ายที่ไม่มีด่านถัดไปจะไม่มีอะไรถูกเขียน (ตัวมันเช็คให้เอง)
            SaveManager.AutoSave(this, investigationManager);

            // ฉากไหนมีแผงสรุปผล (StageResultPanel) ให้โชว์ผลแล้วรอผู้เล่นกด NEXT CASE เอง
            // ไม่ต้องเด้งไปฉากถัดไปอัตโนมัติ ไม่งั้นแผงจะโผล่มาแป๊บเดียวแล้วโดนเปลี่ยนฉากทับ
            if (StageResultPanel.TryShowResult(true)) return;

            // ไปด่านถัดไปตรงนี้ที่เดียว จะได้ครอบทุกทางที่เข้าสถานะ Win (รวมปุ่ม Debug ด้วย)
            GoToNextStageScene();
        }
    }

    // ชื่อฉากด่านถัดไปของด่านปัจจุบัน คืนค่าว่างถ้าไปต่อไม่ได้
    // (ไม่ได้ตั้ง Next Scene Name ไว้ / สะกดชื่อฉากผิด / ลืมใส่ฉากใน Build Settings)
    // เช็คตั้งแต่ตอนนี้เลย ไม่ต้องรอหน่วงจนครบแล้วค่อยไปพังตอน LoadScene
    // logProblems = false ไว้ให้ฝั่ง UI เรียกมาถามเฉย ๆ ว่าไปต่อได้ไหม จะได้ไม่มี error ซ้ำ ๆ ใน Console
    public string ResolveNextSceneName(bool logProblems = true)
    {
        // เผื่อฉากไหนยังไม่ได้ตั้ง Stages ไว้ (เช่นกดปุ่ม Debug ชนะในฉากทดสอบ) จะได้ไม่พังทั้งเกม
        if (stages == null || currentStageIndex < 0 || currentStageIndex >= stages.Count)
        {
            if (logProblems) Debug.LogWarning("[ระบบ] ชนะแล้วแต่หา StageConfig ของด่านปัจจุบันไม่เจอ เลยไม่รู้ว่าต้องไปฉากไหนต่อ");
            return string.Empty;
        }

        string nextScene = stages[currentStageIndex].nextSceneName;

        if (string.IsNullOrWhiteSpace(nextScene))
        {
            if (logProblems) Debug.Log("<color=yellow>[ระบบ]</color> ด่านนี้ไม่ได้ตั้ง Next Scene Name ไว้ เลยอยู่ฉากเดิมต่อ");
            return string.Empty;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextScene))
        {
            if (logProblems) Debug.LogError($"[ระบบ] โหลดฉาก '{nextScene}' ไม่ได้! เช็คชื่อฉากให้ตรง และเพิ่มฉากนี้ใน File > Build Settings ด้วย");
            return string.Empty;
        }

        return nextScene;
    }

    // เรียกตอนชนะด่าน: โหลดฉากด่านถัดไปที่ตั้งไว้ใน Next Scene Name ของด่านปัจจุบัน
    private void GoToNextStageScene()
    {
        if (isLoadingNextScene) return;

        string nextScene = ResolveNextSceneName();
        if (string.IsNullOrEmpty(nextScene)) return;

        isLoadingNextScene = true;
        StartCoroutine(LoadNextSceneCoroutine(nextScene, stages[currentStageIndex].nextSceneDelay));
    }

    private System.Collections.IEnumerator LoadNextSceneCoroutine(string nextScene, float delay)
    {
        if (delay > 0f)
        {
            // unscaledTime เผื่อหน้าจอชนะ/สมุดไปสั่งหยุดเวลาเกมไว้ จะได้ยังนับถอยหลังต่อได้
            yield return new WaitForSecondsRealtime(delay);
        }

        Debug.Log($"<color=green>[ระบบ]</color> ผ่านด่านแล้ว! กำลังไปฉาก '{nextScene}'");

        // กันเคสที่มีอะไรไปตั้ง timeScale = 0 ค้างไว้ ไม่งั้นฉากใหม่จะเปิดมาแล้วค้างนิ่งทั้งฉาก
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextScene);
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

        SoundManager.Instance.PlaySFX(SoundIds.Mistake);

        // ตั้ง Max Mistakes Allowed = 3 คือพลาดครบ 3 ครั้งแพ้ทันที (ไม่ใช่ครั้งที่ 4)
        if (currentMistakes >= config.maxMistakesAllowed)
        {
            LoseGame(loseMessageTooManyMistakes);
        }
    }

    // เรียกตอนลูกค้าได้ของตรงออเดอร์แล้วเดินจากไปแบบพอใจ (Customer.Leave)
    public void AddCompletedOrder()
    {
        // จบเกมไปแล้วไม่ต้องนับต่อ ด้วยเหตุผลเดียวกับ AddMistake
        if (CurrentState == GameState.Lose || CurrentState == GameState.Win) return;

        completedOrders++;
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

        // สลับไปเพลงจบวันตั้งแต่เริ่มนับถอยหลัง ให้ผู้เล่นรู้สึกว่าช่วงทำอาหารจบแล้ว
        SoundManager.Instance.PlayBGM(SoundIds.BgmEndOfDay);

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

        // เก็บกวาดหมูที่ค้างอยู่บนเตาตอนม่านปิดจออยู่ ผู้เล่นจะได้ไม่เห็นมันหายวับต่อหน้า
        // และเสียงย่างที่ดังวนอยู่จะได้ไม่ดังค้างไปทับช่วงหน้าจอจบวัน
        ClearGrillsForEndOfDay();

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

    // public เพราะทางไปหน้าเลือกคนร้ายมีหลายเส้น (จบวันจริง + ปุ่ม Debug ที่ข้ามมาตรง ๆ)
    // ทุกเส้นต้องเรียกตัวนี้ ไม่งั้นของบนเตาจะค้างข้ามไปโผล่ตอนเปิดสมุด พร้อมเสียงย่างที่ไม่มีใครสั่งหยุด
    // ฉากหนึ่งอาจมีเตามากกว่าหนึ่งตัว เลยกวาดทุกตัวในฉาก ไม่ใช่ผูกไว้กับตัวเดียวใน Inspector
    public void ClearGrillsForEndOfDay()
    {
        int cleared = 0;

        GrillStation[] grills = FindObjectsByType<GrillStation>(FindObjectsSortMode.None);
        foreach (GrillStation grill in grills)
        {
            cleared += grill.ClearAllFood();
        }

        Debug.Log($"<color=white>[ระบบ]</color> เก็บของบนเตาตอนจบวันแล้ว {cleared} ชิ้น (เตา {grills.Length} ตัว)");

        // ชั้นที่สอง: ชิ้นที่ยังนับว่า "ย่างอยู่" แต่หลุดจากทะเบียนช่องของเตาไปแล้ว
        // ClearAllFood ไล่ตามทะเบียนช่องอย่างเดียว ถ้าทะเบียนกับสถานะจริงไม่ตรงกันจะเก็บไม่ครบ
        // (ชิ้นที่เพิ่งโดน Destroy ไปด้านบนจะถูกข้าม เพราะ SetGrilling กันสั่งค่าเดิมซ้ำอยู่แล้ว)
        FoodInstance[] foods = FindObjectsByType<FoodInstance>(FindObjectsSortMode.None);
        foreach (FoodInstance food in foods)
        {
            if (food.IsOnGrill) food.SetGrilling(false);
        }

        // ชั้นสุดท้าย: สั่งหยุดเสียงย่างทุกช่องที่ยังดังอยู่ ครอบไปถึงเสียงเตาที่ไม่ได้ออกมาจากตัวอาหาร
        // เช่น SfxTrigger แบบ Loop While Enabled ที่แปะไว้บนตัวเตาในฉาก ซึ่งหยุดเองตอนถูกปิดเท่านั้น
        // ไม่มีใครสั่งหยุดตอนจบวัน แล้วจะดังคลอไปทับช่วงเปิดสมุดหลักฐาน
        SoundManager.Instance.StopSFX(SoundIds.GrillSizzle, 0.3f);
    }
}