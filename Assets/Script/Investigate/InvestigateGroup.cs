using UnityEngine;

public class InvestigateGroup : MonoBehaviour, IInvestigatable
{

    [HideInInspector] public string clueDetail;
    [HideInInspector] public Transform assignedSpawnPoint;

    private float requiredListenTime;
    private float currentListenTime;
    private float lifeTimer;

    private bool isBeingListened = false;
    private bool isClueCollected = false;
    private InvestigationManager manager;

    public void Init(InvestigationManager mgr, string text, float duration)
    {
        manager = mgr;
        clueDetail = text;
        requiredListenTime = duration;
        lifeTimer = requiredListenTime + 10f;
    }

    private void Update()
    {
        if (lifeTimer > 0)
        {
            lifeTimer -= Time.deltaTime;
        }
        else if (!isClueCollected)
        {
            RemoveGroup();
            return;
        }

        if (isBeingListened && !isClueCollected)
        {
            UIManager.Instance.UpdateListeningBar(currentListenTime / requiredListenTime);
        }
    }

    public void OnListenStart()
    {
        if (!isClueCollected)
        {
            isBeingListened = true;
            UIManager.Instance.ShowListeningBar(true);
        }
    }

    public void OnListening()
    {
        if (!isBeingListened || isClueCollected) return;

        currentListenTime += Time.deltaTime;

        if (currentListenTime >= requiredListenTime)
        {
            isClueCollected = true;
            isBeingListened = false;

            UIManager.Instance.ShowListeningBar(false);
            manager.CollectClue(clueDetail);
            RemoveGroup();
        }
    }

    public void OnListenEnd()
    {
        if (isBeingListened)
        {
            isBeingListened = false;
            UIManager.Instance.ShowListeningBar(false);
        }
    }

    private void RemoveGroup()
    {
        if (isBeingListened) UIManager.Instance.ShowListeningBar(false);

        manager.OnGroupLeft(this);
        Destroy(gameObject);
    }
}