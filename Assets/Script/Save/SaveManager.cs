using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// ตัวจัดการไฟล์เซฟทั้งหมด อ่าน/เขียน/ลบ ช่องเซฟ และจำว่าผู้เล่นเลือกโหลดช่องไหนไว้ตอนเปลี่ยนฉาก
//
// *** ห้ามลากไฟล์นี้ไปใส่ GameObject *** ไม่ใช่ MonoBehaviour เป็นแค่ตัวช่วยกลาง
// ตัวที่ต้องวางในฉากคือ SaveSlotPanel (แผงเลือกช่องเซฟ) ซึ่งสร้างให้เองอัตโนมัติอยู่แล้ว
//
// ช่องเซฟมีสองชนิด
//   ช่องอัตโนมัติ (Auto) 1 ช่อง = เกมเขียนทับให้เองทุกครั้งที่ผ่านด่าน
//                                 ผู้เล่นโหลดได้ แต่เขียนทับเองไม่ได้
//   ช่องผู้เล่น (Manual) 3 ช่อง = ผู้เล่นกดเซฟเองจากแผงสรุปผลตอนจบด่าน
//
// ไฟล์เซฟอยู่ที่ Application.persistentDataPath/saves/
// (Windows: C:\Users\<ชื่อผู้ใช้>\AppData\LocalLow\<ชื่อบริษัท>\<ชื่อเกม>\saves)
public static class SaveManager
{
    // อยากได้ช่องเซฟมากกว่านี้แก้ได้ที่สองบรรทัดนี้ที่เดียว แผงเลือกช่องสร้างแถวตามค่านี้ให้เอง
    public const int AutoSlotCount = 1;
    public const int ManualSlotCount = 3;

    private const string SaveFolderName = "saves";

    // อ่านไฟล์ครั้งเดียวแล้วจำไว้ ไม่ต้องไปแตะดิสก์ทุกครั้งที่แผงวาดแถว
    private static SaveData[] autoCache;
    private static SaveData[] manualCache;
    private static bool isCacheReady = false;

    // ช่องที่ผู้เล่นเพิ่งกดโหลด ค้างไว้ข้ามฉาก ให้ GameManager ของฉากปลายทางมาหยิบไปใช้ตอน Awake
    private static SaveData pendingLoad;

    private static string SaveFolder
    {
        get { return Path.Combine(Application.persistentDataPath, SaveFolderName); }
    }

    private static string SlotPath(bool isAuto, int index)
    {
        return Path.Combine(SaveFolder, string.Format("save_{0}_{1}.json", isAuto ? "auto" : "manual", index));
    }

    private static bool IsValidIndex(bool isAuto, int index)
    {
        return index >= 0 && index < (isAuto ? AutoSlotCount : ManualSlotCount);
    }

    // ---------- อ่าน ----------

    // คืน null = ช่องว่าง (ยังไม่เคยเซฟ หรือไฟล์เสีย)
    public static SaveData GetSlot(bool isAuto, int index)
    {
        if (!IsValidIndex(isAuto, index)) return null;

        EnsureCache();
        return isAuto ? autoCache[index] : manualCache[index];
    }

    public static bool HasSlot(bool isAuto, int index)
    {
        return GetSlot(isAuto, index) != null;
    }

    // มีเซฟให้โหลดสักช่องไหม ใช้ตัดสินว่าปุ่ม LOAD ในเมนูหลักควรกดได้หรือเปล่า
    public static bool HasAnySave()
    {
        EnsureCache();

        for (int i = 0; i < AutoSlotCount; i++) if (autoCache[i] != null) return true;
        for (int i = 0; i < ManualSlotCount; i++) if (manualCache[i] != null) return true;

        return false;
    }

    private static void EnsureCache()
    {
        if (isCacheReady) return;
        isCacheReady = true;

        autoCache = new SaveData[AutoSlotCount];
        manualCache = new SaveData[ManualSlotCount];

        for (int i = 0; i < AutoSlotCount; i++) autoCache[i] = ReadFromDisk(true, i);
        for (int i = 0; i < ManualSlotCount; i++) manualCache[i] = ReadFromDisk(false, i);
    }

