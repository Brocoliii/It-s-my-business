using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GrillLightFlicker : MonoBehaviour
{
    [SerializeField] private Light2D light2D;

    [Header("Intensity")]
    [SerializeField] private float minIntensity = 0.8f;
    [SerializeField] private float maxIntensity = 1.2f;

    [Header("Speed")]
    [SerializeField] private float flickerSpeed = 2f;

    private float noiseSeed;

    private void Start()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        noiseSeed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(
            noiseSeed,
            Time.time * flickerSpeed
        );

        light2D.intensity = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            noise
        );
    }
}