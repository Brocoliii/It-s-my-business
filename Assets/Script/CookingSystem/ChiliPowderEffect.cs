using UnityEngine;

// ผงพริกที่พ่นออกจากขวดตอนเขย่า แต่ละครั้งที่เขย่าจะสร้างผงชุดใหม่ค้างไว้ตรงจุดนั้นเลย
// ไม่ผูกติดกับขวด ขวดลากไปที่อื่นต่อ ผงที่พ่นไปแล้วจะไม่ลอยตามไปด้วย
// ถ้าไม่ได้ลาก prefab ผงมาใส่ จะสร้างผงแบบง่ายๆ ให้เองตอนรัน (ไม่ต้องเซ็ตอะไรเพิ่ม)
[DisallowMultipleComponent]
public class ChiliPowderEffect : MonoBehaviour
{
    [Header("อ้างอิง")]
    [SerializeField] private ParticleSystem powderPrefab;
    [SerializeField] private Vector3 powderOffset = new Vector3(0f, -0.15f, 0f);

    [Header("ชั้นการวาด (ให้ผงอยู่หน้าขวด)")]
    [SerializeField] private string sortingLayerName = "Dragging";
    [SerializeField] private int sortingOrder = 10;

    [Header("ปริมาณผงพริก")]
    [SerializeField] private int shakeCount = 6;
    [SerializeField] private int burstCount = 18;
    [SerializeField] private Color powderColor = new Color(0.80f, 0.22f, 0.05f, 0.9f);
    [SerializeField] private float powderSize = 0.08f;
    [SerializeField] private float fallSpeed = 0.8f;
    [SerializeField] private float sideDrift = 0.25f;

    // texture/material ของผงใช้ร่วมกันได้ทุกขวด ไม่ต้องสร้างใหม่ทุกครั้ง
    private static Texture2D sharedSpeckTexture;
    private static Material sharedSpeckMaterial;

    // เรียกทุกครั้งที่นับว่าเขย่าได้หนึ่งจังหวะ (มือกลับทิศ) จะพ่นผงชุดเล็กๆ ค้างไว้ตรงตำแหน่งขวดตอนนั้น
    public void SpawnPuff()
    {
        SpawnOneShot(shakeCount);
    }

    // พ่นผงชุดใหญ่ตอนโรยสำเร็จ ให้ความรู้สึกว่า "โรยลงไปแล้ว" ชัดกว่าจังหวะเขย่าปกติ
    public void Burst()
    {
        SpawnOneShot(burstCount);
    }

    // สร้างระบบอนุภาคใหม่แยกอิสระทุกครั้ง วางตำแหน่งครั้งเดียวแล้วปล่อยค้าง ไม่มีการอัปเดตตำแหน่งซ้ำ
    // เพื่อไม่ให้ผงที่พ่นไปแล้ว "ลอยตาม" เวลาขวดถูกลากไปที่อื่นต่อ
    private void SpawnOneShot(int count)
    {
        ParticleSystem instance = powderPrefab != null ? Instantiate(powderPrefab) : BuildRuntimePowder();
        if (instance == null) return;

        instance.transform.position = transform.position + powderOffset;
        instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ApplySorting(instance.GetComponent<ParticleSystemRenderer>());

        instance.Emit(count);
        Destroy(instance.gameObject, GetMaxLifetime(instance) + 0.1f);
    }

    private static float GetMaxLifetime(ParticleSystem ps)
    {
        ParticleSystem.MinMaxCurve curve = ps.main.startLifetime;
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return curve.constantMax;
            default:
                return 2f; // เผื่อไว้กรณี prefab ใช้ curve แบบอื่น
        }
    }

    private ParticleSystem BuildRuntimePowder()
    {
        GameObject go = new GameObject("ChiliPowder");

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        // ParticleSystem ที่ AddComponent มาจะเล่นเองทันที ต้องสั่งหยุดก่อนตั้งค่า
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startSpeed = 0f;                 // ความเร็วจริงไปคุมที่ velocityOverLifetime แทน
        main.startSize = new ParticleSystem.MinMaxCurve(powderSize * 0.6f, powderSize * 1.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = powderColor;
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f; // พ่นเฉพาะตอนสั่ง Emit(count) เป็นชุดๆ เท่านั้น

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.06f;
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // ต้องเซ็ตครบทั้ง 3 แกนให้เป็นโหมดเดียวกัน (TwoConstants)
        // ถ้าแกนไหนโหมดไม่ตรงกัน Unity จะขึ้น error "Particle Velocity curves must all be in the same mode" รัวๆ ทุกเฟรม
        velocity.x = new ParticleSystem.MinMaxCurve(-sideDrift, sideDrift);
        velocity.y = new ParticleSystem.MinMaxCurve(-fallSpeed, -fallSpeed * 0.6f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // สั่นเล็กน้อยระหว่างร่วงลง ผงจะได้ไม่เป็นเส้นตรงแข็งๆ
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.1f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.6f;
        noise.damping = true;

        // จางเข้า-จางออก ผงจะได้ไม่โผล่มาแล้วหายวับ
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = GetSpeckMaterial();

        return ps;
    }

    private void ApplySorting(ParticleSystemRenderer renderer)
    {
        if (renderer == null) return;

        if (!string.IsNullOrEmpty(sortingLayerName) && SortingLayerExists(sortingLayerName))
        {
            renderer.sortingLayerName = sortingLayerName;
        }
        renderer.sortingOrder = sortingOrder;
    }

    private static bool SortingLayerExists(string layerName)
    {
        foreach (SortingLayer layer in SortingLayer.layers)
        {
            if (layer.name == layerName) return true;
        }
        return false;
    }

    private static Material GetSpeckMaterial()
    {
        if (sharedSpeckMaterial != null) return sharedSpeckMaterial;

        // เผื่อโปรเจกต์ใช้ pipeline ต่างกัน ไล่หา shader ที่มีจริงทีละตัว
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
        {
            Debug.LogWarning("ChiliPowderEffect: หา shader สำหรับผงพริกไม่เจอ ลาก prefab ผงมาใส่ช่อง Powder Prefab แทน");
            return null;
        }

        sharedSpeckMaterial = new Material(shader) { name = "ChiliPowder (Runtime)" };
        sharedSpeckMaterial.mainTexture = GetSpeckTexture();
        return sharedSpeckMaterial;
    }

    // วาดจุดกลมๆ ทึบตรงกลาง ขอบจางนิดเดียว ไว้ใช้เป็นเม็ดผงพริกหนึ่งเม็ด
    private static Texture2D GetSpeckTexture()
    {
        if (sharedSpeckTexture != null) return sharedSpeckTexture;

        const int size = 16;
        sharedSpeckTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ChiliPowderSpeck (Runtime)",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.Pow(alpha, 0.6f); // ให้เม็ดดูทึบเกือบเต็มเม็ด ขอบคมกว่าฝุ่นควัน
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        sharedSpeckTexture.SetPixels(pixels);
        sharedSpeckTexture.Apply();
        return sharedSpeckTexture;
    }
}