    private static SaveData ReadFromDisk(bool isAuto, int index)
    {
        string path = SlotPath(isAuto, index);

        try
        {
            if (!File.Exists(path)) return null;

            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

            // ไฟล์เสีย/เวอร์ชันเก่า ให้ถือว่าเป็นช่องว่างไปเลย ดีกว่าปล่อยให้โหลดแล้วไปพังกลางเกม
            if (data == null || !data.IsValid)
            {
                Debug.LogWarning("[SaveManager] ข้ามไฟล์เซฟที่อ่านไม่ได้/เวอร์ชันไม่ตรง: " + path);
                return null;
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveManager] อ่านไฟล์เซฟไม่สำเร็จ: " + path + " (" + e.Message + ")");
            return null;
        }
    }

    // ---------- เขียน ----------

    public static bool WriteSlot(bool isAuto, int index, SaveData data)
    {
        if (data == null || !IsValidIndex(isAuto, index)) return false;

        // ก๊อปปี้ก่อนเขียนเสมอ เพราะผู้เล่นเอาข้อมูลชุดเดียวกันไปลงได้หลายช่อง
        // ถ้าเก็บตัวเดิมไว้ทุกช่อง เวลาที่บันทึกของช่องก่อนหน้าจะโดนทับตามไปด้วยทั้งหมด
        SaveData copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
        copy.isAutoSave = isAuto;
        copy.savedAtTicks = DateTime.UtcNow.Ticks;

        string path = SlotPath(isAuto, index);

        try
        {
            Directory.CreateDirectory(SaveFolder);
            File.WriteAllText(path, JsonUtility.ToJson(copy, true));
        }
        catch (Exception e)
        {
            // ดิสก์เต็ม/ไม่มีสิทธิ์เขียน ต้องบอกให้รู้ ไม่ใช่เงียบแล้วปล่อยให้ผู้เล่นนึกว่าเซฟติดแล้ว
            Debug.LogError("[SaveManager] เขียนไฟล์เซฟไม่สำเร็จ: " + path + " (" + e.Message + ")");
            return false;
        }

        EnsureCache();
        if (isAuto) autoCache[index] = copy;
        else manualCache[index] = copy;

        Debug.Log("<color=green>[SaveManager]</color> บันทึกลงช่อง " + (isAuto ? "อัตโนมัติ " : "ผู้เล่น ") + (index + 1) + " แล้ว");
        return true;
    }

