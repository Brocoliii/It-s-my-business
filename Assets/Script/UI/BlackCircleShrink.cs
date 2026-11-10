
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class BlackCircleShrink : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float startSize = 3000f;

    [SerializeField]
    private AnimationCurve shrinkCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    private RectTransform rectTransform;

    private IEnumerator Start()
    {
        rectTransform = GetComponent<RectTransform>();

        rectTransform.sizeDelta = Vector2.one * startSize;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);

            // Curve controls the shrinking speed
            float curveValue = shrinkCurve.Evaluate(t);

            float size = Mathf.Lerp(startSize, 0f, curveValue);

            rectTransform.sizeDelta = Vector2.one * size;

            yield return null;
        }

        rectTransform.sizeDelta = Vector2.zero;
        gameObject.SetActive(false);
    }
}
