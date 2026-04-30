using UnityEngine;

public class DebugMenu : MonoBehaviour
{

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
}