    public static bool DeleteSlot(bool isAuto, int index)
    {
        if (!IsValidIndex(isAuto, index)) return false;

        string path = SlotPath(isAuto, index);

        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveManager] ลบไฟล์เซฟไม่สำเร็จ: " + path + " (" + e.Message + ")");
            return false;
        }

        EnsureCache();
        if (isAuto) autoCache[index] = null;
        else manualCache[index] = null;

        return true;
    }

    // ---------- สร้างข้อมูลเซฟจากสภาพเกมตอนนี้ ----------

    // เรียกตอนผ่านด่านแล้วเท่านั้น (จุดเดียวที่เกมนี้มี "จุดเล่นต่อ" ที่ชัดเจน)
    // คืน null = ไม่มีอะไรให้เล่นต่อแล้ว (ด่านสุดท้าย หรือฉากนี้ยังไม่ได้ตั้งด่านไว้) ผู้เรียกต้องซ่อนปุ่มเซฟทิ้ง
    public static SaveData CaptureStageCleared(GameManager gameManager, InvestigationManager investigationManager)
    {
        if (gameManager == null) return null;

        SaveData data = new SaveData();

        if (!ResolveContinuePoint(gameManager, data)) return null;

        StageConfig stage = GetCurrentStage(gameManager);

        data.clearedStageName = stage != null ? stage.stageName : "";
        data.completedOrders = Mathf.Max(0, gameManager.completedOrders);
        data.mistakes = Mathf.Max(0, gameManager.currentMistakes);
        data.maxMistakes = stage != null ? Mathf.Max(0, stage.maxMistakesAllowed) : 0;

        int clues = investigationManager != null ? investigationManager.collectedClues.Count : 0;
        data.clueTarget = GetClueTarget(stage);
        // เก็บเกินเป้าได้ แต่บนการ์ดไม่ควรโชว์ 5/4 เลยตัดไว้ที่เป้า เหมือนที่แผงสรุปผลทำ
        data.cluesCollected = Mathf.Min(clues, data.clueTarget);

        return data;
    }

    // หา "จุดที่จะเล่นต่อ" แล้วเขียนลง data คืน false = ไม่มีที่ไปต่อแล้ว
    private static bool ResolveContinuePoint(GameManager gameManager, SaveData data)
    {
        string nextScene = gameManager.ResolveNextSceneName(false);

        if (!string.IsNullOrEmpty(nextScene))
        {
            // ทางปกติของเกมนี้: ด่านถัดไปเป็นคนละฉาก
            // ฉากปลายทางมี GameManager ของตัวเองที่ตั้ง Current Stage Index ไว้ถูกแล้ว
            // ห้ามไปเขียนทับด้วยเลขจากฉากนี้ ไม่งั้นจะเล่นผิดด่าน
            data.sceneName = nextScene;
            data.stageIndex = 0;
            data.applyStageIndex = false;
            return true;
        }

        // ไม่ได้ตั้งฉากถัดไปไว้ แต่ในลิสต์ Stages ของฉากนี้ยังมีด่านถัดไปอยู่
        // แปลว่าเป็นแบบหลายด่านในฉากเดียว ต้องจำเลขด่านไปด้วยถึงจะเล่นต่อถูกจุด
        int nextIndex = gameManager.currentStageIndex + 1;

        if (gameManager.stages != null && nextIndex >= 0 && nextIndex < gameManager.stages.Count)
        {
            data.sceneName = SceneManager.GetActiveScene().name;
            data.stageIndex = nextIndex;
            data.applyStageIndex = true;
            return true;
        }

        Debug.Log("<color=yellow>[SaveManager]</color> ด่านนี้ไม่มีด่านถัดไปแล้ว เลยไม่มีจุดให้เซฟไว้เล่นต่อ");
        return false;
    }

    // เขียนเซฟอัตโนมัติ ลงช่องว่างก่อน ถ้าเต็มหมดค่อยทับช่องที่เก่าที่สุด
    // ตอนนี้ตั้ง Auto Slot Count = 1 ก็คือทับช่องเดิมทุกครั้งที่ผ่านด่าน
    // (ตัวเลือกช่องเขียนเผื่อไว้รองรับหลายช่องอยู่แล้ว เพิ่มค่าคงที่ข้างบนอย่างเดียวก็กลายเป็นวนทับทันที)
    public static bool AutoSave(GameManager gameManager, InvestigationManager investigationManager)
    {
        SaveData data = CaptureStageCleared(gameManager, investigationManager);
        if (data == null) return false;

        return WriteSlot(true, PickAutoSlotToOverwrite(), data);
    }

    private static int PickAutoSlotToOverwrite()
    {
        EnsureCache();

        int oldestIndex = 0;
        long oldestTicks = long.MaxValue;

        for (int i = 0; i < AutoSlotCount; i++)
        {
            // เจอช่องว่างเมื่อไรใช้เลย ไม่ต้องไปทับของเก่าทั้งที่ยังมีที่เหลือ
            if (autoCache[i] == null) return i;

            if (autoCache[i].savedAtTicks < oldestTicks)
            {
                oldestTicks = autoCache[i].savedAtTicks;
                oldestIndex = i;
            }
        }

        return oldestIndex;
    }

    // ---------- โหลด ----------

    // เรียกตอนผู้เล่นกดช่องเซฟในแผงโหลด: จำช่องไว้แล้วเปลี่ยนไปฉากปลายทาง
    // เฟดดำให้ถ้าฉากมี ScreenFader (เมนูหลักมีอยู่แล้ว) ไม่มีก็โหลดตรง ๆ
    public static bool LoadSlot(bool isAuto, int index)
    {
        SaveData data = GetSlot(isAuto, index);
        if (data == null) return false;

        if (!Application.CanStreamedLevelBeLoaded(data.sceneName))
        {
            Debug.LogError("[SaveManager] เซฟช่องนี้ชี้ไปฉาก '" + data.sceneName +
                "' ซึ่งโหลดไม่ได้! เช็คชื่อฉากให้ตรง และเพิ่มฉากนี้ใน File > Build Settings ด้วย");
            return false;
        }

        pendingLoad = data;

        // กันฉากใหม่เปิดมาแล้วค้างนิ่ง เผื่อแผงที่เรียกมาหยุดเวลาเกมไว้ (แผงสรุปผลตอนจบด่านทำแบบนั้น)
        Time.timeScale = 1f;

        // SoundManager ข้ามฉากไปด้วย (DontDestroyOnLoad) เสียงที่ค้างอยู่เลยตามไปดังในฉากใหม่ได้
        if (SoundManager.Instance != null) SoundManager.Instance.StopAllSFX();

        ScreenFader fader = UnityEngine.Object.FindObjectOfType<ScreenFader>();

        if (fader != null) fader.FadeOutAndLoadScene(data.sceneName);
        else SceneManager.LoadScene(data.sceneName);

        return true;
    }

    // เรียกจาก GameManager.Awake ของฉากปลายทาง เอาเลขด่านที่เซฟไว้มาใส่ให้ (ถ้าจำเป็น)
    public static void ApplyPendingLoad(GameManager gameManager)
    {
        if (pendingLoad == null || gameManager == null) return;

        SaveData data = pendingLoad;
        // หยิบไปแล้วต้องเคลียร์ทิ้งทันที ไม่งั้นเล่นต่อไปอีกหลายด่านก็ยังโดนดึงกลับมาด่านเดิม
        pendingLoad = null;

        // ไม่ต้องทำอะไรถ้าฉากปลายทางตั้งเลขด่านของตัวเองไว้ถูกแล้ว (ทางปกติของเกมนี้)
        if (!data.applyStageIndex) return;

        if (data.sceneName != SceneManager.GetActiveScene().name) return;

        if (gameManager.stages == null || data.stageIndex < 0 || data.stageIndex >= gameManager.stages.Count)
        {
            Debug.LogWarning("[SaveManager] เซฟชี้ไปด่านที่ " + data.stageIndex +
                " แต่ฉากนี้ไม่มีด่านนั้นในลิสต์ Stages เลยใช้ค่าที่ตั้งไว้ในฉากแทน");
            return;
        }

        gameManager.currentStageIndex = data.stageIndex;
        Debug.Log("<color=green>[SaveManager]</color> โหลดเซฟแล้ว เริ่มที่ด่านลำดับที่ " + data.stageIndex);
    }

    // ---------- ตัวช่วย ----------

    private static StageConfig GetCurrentStage(GameManager gameManager)
    {
        if (gameManager == null || gameManager.stages == null) return null;

        int index = gameManager.currentStageIndex;
        if (index < 0 || index >= gameManager.stages.Count) return null;

        return gameManager.stages[index];
    }

    // นับเบาะแสเทียบกับทั้งกองของด่าน เหมือนค่าปริยายของแผงสรุปผล ตัวเลขบนการ์ดจะได้ตรงกับที่ผู้เล่นเพิ่งเห็น
    private static int GetClueTarget(StageConfig stage)
    {
        if (stage == null) return 0;

        if (stage.cluesPool != null && stage.cluesPool.Count > 0) return stage.cluesPool.Count;

        return Mathf.Max(0, stage.requiredCluesToPass);
    }
}
