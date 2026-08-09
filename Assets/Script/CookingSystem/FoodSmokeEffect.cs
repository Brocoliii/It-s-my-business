using UnityEngine;

// ควันที่ลอยขึ้นจากตัววัตถุดิบตอนวางอยู่บนเตา
// ยิ่งสุกมาก ควันยิ่งเยอะและสีเข้มขึ้น พอไหม้จะกลายเป็นควันดำพวยพุ่ง
// ถ้าไม่ได้ลาก prefab ควันมาใส่ จะสร้างควันแบบง่ายๆ ให้เองตอนรัน (ไม่ต้องเซ็ตอะไรเพิ่ม)
[DisallowMultipleComponent]
public class FoodSmokeEffect : MonoBehaviour
{
    public enum SmokeStage { None, Raw, Medium, Cooked, Burnt }

    [Header("อ้างอิง")]
    [SerializeField] private FoodInstance food;
    [SerializeField] private ParticleSystem smokePrefab;
    [SerializeField] private Vector3 smokeOffset = new Vector3(0f, 0.2f, 0f);

    [Header("ชั้นการวาด (ให้ควันอยู่หน้าตัวอาหาร)")]
    [SerializeField] private string sortingLayerName = "Food";
    [SerializeField] private int sortingOrder = 50;

    [Header("ปริมาณควันแต่ละระดับความสุก (ชิ้น/วินาที)")]
    [SerializeField] private float rawRate = 3f;
    [SerializeField] private float mediumRate = 7f;
    [SerializeField] private float cookedRate = 13f;
    [SerializeField] private float burntRate = 26f;

    [Header("สีควันแต่ละระดับความสุก")]
    [SerializeField] private Color rawColor = new Color(1f, 1f, 1f, 0.30f);
    [SerializeField] private Color mediumColor = new Color(0.96f, 0.94f, 0.90f, 0.45f);
    [SerializeField] private Color cookedColor = new Color(0.80f, 0.78f, 0.74f, 0.60f);
    [SerializeField] private Color burntColor = new Color(0.22f, 0.21f, 0.20f, 0.85f);

    [Header("การลอยขึ้น")]
    [SerializeField] private float riseSpeed = 0.6f;
    [SerializeField] private float burntRiseSpeed = 1.2f;
    [SerializeField] private float smokeSize = 0.35f;
    [SerializeField] private float sideDrift = 0.12f;

    private ParticleSystem smoke;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private SmokeStage currentStage = SmokeStage.None;
    private bool builtAtRuntime;

    // texture/material ของควันใช้ร่วมกันได้ทุกชิ้น ไม่ต้องสร้างใหม่ทุกครั้งที่หยิบวัตถุดิบ
    private static Texture2D sharedPuffTexture;
    private static Material sharedPuffMaterial;

    private void Awake()
    {
        if (food == null) food = GetComponentInParent<FoodInstance>();
    }

    public void SetFood(FoodInstance instance)
    {
        food = instance;
    }

    private void Update()
    {
        SmokeStage stage = EvaluateStage();
        if (stage == currentStage) return;

        currentStage = stage;
        ApplyStage(stage);
    }

    private SmokeStage EvaluateStage()
    {
        if (food == null || !food.IsOnGrill) return SmokeStage.None;

        FoodData data = food.GetData();
        if (data == null) return SmokeStage.None;

        // ใช้เวลาของ "ด้านที่แนบเตาอยู่ตอนนี้" เพราะควันมาจากด้านที่โดนความร้อนจริงๆ
        float timer = food.CurrentCookTimer;

        if (timer >= data.burnTime) return SmokeStage.Burnt;
        if (timer >= data.cookTime) return SmokeStage.Cooked;
        if (timer >= data.mediumTime) return SmokeStage.Medium;
        return SmokeStage.Raw;
    }

