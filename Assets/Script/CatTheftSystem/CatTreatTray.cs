using System.Collections;
using UnityEngine;

public class CatTreatTray : MonoBehaviour
{
    [Header("Cat Treat")]
    [SerializeField] private GameObject treatPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Pickup Animation")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float pickupAnimDuration = 0.25f;
    [SerializeField] private float pickupSquashAmount = 0.12f;

    private Coroutine pickupAnimCoroutine;
    private Vector3 baseScale;
    private bool hasCachedBaseScale;

    public CatTreat SpawnTreatAt(Vector3 worldPosition)
    {
        if (treatPrefab == null) return null;

        worldPosition.z = spawnPoint != null ? spawnPoint.position.z : transform.position.z;
        GameObject treatObject = Instantiate(treatPrefab, worldPosition, Quaternion.identity);
        CatTreat treat = treatObject.GetComponent<CatTreat>();
        if (treat == null) treat = treatObject.GetComponentInChildren<CatTreat>(true);

        if (treat == null)
        {
            Debug.LogError($"[CatTreatTray] prefab '{treatPrefab.name}' has no CatTreat component.", this);
            Destroy(treatObject);
            return null;
        }

        PlayPickupAnimation();
        return treat;
    }

    private void PlayPickupAnimation()
    {
        Transform target = visualRoot != null ? visualRoot : transform;
        if (!hasCachedBaseScale)
        {
            baseScale = target.localScale;
            hasCachedBaseScale = true;
        }

        if (pickupAnimCoroutine != null) StopCoroutine(pickupAnimCoroutine);
        pickupAnimCoroutine = StartCoroutine(PickupSquash(target));
    }

    private IEnumerator PickupSquash(Transform target)
    {
        float elapsed = 0f;
        while (elapsed < pickupAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pickupAnimDuration);
            float squash = Mathf.Sin(t * Mathf.PI) * pickupSquashAmount;
            target.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y, baseScale.z);
            yield return null;
        }

        target.localScale = baseScale;
        pickupAnimCoroutine = null;
    }
}