using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Displays credits pages with a simple paper-like page turn animation.
// Put this component on the credits panel or a child object.
//
// Page layout convention (spread = 2 pages):
//   pages[0] = left,  pages[1] = right   (spread 0)
//   pages[2] = left,  pages[3] = right   (spread 1)
//   ...
//
// Page turn roles (identical for both directions, only the SIDE is mirrored):
//   turning page    = the page of the OLD spread that physically moves
//                     forward  -> old RIGHT page (binding at its left edge)
//                     backward -> old LEFT  page (binding at its right edge)
//   under page      = the page of the NEW spread on the same side as the turning page.
//                     It is uncovered while the turning page folds away.
//   stationary page = the page of the OLD spread on the opposite side. It stays put.
//   revealed page   = the page of the NEW spread on the opposite side.
//                     It grows out of the spine and must be drawn ABOVE the stationary page.
//
// Required draw order (bottom -> top), the same for both directions:
//   under / stationary pages  <  revealed page  <  turning page
public class CreditsBookUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("หน้าหนังสือ")]
    [Tooltip("ลากหน้าเครดิตมาเรียงตามลำดับ เช่น หน้า 1, 2, 3, 4")]
    [SerializeField] private GameObject[] pages;

    [Header("ปุ่มเปลี่ยนหน้า")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private bool wrapPages;

    [Header("แอนิเมชันพลิกกระดาษ")]
    [SerializeField] private float pageTurnDuration = 0.45f;
    [SerializeField] private AnimationCurve pageTurnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0f, 0.45f)] private float pageFoldWidth = 0.08f;
    [SerializeField, Range(0f, 12f)] private float pageBendAngle = 4f;
    [SerializeField, Range(0f, 0.08f)] private float pageLift = 0.015f;
    [SerializeField, Range(0.4f, 0.95f)] private float revealStart = 0.8f;
    [SerializeField, Range(0.7f, 1f)] private float revealEnd = 0.98f;
    [SerializeField] private float swipeThreshold = 60f;

    // Index of a page inside a spread.
    private const int LeftSide = 0;
    private const int RightSide = 1;

    // Sorting offsets above the highest canvas used by any page.
    // Revealed page must always be below the turning page.
    private const int RevealedSortingOffset = 500;
    private const int TurningSortingOffset = 1000;

    private int currentSpreadIndex;
    private Coroutine pageTurnAnimation;
    private bool isTurningPage;
    private Vector2 dragStartPosition;

    // Everything that must be undone when a page turn finishes OR is interrupted.
    private PageTurnState activeTurn;

    private void Awake()
    {
        DetachNavigationFromPages(previousButton);
        DetachNavigationFromPages(nextButton);

        if (previousButton != null) previousButton.onClick.AddListener(ShowPreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextPage);
    }

    private void OnEnable()
    {
        Open();
    }

    public void Open()
    {
        StopPageTurnAnimation();
        currentSpreadIndex = 0;
        SetAllPagesInactive();
        ResetPageVisualStates();

        if (pages == null || pages.Length == 0)
        {
            SetNavigationInteractable(false, false);
            return;
        }

        SetSpreadActive(currentSpreadIndex, true);
        RefreshNavigation();
        ClearNavigationSelection();
    }

    public void ShowNextPage()
    {
        ShowSpread(currentSpreadIndex + 1);
    }

    public void ShowPreviousPage()
    {
        ShowSpread(currentSpreadIndex - 1);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float horizontalDistance = eventData.position.x - dragStartPosition.x;
        if (Mathf.Abs(horizontalDistance) < Mathf.Max(1f, swipeThreshold))
        {
            return;
        }

        if (horizontalDistance < 0f)
        {
            ShowNextPage();
        }
        else
        {
            ShowPreviousPage();
        }
    }

    private void ShowSpread(int requestedIndex)
    {
        if (isTurningPage || pages == null || pages.Length < 2) return;

        int spreadCount = GetSpreadCount();
        int nextIndex;
        if (wrapPages)
        {
            nextIndex = (requestedIndex % spreadCount + spreadCount) % spreadCount;
        }
        else
        {
            nextIndex = Mathf.Clamp(requestedIndex, 0, spreadCount - 1);
        }

        if (nextIndex == currentSpreadIndex)
        {
            RefreshNavigation();
            return;
        }

        pageTurnAnimation = StartCoroutine(AnimatePageTurn(nextIndex, nextIndex > currentSpreadIndex));
    }

    private IEnumerator AnimatePageTurn(int nextIndex, bool turningForward)
    {
        isTurningPage = true;

        GameObject[] outgoingPages = GetSpreadPages(currentSpreadIndex);
        GameObject[] incomingPages = GetSpreadPages(nextIndex);

        // Forward : the old RIGHT page turns over and the new LEFT page is revealed.
        // Backward: the old LEFT  page turns over and the new RIGHT page is revealed.
        // The revealed page is always on the opposite side of the turning page.
        int turningSide = turningForward ? RightSide : LeftSide;
        int revealedSide = 1 - turningSide;

        GameObject turningPage = outgoingPages[turningSide];
        GameObject revealedPage = incomingPages[revealedSide];
        RectTransform turningPageTransform = turningPage == null
            ? null
            : turningPage.GetComponent<RectTransform>();
        RectTransform revealedPageTransform = revealedPage == null
            ? null
            : revealedPage.GetComponent<RectTransform>();

        if (turningPageTransform == null || revealedPageTransform == null)
        {
            SetSpreadActive(currentSpreadIndex, false);
            SetSpreadActive(nextIndex, true);
            currentSpreadIndex = nextIndex;
            isTurningPage = false;
            pageTurnAnimation = null;
            RefreshNavigation();
            yield break;
        }

        SetSpreadActive(nextIndex, true);

        CanvasGroup turningPageGroup = GetOrAddCanvasGroup(turningPage);
        CanvasGroup revealedPageGroup = GetOrAddCanvasGroup(revealedPage);
        CanvasGroup[] outgoingGroups = GetCanvasGroups(outgoingPages);
        CanvasGroup[] incomingGroups = GetCanvasGroups(incomingPages);

        // Spine position inside each page (0 = left edge, 1 = right edge).
        float turningBindingX = turningForward ? 0f : 1f;
        float revealedBindingX = turningForward ? 1f : 0f;
        float bendDirection = turningForward ? -1f : 1f;

        // Remember everything we are going to touch so it can be restored,
        // even if the animation is interrupted (Open / OnDisable).
        PageTurnState turn = new PageTurnState
        {
            TurningRect = turningPageTransform,
            TurningPosition = turningPageTransform.localPosition,
            TurningScale = turningPageTransform.localScale,
            TurningRotation = turningPageTransform.localRotation,
            TurningSiblingIndex = turningPageTransform.GetSiblingIndex(),
            RevealedRect = revealedPageTransform,
            RevealedPosition = revealedPageTransform.localPosition,
            RevealedScale = revealedPageTransform.localScale,
            RevealedRotation = revealedPageTransform.localRotation,
            Groups = CombineGroups(outgoingGroups, incomingGroups)
        };
        activeTurn = turn;

        // ---- Layering (same logic for both directions) ----
        // Do NOT depend on sibling order: sibling order is only correct for one direction.
        // Both moving pages get an explicit sorting order above every page canvas:
        //   revealed page = base + 500   (above the stationary page it grows over)
        //   turning page  = base + 1000  (above the under page it uncovers + the revealed page)
        Canvas parentCanvas = turningPageTransform.parent != null
            ? turningPageTransform.parent.GetComponentInParent<Canvas>()
            : null;
        int sortingLayerId = parentCanvas != null ? parentCanvas.sortingLayerID : 0;
        int baseSortingOrder = GetHighestPageSortingOrder(parentCanvas);

        turn.RevealedLayer = new PageLayer(
            revealedPage, sortingLayerId, baseSortingOrder + RevealedSortingOffset);
        turn.TurningLayer = new PageLayer(
            turningPage, sortingLayerId, baseSortingOrder + TurningSortingOffset);

        // ---- Visual state ----
        // Every page stays fully opaque. The stationary page is NOT hidden anymore:
        // it is covered by the revealed page, exactly like in a forward turn.
        SetAlphas(outgoingGroups, 1f);
        SetAlphas(incomingGroups, 1f);
        SetRaycastBlocking(outgoingGroups, false);
        SetRaycastBlocking(incomingGroups, false);
        turningPageGroup.alpha = 1f;
        revealedPageGroup.alpha = 0f;

        // Revealed page starts as a thin strip at the spine.
        ApplyPageTurnPose(
            revealedPageTransform,
            turn.RevealedScale,
            turn.RevealedRotation,
            revealedBindingX,
            pageFoldWidth,
            0f,
            0f);

        // Extra tie-breaker inside the hierarchy; the real ordering comes from the canvases.
        turningPageTransform.SetAsLastSibling();

        float duration = Mathf.Max(0.01f, pageTurnDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = pageTurnCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            float foldProgress = Mathf.SmoothStep(0f, 1f, progress);
            float outgoingWidth = Mathf.Lerp(1f, pageFoldWidth, foldProgress);
            float bend = Mathf.Sin(foldProgress * Mathf.PI) * pageBendAngle;
            float lift = Mathf.Sin(foldProgress * Mathf.PI) * pageLift;

            ApplyPageTurnPose(
                turningPageTransform,
                turn.TurningScale,
                turn.TurningRotation,
                turningBindingX,
                outgoingWidth,
                bendDirection * bend,
                lift);
            ApplyPageTurnPose(
                revealedPageTransform,
                turn.RevealedScale,
                turn.RevealedRotation,
                revealedBindingX,
                Mathf.Lerp(pageFoldWidth, 1f, foldProgress),
                -bendDirection * bend,
                lift);

            float revealProgress = EvaluateRevealProgress(
                foldProgress, revealStart, revealEnd);
            revealedPageGroup.alpha = revealProgress;
            turningPageGroup.alpha = 1f - revealProgress;
            yield return null;
        }

        // Restore poses, sorting, alphas and raycasts, then swap the visible spread.
        RestoreActiveTurn();
        SetSpreadActive(currentSpreadIndex, false);
        currentSpreadIndex = nextIndex;
        isTurningPage = false;
        pageTurnAnimation = null;
        RefreshNavigation();

        if (SoundManager.Exists)
        {
            SoundManager.Instance.PlaySFX(SoundIds.UiPageTurn);
        }
    }

    private void ResetPageVisualStates()
    {
        if (pages == null) return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null) continue;

            CanvasGroup group = GetOrAddCanvasGroup(pages[i]);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private float EvaluateRevealProgress(float progress, float start, float end)
    {
        start = Mathf.Clamp01(start);
        end = Mathf.Clamp01(end);
        if (end <= start)
        {
            end = Mathf.Min(1f, start + 0.01f);
        }

        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, progress));
    }

    private void RefreshNavigation()
    {
        int spreadCount = GetSpreadCount();
        bool canGoPrevious = wrapPages ? spreadCount > 1 : currentSpreadIndex > 0;
        bool canGoNext = wrapPages ? spreadCount > 1 : currentSpreadIndex < spreadCount - 1;
        SetNavigationInteractable(canGoPrevious, canGoNext);
        ClearNavigationSelection();
    }

    private void SetNavigationInteractable(bool previousInteractable, bool nextInteractable)
    {
        if (previousButton != null) previousButton.interactable = previousInteractable;
        if (nextButton != null) nextButton.interactable = nextInteractable;
    }

    private void SetAllPagesInactive()
    {
        if (pages == null) return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null) pages[i].SetActive(false);
        }
    }

    private int GetSpreadCount()
    {
        return pages == null ? 0 : Mathf.CeilToInt(pages.Length / 2f);
    }

    private GameObject[] GetSpreadPages(int spreadIndex)
    {
        int firstPageIndex = spreadIndex * 2;
        return new[]
        {
            firstPageIndex < pages.Length ? pages[firstPageIndex] : null,
            firstPageIndex + 1 < pages.Length ? pages[firstPageIndex + 1] : null
        };
    }

    private void SetSpreadActive(int spreadIndex, bool isActive)
    {
        GameObject[] spreadPages = GetSpreadPages(spreadIndex);
        for (int i = 0; i < spreadPages.Length; i++)
        {
            if (spreadPages[i] != null) spreadPages[i].SetActive(isActive);
        }
    }

    private CanvasGroup[] GetCanvasGroups(GameObject[] spreadPages)
    {
        return new[]
        {
            spreadPages[0] == null ? null : GetOrAddCanvasGroup(spreadPages[0]),
            spreadPages[1] == null ? null : GetOrAddCanvasGroup(spreadPages[1])
        };
    }

    private CanvasGroup[] CombineGroups(CanvasGroup[] first, CanvasGroup[] second)
    {
        CanvasGroup[] combined = new CanvasGroup[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    private void SetAlphas(CanvasGroup[] groups, float alpha)
    {
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null) groups[i].alpha = alpha;
        }
    }

    private void SetRaycastBlocking(CanvasGroup[] groups, bool blocksRaycasts)
    {
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null) groups[i].blocksRaycasts = blocksRaycasts;
        }
    }

    // Highest sorting order of the parent canvas and of every canvas found inside any page.
    private int GetHighestPageSortingOrder(Canvas fallbackCanvas)
    {
        int highestSortingOrder = fallbackCanvas != null
            ? fallbackCanvas.sortingOrder
            : 0;

        if (pages == null) return highestSortingOrder;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null) continue;

            Canvas[] pageCanvases = pages[i].GetComponentsInChildren<Canvas>(true);
            for (int j = 0; j < pageCanvases.Length; j++)
            {
                highestSortingOrder = Mathf.Max(highestSortingOrder, pageCanvases[j].sortingOrder);
            }
        }

        return highestSortingOrder;
    }

    private void RestoreActiveTurn()
    {
        if (activeTurn == null) return;

        PageTurnState turn = activeTurn;
        activeTurn = null;
        turn.Restore();
    }

    // Stores the original state of one canvas so it can be restored later.
    private struct PageCanvasState
    {
        public readonly Canvas Canvas;
        public readonly bool Enabled;
        public readonly bool OverrideSorting;
        public readonly int SortingLayerId;
        public readonly int SortingOrder;

        public PageCanvasState(
            Canvas canvas,
            bool enabled,
            bool overrideSorting,
            int sortingLayerId,
            int sortingOrder)
        {
            Canvas = canvas;
            Enabled = enabled;
            OverrideSorting = overrideSorting;
            SortingLayerId = sortingLayerId;
            SortingOrder = sortingOrder;
        }

        public void Apply()
        {
            if (Canvas == null) return;

            Canvas.enabled = Enabled;
            Canvas.overrideSorting = OverrideSorting;
            Canvas.sortingLayerID = SortingLayerId;
            Canvas.sortingOrder = SortingOrder;
        }
    }

    // Temporarily gives a page an explicit sorting order (adds a Canvas if it has none)
    // and puts every nested canvas inside it just above the page itself, so the page's
    // content can never end up behind another page.
    private sealed class PageLayer
    {
        private readonly Canvas rootCanvas;
        private readonly bool addedRootCanvas;
        private readonly PageCanvasState rootState;
        private readonly PageCanvasState[] childStates;

        public PageLayer(GameObject page, int sortingLayerId, int sortingOrder)
        {
            rootCanvas = page.GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = page.AddComponent<Canvas>();
                addedRootCanvas = true;
            }

            rootState = new PageCanvasState(
                rootCanvas,
                rootCanvas.enabled,
                rootCanvas.overrideSorting,
                rootCanvas.sortingLayerID,
                rootCanvas.sortingOrder);

            Canvas[] allCanvases = page.GetComponentsInChildren<Canvas>(true);
            List<PageCanvasState> children = new List<PageCanvasState>();
            for (int i = 0; i < allCanvases.Length; i++)
            {
                Canvas canvas = allCanvases[i];
                if (canvas == rootCanvas) continue;

                children.Add(new PageCanvasState(
                    canvas,
                    canvas.enabled,
                    canvas.overrideSorting,
                    canvas.sortingLayerID,
                    canvas.sortingOrder));
                canvas.overrideSorting = true;
                canvas.sortingLayerID = sortingLayerId;
                canvas.sortingOrder = sortingOrder + 1;
            }

            childStates = children.ToArray();

            rootCanvas.enabled = true;
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingLayerID = sortingLayerId;
            rootCanvas.sortingOrder = sortingOrder;
        }

        public void Restore()
        {
            for (int i = 0; i < childStates.Length; i++)
            {
                childStates[i].Apply();
            }

            if (rootCanvas == null) return;

            if (addedRootCanvas)
            {
                UnityEngine.Object.Destroy(rootCanvas);
                return;
            }

            rootState.Apply();
        }
    }

    // Snapshot of everything one page turn modifies.
    private sealed class PageTurnState
    {
        public RectTransform TurningRect;
        public Vector3 TurningPosition;
        public Vector3 TurningScale;
        public Quaternion TurningRotation;
        public int TurningSiblingIndex;

        public RectTransform RevealedRect;
        public Vector3 RevealedPosition;
        public Vector3 RevealedScale;
        public Quaternion RevealedRotation;

        public PageLayer TurningLayer;
        public PageLayer RevealedLayer;
        public CanvasGroup[] Groups;

        public void Restore()
        {
            if (TurningRect != null)
            {
                TurningRect.localPosition = TurningPosition;
                TurningRect.localScale = TurningScale;
                TurningRect.localRotation = TurningRotation;
                TurningRect.SetSiblingIndex(TurningSiblingIndex);
            }

            if (RevealedRect != null)
            {
                RevealedRect.localPosition = RevealedPosition;
                RevealedRect.localScale = RevealedScale;
                RevealedRect.localRotation = RevealedRotation;
            }

            if (TurningLayer != null) TurningLayer.Restore();
            if (RevealedLayer != null) RevealedLayer.Restore();

            if (Groups == null) return;

            for (int i = 0; i < Groups.Length; i++)
            {
                if (Groups[i] == null) continue;

                Groups[i].alpha = 1f;
                Groups[i].blocksRaycasts = true;
            }
        }
    }

    // Scales / rotates a page while keeping its binding (spine) edge fixed in world space.
    private void ApplyPageTurnPose(
        RectTransform rectTransform,
        Vector3 originalScale,
        Quaternion originalRotation,
        float bindingX,
        float widthScale,
        float bendAngle,
        float lift)
    {
        Vector3 bindingPointBeforeScale = GetBindingPointWorld(rectTransform, bindingX);
        rectTransform.localScale = new Vector3(
            originalScale.x * widthScale,
            originalScale.y * (1f + lift),
            originalScale.z);
        rectTransform.localRotation =
            originalRotation * Quaternion.Euler(0f, 0f, bendAngle);
        Vector3 bindingPointAfterScale = GetBindingPointWorld(rectTransform, bindingX);
        rectTransform.position += bindingPointBeforeScale - bindingPointAfterScale;
    }

    private Vector3 GetBindingPointWorld(RectTransform rectTransform, float normalizedX)
    {
        Vector2 localPoint = new Vector2(
            (normalizedX - rectTransform.pivot.x) * rectTransform.rect.width,
            (0.5f - rectTransform.pivot.y) * rectTransform.rect.height);
        return rectTransform.TransformPoint(localPoint);
    }

    private void DetachNavigationFromPages(Button navigationButton)
    {
        if (navigationButton == null || navigationButton.transform == transform || pages == null)
        {
            return;
        }

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null || !navigationButton.transform.IsChildOf(pages[i].transform))
            {
                continue;
            }

            navigationButton.transform.SetParent(transform, true);
            Debug.LogWarning(
                "[CreditsBookUI] A navigation button was inside a page and was moved to the book root.",
                navigationButton);
            return;
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject page)
    {
        CanvasGroup group = page.GetComponent<CanvasGroup>();
        return group != null ? group : page.AddComponent<CanvasGroup>();
    }

    private void StopPageTurnAnimation()
    {
        if (pageTurnAnimation != null)
        {
            StopCoroutine(pageTurnAnimation);
            pageTurnAnimation = null;
        }

        // If a turn was interrupted, undo its temporary canvases, poses and alphas.
        RestoreActiveTurn();
        isTurningPage = false;
    }

    private void ClearNavigationSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnDisable()
    {
        StopPageTurnAnimation();
    }

    private void OnDestroy()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(ShowPreviousPage);
        if (nextButton != null) nextButton.onClick.RemoveListener(ShowNextPage);
    }
}