    private void ApplyStage(SmokeStage stage)
    {
        if (stage == SmokeStage.None)
        {
            // หยุดพ่นเฉยๆ ไม่ล้างทิ้ง ควันที่ค้างอยู่จะได้จางหายไปเองแบบธรรมชาติ
            if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            return;
        }

        if (!EnsureSmoke()) return;

        emissionModule.rateOverTime = GetRate(stage);
        mainModule.startColor = GetColor(stage);
        mainModule.startSize = new ParticleSystem.MinMaxCurve(smokeSize * 0.7f, smokeSize * (stage == SmokeStage.Burnt ? 1.6f : 1.2f));

        // ถ้าเป็นควันที่ผู้เล่นทำ prefab มาเอง ไม่ไปยุ่งกับความเร็วของเขา
        // (แก้แกนเดียวแล้วโหมดไม่ตรงกับแกนอื่น = Unity ด่าไม่หยุด)
        if (builtAtRuntime)
        {
            float rise = stage == SmokeStage.Burnt ? burntRiseSpeed : riseSpeed;
            velocityModule.x = new ParticleSystem.MinMaxCurve(-sideDrift, sideDrift);
            velocityModule.y = new ParticleSystem.MinMaxCurve(rise * 0.7f, rise);
            velocityModule.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        if (!smoke.isEmitting) smoke.Play();
    }

    private float GetRate(SmokeStage stage)
    {
        switch (stage)
        {
            case SmokeStage.Medium: return mediumRate;
            case SmokeStage.Cooked: return cookedRate;
            case SmokeStage.Burnt: return burntRate;
            default: return rawRate;
        }
    }

    private Color GetColor(SmokeStage stage)
    {
        switch (stage)
        {
            case SmokeStage.Medium: return mediumColor;
            case SmokeStage.Cooked: return cookedColor;
            case SmokeStage.Burnt: return burntColor;
            default: return rawColor;
        }
    }

    private bool EnsureSmoke()
    {
        if (smoke != null) return true;

        Transform parent = food != null ? food.FoodRoot : transform;

        if (smokePrefab != null)
        {
            smoke = Instantiate(smokePrefab, parent);
            smoke.transform.localPosition = smokeOffset;
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            builtAtRuntime = false;
        }
        else
        {
            smoke = BuildRuntimeSmoke(parent);
            builtAtRuntime = true;
        }

        if (smoke == null) return false;

        mainModule = smoke.main;
        emissionModule = smoke.emission;
        velocityModule = smoke.velocityOverLifetime;

        ApplySorting(smoke.GetComponent<ParticleSystemRenderer>());
        return true;
    }

    private ParticleSystem BuildRuntimeSmoke(Transform parent)
    {
        GameObject go = new GameObject("GrillSmoke");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = smokeOffset;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        // ParticleSystem ที่ AddComponent มาจะเล่นเองทันที ต้องสั่งหยุดก่อนตั้งค่า
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        main.startSpeed = 0f;                 // ความเร็วจริงไปคุมที่ velocityOverLifetime แทน
        main.startSize = smokeSize;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = rawColor;
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // Shape = ให้ขนาดควันไม่โดนสเกลของตัวอาหาร (ตอนพลิกอาหาร scale.x จะถูกบีบจนแบน)
        main.scalingMode = ParticleSystemScalingMode.Shape;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rawRate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        // ต้องเซ็ตครบทั้ง 3 แกนให้เป็นโหมดเดียวกัน (TwoConstants)
        // ถ้าแกนไหนโหมดไม่ตรงกัน Unity จะขึ้น error "Particle Velocity curves must all be in the same mode" รัวๆ ทุกเฟรม
        velocity.x = new ParticleSystem.MinMaxCurve(-sideDrift, sideDrift);
        velocity.y = new ParticleSystem.MinMaxCurve(riseSpeed * 0.7f, riseSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // ส่ายไปมาเล็กน้อยระหว่างลอยขึ้น ควันจะได้ไม่เป็นเส้นตรงแข็งๆ
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.4f;
        noise.damping = true;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve growCurve = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(1f, 1.5f));
        size.size = new ParticleSystem.MinMaxCurve(1f, growCurve);

        // จางเข้า-จางออก ควันจะได้ไม่โผล่มาแล้วหายวับ
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
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.material = GetPuffMaterial();

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

    private static Material GetPuffMaterial()
    {
        if (sharedPuffMaterial != null) return sharedPuffMaterial;

        // เผื่อโปรเจกต์ใช้ pipeline ต่างกัน ไล่หา shader ที่มีจริงทีละตัว
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
        {
            Debug.LogWarning("FoodSmokeEffect: หา shader สำหรับควันไม่เจอ ลากอ prefab ควันมาใส่ช่อง Smoke Prefab แทน");
            return null;
        }

        sharedPuffMaterial = new Material(shader) { name = "GrillSmoke (Runtime)" };
        sharedPuffMaterial.mainTexture = GetPuffTexture();
        return sharedPuffMaterial;
    }

    // วาดวงกลมฟุ้งๆ ขอบจาง ไว้ใช้เป็นรูปควันหนึ่งก้อน
    private static Texture2D GetPuffTexture()
    {
        if (sharedPuffTexture != null) return sharedPuffTexture;

        const int size = 64;
        sharedPuffTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "GrillSmokePuff (Runtime)",
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
                alpha *= alpha;   // ยกกำลังสองให้ขอบนุ่มขึ้น ตรงกลางทึบกว่าขอบ
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        sharedPuffTexture.SetPixels(pixels);
        sharedPuffTexture.Apply();
        return sharedPuffTexture;
    }
}
