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

            GameManager.Instance.NotifyAllCluesCollected();

            // เคลียร์ลูกค้าทุกคนที่ค้างอยู่ในฉาก ไม่ผ่าน Leave() เพื่อไม่ให้ตั้งคูลดาวน์เกิดลูกค้าใหม่
            Customer[] activeCustomers = Object.FindObjectsByType<Customer>(FindObjectsSortMode.None);
            foreach (Customer c in activeCustomers) Destroy(c.gameObject);

            GameManager.Instance.ChangeState(GameManager.GameState.CulpritSelection);

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

            // ผ่านม่านปิดจอก่อนเปิดสมุด เหมือนเส้นทางจริงใน GameManager.EndOfDayCoroutine
            StartCoroutine(PlayShutterThenOpenNotebook("เคลียร์ฉากและเปิดสมุดแล้ว!"));
        }
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
    }
}