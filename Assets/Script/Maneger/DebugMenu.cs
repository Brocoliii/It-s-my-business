using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    [Header("Debug Win Button")]
    public bool showWinDebugButton = true;
    public Vector2 winButtonPosition = new Vector2(20f, 20f);
    public Vector2 winButtonSize = new Vector2(180f, 44f);

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
            GameManager.Instance.ChangeState(GameManager.GameState.Win);
            Debug.Log("<color=cyan>[Debug]</color> ข้ามไปสถานะชนะ");
        }
        else
        {
            Debug.LogWarning("<color=cyan>[Debug]</color> ไม่พบ GameManager เพื่อ skip ไป win");
        }
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
            foreach (FoodInstance f in activeFoods) Destroy(f.gameObject);

            Cup currentCup = Object.FindAnyObjectByType<Cup>();
            if (currentCup != null) Destroy(currentCup.gameObject);

            GameManager.Instance.ChangeState(GameManager.GameState.CulpritSelection);

            NotebookManager notebook = Object.FindAnyObjectByType<NotebookManager>(FindObjectsInactive.Include);
            if (notebook != null)
            {
                notebook.OpenNotebook();
                Debug.Log("<color=cyan>[Debug]</color> เคลียร์ฉากและเปิดสมุดแล้ว!");
            }
        }
    }

    private void OnGUI()
    {
        if (!showWinDebugButton || !Application.isPlaying) return;

        if (GUI.Button(new Rect(winButtonPosition.x, winButtonPosition.y, winButtonSize.x, winButtonSize.y), "Debug: Skip to Win"))
        {
            ForceWin();
        }
    }
}