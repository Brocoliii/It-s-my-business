using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatTheftSystem : MonoBehaviour
{
    public static CatTheftSystem Instance { get; private set; }

    [Header("Theft Timing")]
    [SerializeField] private float firstTheftDelay = 5f;
    [SerializeField] private float minimumDelayBetweenThefts = 15f;
    [SerializeField] private float maximumDelayBetweenThefts = 30f;
    [SerializeField] private float theftDuration = 5f;

    [Header("Cat Paw")]
    [SerializeField] private Transform catPaw;
    [SerializeField] private SpriteRenderer catPawRenderer;
    [SerializeField] private Collider2D catPawCollider;
    [SerializeField] private GameObject[] catPawPrefabs;
    [SerializeField] private float pawMoveDuration = 0.35f;
    [SerializeField] private float treatLeaveDuration = 1.25f;
    [SerializeField] private Transform pawEntryPoint;
    [SerializeField] private Transform pawExitPoint;
    [SerializeField] private Transform treatAttachPoint;

    private FoodInstance targetedFood;
    private Coroutine theftCoroutine;
    private Coroutine pawMoveCoroutine;
    private GameObject spawnedCatPaw;
    private bool isPawVisible;
    private bool stopRequested;

    public bool IsTheftActive => targetedFood != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveCatPawCollider();
        HidePaw();
    }

    private void Start()
    {
        theftCoroutine = StartCoroutine(TheftLoop());
    }

    public void StopForEndOfDay()
    {
        stopRequested = true;

        if (theftCoroutine != null)
        {
            StopCoroutine(theftCoroutine);
            theftCoroutine = null;
        }

        CancelTheft();

        if (SoundManager.Exists)
        {
            SoundManager.Instance.StopSFX(SoundIds.CatTheftSuccess);
            SoundManager.Instance.StopSFX(SoundIds.CatDisappear);
        }
    }

    private void OnDestroy()
    {
        if (spawnedCatPaw != null) Destroy(spawnedCatPaw);
        if (Instance == this) Instance = null;
    }

    public bool TryDeliverTreat(Vector2 treatPosition, CatTreat treat)
    {
        if (targetedFood == null || catPawCollider == null || treat == null) return false;
        if (!catPawCollider.OverlapPoint(treatPosition)) return false;

        if (pawMoveCoroutine != null)
        {
            StopCoroutine(pawMoveCoroutine);
            pawMoveCoroutine = null;
        }

        targetedFood.SetBeingStolen(false);
        targetedFood = null;
        treat.AttachToCat(treatAttachPoint != null ? treatAttachPoint : catPaw);
        SoundManager.Instance.PlaySFX(SoundIds.CatGotTreat, GetPawSoundPosition());
        SetPawVisible(true);
        pawMoveCoroutine = StartCoroutine(CarryTreatDownAndFinish(treat));
        return true;
    }

    private void ResolveCatPawCollider()
    {
        if (catPawCollider == null && catPaw != null)
            catPawCollider = catPaw.GetComponentInChildren<Collider2D>(true);

        if (catPaw == null && catPawPrefabs != null && catPawPrefabs.Length > 0) return;

        if (catPawCollider == null)
            Debug.LogError("[CatTheftSystem] Cat Paw needs a Collider2D for treat drop detection.", this);
    }

    private void SelectRandomCatPaw()
    {
        if (catPawPrefabs == null || catPawPrefabs.Length == 0) return;

        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (GameObject prefab in catPawPrefabs)
        {
            if (prefab != null) validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0) return;

        if (spawnedCatPaw != null) Destroy(spawnedCatPaw);

        GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
        spawnedCatPaw = Instantiate(selectedPrefab, GetPawEntryPosition(), selectedPrefab.transform.rotation);
        catPaw = spawnedCatPaw.transform;
        catPawRenderer = catPaw.GetComponentInChildren<SpriteRenderer>(true);
        catPawCollider = catPaw.GetComponentInChildren<Collider2D>(true);

        if (catPawCollider == null)
            Debug.LogError("[CatTheftSystem] Every cat paw prefab needs a Collider2D.", selectedPrefab);
    }

    private IEnumerator TheftLoop()
    {
        while (GameManager.Instance != null &&
               GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            yield return null;
        }

        yield return new WaitForSeconds(firstTheftDelay);

        while (CanRunTheft())
        {
            if (TryStartTheft())
            {
                yield return new WaitForSeconds(theftDuration);
                if (!CanRunTheft())
                {
                    CancelTheft();
                    yield break;
                }

                if (targetedFood != null) CompleteTheft();
            }

            yield return StartCoroutine(WaitForNextTheft());
        }

        CancelTheft();
    }

    private IEnumerator WaitForNextTheft()
    {
        float remainingDelay = Random.Range(minimumDelayBetweenThefts, maximumDelayBetweenThefts);
        Debug.Log($"[CatTheftSystem] Next theft in {remainingDelay:F1} seconds.", this);

        while (remainingDelay > 0f && CanRunTheft())
        {
            while (!HasFoodToSteal() && CanRunTheft())
            {
                Debug.Log($"[CatTheftSystem] Theft countdown paused at {remainingDelay:F1} seconds: waiting for food.", this);
                yield return new WaitForSeconds(1f);
            }

            if (!CanRunTheft()) yield break;

            yield return new WaitForSeconds(1f);
            remainingDelay -= 1f;
        }
    }

    private bool CanRunTheft()
    {
        return !stopRequested &&
               GameManager.Instance != null &&
               GameManager.Instance.CurrentState == GameManager.GameState.Playing;
    }

    private bool HasFoodToSteal()
    {
        List<FoodInstance> candidates = new List<FoodInstance>();
        AddOccupiedFoods(candidates, FindObjectsOfType<SeasoningStation>());
        AddOccupiedFoods(candidates, FindObjectsOfType<GrillStation>());
        return candidates.Count > 0;
    }

    private bool TryStartTheft()
    {
        if (!CanRunTheft() || targetedFood != null || pawMoveCoroutine != null) return false;

        List<FoodInstance> candidates = new List<FoodInstance>();
        AddOccupiedFoods(candidates, FindObjectsOfType<SeasoningStation>());
        AddOccupiedFoods(candidates, FindObjectsOfType<GrillStation>());

        if (candidates.Count == 0) return false;

        targetedFood = candidates[Random.Range(0, candidates.Count)];
        targetedFood.SetBeingStolen(true);
        SelectRandomCatPaw();

        if (catPaw != null)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowCatWarningBubble();

            Vector3 targetPosition;
            if (targetedFood.currentGrill != null && targetedFood.currentGrill.TryGetSlotPosition(targetedFood, out targetPosition)
                || targetedFood.currentSeasoning != null && targetedFood.currentSeasoning.TryGetSlotPosition(targetedFood, out targetPosition))
            {
                if (pawMoveCoroutine != null) StopCoroutine(pawMoveCoroutine);
                catPaw.position = GetPawEntryPosition();
                SetPawVisible(true);
                pawMoveCoroutine = StartCoroutine(MovePawTo(targetPosition));
            }
        }

        SetPawVisible(true);
        return true;
    }

    private void AddOccupiedFoods(List<FoodInstance> candidates, SeasoningStation[] stations)
    {
        foreach (SeasoningStation station in stations)
        {
            if (station == null) continue;
            AddOccupiedFoods(candidates, station.FoodsOnSlots);
        }
    }

    private void AddOccupiedFoods(List<FoodInstance> candidates, GrillStation[] grills)
    {
        foreach (GrillStation grill in grills)
        {
            if (grill == null) continue;
            AddOccupiedFoods(candidates, grill.FoodsOnSlots);
        }
    }

    private static void AddOccupiedFoods(List<FoodInstance> candidates, FoodInstance[] foods)
    {
        foreach (FoodInstance food in foods)
        {
            if (food != null && !candidates.Contains(food) && !food.IsBeingStolen)
                candidates.Add(food);
        }
    }

    private void CancelTheft()
    {
        if (pawMoveCoroutine != null)
        {
            StopCoroutine(pawMoveCoroutine);
            pawMoveCoroutine = null;
        }

        if (targetedFood != null) targetedFood.SetBeingStolen(false);
        targetedFood = null;
        ResetPawToEntryPosition();
        HidePaw();
    }

    private void CompleteTheft()
    {
        if (!CanRunTheft()) return;

        FoodInstance stolenFood = targetedFood;
        SoundManager.Instance.PlaySFX(SoundIds.CatTheftSuccess, GetPawSoundPosition());
        if (stolenFood.currentGrill != null) stolenFood.currentGrill.RemoveFood(stolenFood);
        if (stolenFood.currentSeasoning != null) stolenFood.currentSeasoning.RemoveFood(stolenFood);
        targetedFood = null;

        if (pawMoveCoroutine != null) StopCoroutine(pawMoveCoroutine);
        pawMoveCoroutine = StartCoroutine(CarryFoodDownAndFinish(stolenFood));
    }

    private IEnumerator MovePawTo(Vector3 targetPosition)
    {
        Vector3 startPosition = catPaw.position;
        float elapsed = 0f;

        while (elapsed < pawMoveDuration)
        {
            elapsed += Time.deltaTime;
            catPaw.position = Vector3.Lerp(startPosition, targetPosition, Mathf.Clamp01(elapsed / pawMoveDuration));
            yield return null;
        }

        catPaw.position = targetPosition;
        pawMoveCoroutine = null;
    }

    private IEnumerator CarryFoodDownAndFinish(FoodInstance stolenFood)
    {
        Vector3 pawStartPosition = catPaw.position;
        Vector3 foodStartPosition = stolenFood.Position;
        Vector3 exitPosition = GetPawExitPosition();
        float elapsed = 0f;

        while (elapsed < pawMoveDuration)
        {
            if (!CanRunTheft())
            {
                pawMoveCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pawMoveDuration);
            catPaw.position = Vector3.Lerp(pawStartPosition, exitPosition, t);
            stolenFood.Position = Vector3.Lerp(foodStartPosition, exitPosition, t);
            yield return null;
        }

        catPaw.position = exitPosition;
        stolenFood.Position = exitPosition;
        pawMoveCoroutine = null;
        if (!CanRunTheft()) yield break;

        HidePaw();
        stolenFood.SetBeingStolen(false);
        Destroy(stolenFood.RootObject);
    }

    private IEnumerator CarryTreatDownAndFinish(CatTreat treat)
    {
        Vector3 startPosition = catPaw.position;
        Vector3 exitPosition = GetPawExitPosition();
        float elapsed = 0f;

        while (elapsed < treatLeaveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / treatLeaveDuration);
            catPaw.position = Vector3.Lerp(startPosition, exitPosition, t);
            yield return null;
        }

        catPaw.position = exitPosition;
        pawMoveCoroutine = null;
        HidePaw();
        Destroy(treat.gameObject);
    }

    private Vector3 GetPawEntryPosition()
    {
        return pawEntryPoint != null ? pawEntryPoint.position : GetPawExitPosition();
    }

    private Vector3 GetPawExitPosition()
    {
        if (pawExitPoint != null) return pawExitPoint.position;
        if (pawEntryPoint != null) return pawEntryPoint.position;
        return catPaw != null ? catPaw.position : Vector3.zero;
    }

    private void ResetPawToEntryPosition()
    {
        if (catPaw != null) catPaw.position = GetPawEntryPosition();
    }

    private void SetPawVisible(bool visible)
    {
        if (isPawVisible == visible) return;

        isPawVisible = visible;
        if (catPawRenderer != null) catPawRenderer.enabled = visible;
        else if (catPaw != null) catPaw.gameObject.SetActive(visible);

        if (CanRunTheft())
            SoundManager.Instance.PlaySFX(visible ? SoundIds.CatAppear : SoundIds.CatDisappear, GetPawSoundPosition());
    }

    private void HidePaw()
    {
        SetPawVisible(false);
    }

    private Vector3 GetPawSoundPosition()
    {
        return catPaw != null ? catPaw.position : transform.position;
    }
}