using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    [Header("Debug Buttons")]
    public bool showWinDebugButton = true;
    public Vector2 winButtonPosition = new Vector2(20f, 20f);
    public Vector2 winButtonSize = new Vector2(180f, 44f);
    public bool showInvestSelectorDebugButton = true;
    public Vector2 investSelectorButtonPosition = new Vector2(20f, 80f);
    public bool showForceCookSeasoningDebugButton = true;
    public Vector2 forceCookSeasoningButtonPosition = new Vector2(20f, 140f);
    public bool showSkipIntroDebugButton = true;
    public Vector2 skipIntroButtonPosition = new Vector2(20f, 200f);
    public bool showLoseDebugButton = true;
    public Vector2 loseButtonPosition = new Vector2(20f, 260f);
    public bool showDiscardCluesDebugButton = true;
    public Vector2 discardCluesButtonPosition = new Vector2(20f, 320f);

    [ContextMenu("บังคับเสกลูกค้า")]
    public void ForceSpawnCustomer()
    {
        CustomerManager cm = FindObjectOfType<CustomerManager>();
        if (cm != null && cm.customerSlots.Length > 0)
        {
            int randomSlot = Random.Range(0, cm.customerSlots.Length);
            cm.SpawnCustomer(randomSlot);
            Debug.Log($"<color=cyan>[Debug]</color> บังคับเสกลูกค้าที่ช่อง {randomSlot}");
        }
    }

    [ContextMenu("บังคับเสกกลุ่มคนคุย")]
    public void ForceSpawnInvestigation()
    {
        InvestigationManager im = FindObjectOfType<InvestigationManager>();
        if (im != null)
        {
            StageConfig currentStage = GameManager.Instance.GetCurrentStage();
            im.SpawnGroup(currentStage);
            Debug.Log("<color=cyan>[Debug]</color> บังคับเสกกลุ่มสืบสวน");
        }
    }

    [ContextMenu("Skip to Win (Debug)")]
    public void ForceWin()
    {
        if (GameManager.Instance != null)
        {
            // เคลียร์ลูกค้าและกลุ่มสืบสวนที่ค้างอยู่ในฉากก่อน จำลองสภาพว่าผู้เล่นเคลียร์ทุกคน/ทุกกลุ่มแล้วจริง ๆ ก่อนชนะ
            Customer[] activeCustomers = Object.FindObjectsByType<Customer>(FindObjectsSortMode.None);
            foreach (Customer c in activeCustomers) Destroy(c.gameObject);

            InvestigateGroup[] activeGroups = Object.FindObjectsByType<InvestigateGroup>(FindObjectsSortMode.None);
            foreach (InvestigateGroup g in activeGroups) g.ForceRemove();

            GameManager.Instance.ChangeState(GameManager.GameState.Win);
            Debug.Log("<color=cyan>[Debug]</color> เคลียร์ลูกค้า/กลุ่มสืบสวนแล้วข้ามไปสถานะชนะ");
        }
        else
        {
            Debug.LogWarning("<color=cyan>[Debug]</color> ไม่พบ GameManager เพื่อ skip ไป win");
        }
    }

    // ยิงพลาดรัว ๆ จนเกินโควตา แทนที่จะกระโดดเข้าสถานะ Lose ตรง ๆ
    // จะได้ทดสอบเส้นทางจริง (AddMistake -> LoseGame -> หน้าจอแพ้) ไม่ใช่แค่เปิดแผงมาดู
    [ContextMenu("Skip to Lose (Debug)")]
    public void ForceLose()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("<color=cyan>[Debug]</color> ไม่พบ GameManager เพื่อ skip ไป lose");
            return;
        }

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        int guard = 0;

        while (GameManager.Instance.CurrentState != GameManager.GameState.Lose && guard <= currentStage.maxMistakesAllowed + 1)
        {
            GameManager.Instance.AddMistake();
            guard++;
        }

        Debug.Log("<color=cyan>[Debug]</color> ทำพลาดจนเกินโควตาแล้ว ควรเห็นหน้าจอแพ้");
    }

    // ปล่อยเบาะแสหลุดทีละชิ้นเหมือนผู้เล่นฟังไม่ทันจริง ๆ จนเหลือไม่พอถึงโควตา
    // จะได้ทดสอบเส้นทางจริง (DiscardClue -> NotifyCluesUnreachable -> หน้าจอแพ้) ไม่ใช่กระโดดเข้าสถานะแพ้ตรง ๆ
    [ContextMenu("Skip to Lose by Not Enough Clues (Debug)")]
    public void ForceLoseByNotEnoughClues()
    {
        InvestigationManager im = Object.FindAnyObjectByType<InvestigationManager>();
        if (GameManager.Instance == null || im == null)
        {
            Debug.LogWarning("<color=cyan>[Debug]</color> ไม่พบ GameManager หรือ InvestigationManager เพื่อทดสอบแพ้เพราะเบาะแสไม่พอ");
            return;
        }

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        if (currentStage == null || currentStage.cluesPool == null) return;

        // เอากลุ่มที่ค้างบนจอออกก่อน ไม่งั้นตอนมันหมดเวลาทีหลังจะไปสรุปผลเบาะแสซ้ำกับที่เราเพิ่งทิ้งไป
        InvestigateGroup[] activeGroups = Object.FindObjectsByType<InvestigateGroup>(FindObjectsSortMode.None);
        foreach (InvestigateGroup g in activeGroups) g.ForceRemove();

        foreach (ClueData1 clue in currentStage.cluesPool)
        {
            // แพ้แล้วหยุดทันที จะได้เห็นว่าหลุดไปกี่ชิ้นถึงตัดจบจริง ๆ
            if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) break;
            if (clue != null) im.DiscardClue(clue.clueText);
        }

        Debug.Log($"<color=cyan>[Debug]</color> ปล่อยเบาะแสหลุดจนไม่ถึงโควตาแล้ว (หลุด {im.lostClues.Count} ชิ้น / ต้องการ {currentStage.requiredCluesToPass}) ควรเห็นหน้าจอแพ้");
    }

    [ContextMenu("Skip to Invest Selector (Debug)")]
    public void SkipToInvestSelector()
    {
        if (GameManager.Instance != null)
        {
            // เก็บเบาะแสให้ครบโควต้าจริง ๆ ก่อน ไม่ใช่แค่กระโดดสถานะเฉย ๆ
            // เพื่อให้เงื่อนไขชนะ (SubmitVerdict) ผ่านได้จริงตอนทดสอบ
            InvestigationManager im = Object.FindAnyObjectByType<InvestigationManager>();
            StageConfig currentStage = GameManager.Instance.GetCurrentStage();

            if (im != null && currentStage != null)
            {
                im.collectedClues.Clear();
                for (int i = 0; i < currentStage.requiredCluesToPass; i++)
                {
                    if (i < currentStage.cluesPool.Count) im.CollectClue(currentStage.cluesPool[i].clueText);
                }
            }

            GameManager.Instance.NotifyCluesQuotaMet();

            // เคลียร์ลูกค้าทุกคนที่ค้างอยู่ในฉาก ไม่ผ่าน Leave() เพื่อไม่ให้ตั้งคูลดาวน์เกิดลูกค้าใหม่
            Customer[] activeCustomers = Object.FindObjectsByType<Customer>(FindObjectsSortMode.None);
            foreach (Customer c in activeCustomers) Destroy(c.gameObject);

            GameManager.Instance.ChangeState(GameManager.GameState.CulpritSelection);

            // เก็บกลุ่มสืบสวนที่ค้างบนจอออก หลังเปลี่ยนสถานะแล้วเท่านั้น
            // เพราะครบโควตาแล้วกลุ่มยังเกิดต่อได้ ตอนกดปุ่มนี้จึงมีสิทธิ์มีกลุ่มค้างอยู่จริง
            // ถ้าเอาออกตอนยังเป็น Playing อยู่ OnGroupLeft จะไปสรุปว่าเบาะแสหมดกองแล้วสั่งจบวันซ้อนกับปุ่มนี้
            ClearActiveInvestigateGroups();

            // ผ่านม่านปิดจอก่อนเปิดสมุด เหมือนเส้นทางจริงใน GameManager.EndOfDayCoroutine
            // ไม่งั้นปุ่ม Debug นี้จะเปิดสมุดผุดขึ้นมาเฉย ๆ ไม่เหมือนตอนเล่นจริง
            StartCoroutine(PlayShutterThenOpenNotebook("ข้ามไปเลือกผู้ต้องสงสัยแล้ว (เก็บเบาะแสครบ + เคลียร์ลูกค้าหมด)"));
        }
        else
        {
            Debug.LogWarning("<color=cyan>[Debug]</color> ไม่พบ GameManager เพื่อ skip ไป invest selector");
        }
    }

    [ContextMenu("บังคับทำอาหารบนโต๊ะปรุงรสให้สุกทันที")]
    public void ForceCookSeasoningStationFood()
    {
        SeasoningStation[] stations = Object.FindObjectsByType<SeasoningStation>(FindObjectsSortMode.None);
        int count = 0;
        foreach (SeasoningStation station in stations)
        {
            foreach (FoodInstance food in station.FoodsOnSlots)
            {
                if (food == null) continue;
                food.ForceCook();
                count++;
            }
        }
        Debug.Log($"<color=cyan>[Debug]</color> บังคับให้อาหารบนโต๊ะปรุงรสสุกแล้ว {count} ชิ้น");
    }

    [ContextMenu("บังคับจบวันและเคลียร์ฉาก")]
    public void ForceEndDay()
    {
        if (GameManager.Instance != null)
        {
            InvestigationManager im = Object.FindAnyObjectByType<InvestigationManager>();
            StageConfig currentStage = GameManager.Instance.GetCurrentStage();

            if (im != null && currentStage != null)
            {
                im.collectedClues.Clear();
                for (int i = 0; i < currentStage.requiredCluesToPass; i++)
                {
                    if (i < currentStage.cluesPool.Count) im.CollectClue(currentStage.cluesPool[i].clueText);
                }
            }

            Customer[] activeCustomers = Object.FindObjectsByType<Customer>(FindObjectsSortMode.None);
            foreach (Customer c in activeCustomers) Destroy(c.gameObject);

            FoodInstance[] activeFoods = Object.FindObjectsByType<FoodInstance>(FindObjectsSortMode.None);
            foreach (FoodInstance f in activeFoods) Destroy(f.RootObject);

            Cup currentCup = Object.FindAnyObjectByType<Cup>();
            if (currentCup != null) Destroy(currentCup.gameObject);

            GameManager.Instance.ChangeState(GameManager.GameState.CulpritSelection);

            // เอากลุ่มสืบสวนออกหลังเปลี่ยนสถานะแล้ว ด้วยเหตุผลเดียวกับใน SkipToInvestSelector
            ClearActiveInvestigateGroups();

            // ผ่านม่านปิดจอก่อนเปิดสมุด เหมือนเส้นทางจริงใน GameManager.EndOfDayCoroutine
            StartCoroutine(PlayShutterThenOpenNotebook("เคลียร์ฉากและเปิดสมุดแล้ว!"));
        }
    }

    private void ClearActiveInvestigateGroups()
    {
        InvestigateGroup[] activeGroups = Object.FindObjectsByType<InvestigateGroup>(FindObjectsSortMode.None);
        foreach (InvestigateGroup g in activeGroups) g.ForceRemove();
    }

    // ปุ่ม Debug ต่าง ๆ ข้ามไปสถานะ CulpritSelection ตรง ๆ แต่ยังอยากให้ดูเหมือนเปลี่ยนฉากด้วยม่านเหมือนเล่นจริง
    // เลยรวมไว้ที่เดียวแทนที่จะก็อปโค้ดเดิมจาก GameManager.EndOfDayCoroutine ซ้ำในทุกปุ่ม
    private System.Collections.IEnumerator PlayShutterThenOpenNotebook(string successLog)
    {
        SceneShutterTransition shutter = Object.FindAnyObjectByType<SceneShutterTransition>(FindObjectsInactive.Include);
        if (shutter != null)
        {
            yield return shutter.PlayCloseSequence();
        }

        NotebookManager notebook = Object.FindAnyObjectByType<NotebookManager>(FindObjectsInactive.Include);
        if (notebook != null)
        {
            notebook.OpenNotebook();
            Debug.Log($"<color=cyan>[Debug]</color> {successLog}");
        }
        else
        {
            Debug.LogWarning("<color=cyan>[Debug]</color> ไม่พบ NotebookManager เพื่อเปิดสมุด");
        }
    }

    [ContextMenu("Skip Intro Dialogue (Debug)")]
    public void SkipIntroDialogue()
    {
        IntroDialogue intro = Object.FindAnyObjectByType<IntroDialogue>();
        if (intro != null)
        {
            intro.SkipIntro();
            Debug.Log("<color=cyan>[Debug]</color> ข้าม Intro Dialogue แล้ว");
        }
        else
        {
            Debug.LogWarning("<color=cyan>[Debug]</color> ไม่พบ IntroDialogue เพื่อข้าม");
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        if (showWinDebugButton && GUI.Button(new Rect(winButtonPosition.x, winButtonPosition.y, winButtonSize.x, winButtonSize.y), "Debug:Skip to Win"))
        {
            ForceWin();
        }

        if (showInvestSelectorDebugButton && GUI.Button(new Rect(investSelectorButtonPosition.x, investSelectorButtonPosition.y, winButtonSize.x, winButtonSize.y), "Debug:Skip to Invest"))
        {
            SkipToInvestSelector();
        }

        if (showForceCookSeasoningDebugButton && GUI.Button(new Rect(forceCookSeasoningButtonPosition.x, forceCookSeasoningButtonPosition.y, winButtonSize.x, winButtonSize.y), "Debug:Cook Seasoning Food"))
        {
            ForceCookSeasoningStationFood();
        }

        if (showSkipIntroDebugButton && GUI.Button(new Rect(skipIntroButtonPosition.x, skipIntroButtonPosition.y, winButtonSize.x, winButtonSize.y), "Debug:Skip Intro"))
        {
            SkipIntroDialogue();
        }

        if (showLoseDebugButton && GUI.Button(new Rect(loseButtonPosition.x, loseButtonPosition.y, winButtonSize.x, winButtonSize.y), "Debug:Skip to Lose"))
        {
            ForceLose();
        }

        if (showDiscardCluesDebugButton && GUI.Button(new Rect(discardCluesButtonPosition.x, discardCluesButtonPosition.y, winButtonSize.x, winButtonSize.y), "Debug:Lose by No Clues"))
        {
            ForceLoseByNotEnoughClues();
        }
    }
}