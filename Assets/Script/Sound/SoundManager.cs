using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

// ตัวจัดการเสียงกลางของทั้งเกม (SFX + BGM)
//
// วิธีใช้แบบสั้นที่สุด:
//   SoundManager.Instance.PlaySFX("ui_click");
//   SoundManager.Instance.PlayBGM("stage_1");
//
// ไม่ต้องวางอะไรในฉากก็ได้ ถ้าไม่เจอตัวจัดการในฉาก มันจะสร้างตัวเองขึ้นมาให้อัตโนมัติ
// โดยจะไปหา Prefab ชื่อ "SoundManager" ในโฟลเดอร์ Resources ก่อน (เอาไว้ตั้งค่าล่วงหน้าได้)
// ถ้าไม่มี Prefab ก็จะสร้างตัวเปล่า แล้วโหลดคลังเสียงชื่อ "SoundLibrary" จาก Resources แทน
//
// ตัวนี้ข้ามฉากไปด้วย (DontDestroyOnLoad) เพลงเลยเล่นต่อเนื่องข้ามด่านได้
public class SoundManager : MonoBehaviour
{
    // ชื่อไฟล์ใน Resources ที่ระบบจะไปหาให้เองตอนไม่มีตัวจัดการอยู่ในฉาก
    private const string ResourcesPrefabName = "SoundManager";
    private const string ResourcesLibraryName = "SoundLibrary";

    // คีย์ที่ใช้จำระดับเสียงลงเครื่องผู้เล่น
    private const string MasterVolumeKey = "Sound.MasterVolume";
    private const string BgmVolumeKey = "Sound.BgmVolume";
    private const string SfxVolumeKey = "Sound.SfxVolume";
    private const string MutedKey = "Sound.Muted";

    private static SoundManager instance;
    private static bool isQuitting = false;

    // เรียกจากที่ไหนก็ได้ ไม่ต้องเช็ค null (ถ้ายังไม่มีจะสร้างให้เอง)
    // ยกเว้นตอนเกมกำลังปิด จะคืน null เพื่อไม่ให้ไปสร้าง GameObject ค้างไว้ตอน OnDestroy
    public static SoundManager Instance
    {
        get
        {
            if (instance != null) return instance;
            if (isQuitting) return null;

            // เผื่อมีวางไว้ในฉากอยู่แล้วแต่ Awake ยังไม่ทันวิ่ง (เช่นถูกเรียกจาก Awake ของตัวอื่น)
            instance = FindObjectOfType<SoundManager>();
            if (instance != null) return instance;

            instance = CreateFromResources();
            return instance;
        }
    }

    // เช็คว่ามีตัวจัดการอยู่จริงไหม โดยไม่ไปสร้างตัวใหม่
    // ใช้ตอนที่ "ไม่มีเสียงก็ไม่เป็นไร" เช่นในโค้ดที่วิ่งตอนปิดเกม
    public static bool Exists => instance != null;

    [Header("คลังเสียง")]
    [Tooltip("ไฟล์คลังเสียงของเกม (คลิกขวาใน Project > Create > MyGame > Sound Library)\nปล่อยว่าง = ระบบจะไปโหลดไฟล์ชื่อ \"SoundLibrary\" จากโฟลเดอร์ Resources ให้เอง")]
    public SoundLibrary library;

    [Header("Audio Mixer (ไม่ใส่ก็ได้)")]
    [Tooltip("ใส่ AudioMixer ถ้าอยากคุมเสียงผ่าน Mixer (ใส่เอฟเฟกต์ ดัค เสียงได้)\nปล่อยว่าง = ระบบคูณระดับเสียงให้เองตรง ๆ ซึ่งพอสำหรับเกมทั่วไปแล้ว")]
    public AudioMixer mixer;
    [Tooltip("กลุ่มใน Mixer ที่จะให้เสียง SFX วิ่งผ่าน")]
    public AudioMixerGroup sfxMixerGroup;
    [Tooltip("กลุ่มใน Mixer ที่จะให้เพลง BGM วิ่งผ่าน")]
    public AudioMixerGroup bgmMixerGroup;
    [Tooltip("ชื่อพารามิเตอร์เสียงรวมที่ Expose ไว้ใน Mixer")]
    public string masterVolumeParameter = "MasterVolume";
    public string bgmVolumeParameter = "BgmVolume";
    public string sfxVolumeParameter = "SfxVolume";

    [Header("ช่องเสียง SFX")]
    [Tooltip("จำนวนเสียงที่เล่นพร้อมกันได้ตอนเริ่มเกม ระบบจะสร้างเพิ่มให้เองถ้าไม่พอ")]
    public int initialSfxVoices = 16;
    [Tooltip("เพดานจำนวนเสียงที่เล่นพร้อมกันได้ เกินกว่านี้จะไปแย่งช่องของเสียงที่เล่นมานานที่สุดแทน")]
    public int maxSfxVoices = 32;

    [Header("เพลงประกอบ (BGM)")]
    [Tooltip("เวลาที่ใช้ไล่เสียงเพลงเข้า/ออกตอนเปลี่ยนเพลง (วินาที) ใส่ 0 = ตัดเปลี่ยนทันที")]
    public float defaultBgmFadeDuration = 1f;

    [Header("ระดับเสียงเริ่มต้น (ใช้เฉพาะครั้งแรกที่เปิดเกม)")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1f;
    [Range(0f, 1f)] public float defaultBgmVolume = 0.7f;
    [Range(0f, 1f)] public float defaultSfxVolume = 1f;

    [Header("Debug")]
    [Tooltip("ขึ้น log ทุกครั้งที่เรียกเสียงที่ไม่มีในคลัง เปิดไว้ตอนทำเกมจะได้รู้ว่าพิมพ์ชื่อผิดตรงไหน")]
    public bool warnOnMissingSound = true;

    [Tooltip("เตือนเมื่อไฟล์เสียงวนตั้งค่านำเข้าไว้แบบที่ทำให้เสียงสะดุดตอนวนกลับ (Load Type = Streaming)")]
    public bool warnOnLoopImportSettings = true;

    private float masterVolume = 1f;
    private float bgmVolume = 0.7f;
    private float sfxVolume = 1f;
    private bool isMuted = false;

    // ช่องเสียง 1 ช่อง = AudioSource 1 ตัวที่หมุนเวียนใช้ซ้ำ
    // ทำแบบนี้แทนการสร้าง AudioSource ใหม่ทุกครั้ง เพราะการสร้าง/ทำลาย GameObject รัว ๆ กินแรงเครื่องมาก
    // internal ไม่ใช่ private เพราะ SoundHandle ต้องถือตัวช่องนี้ไว้อ้างอิงทีหลัง
    internal class Voice
    {
        public AudioSource source;
        public Transform tf;
        public string id;
        public bool isLoop;
        public float baseVolume = 1f;
        // ค่าที่ผู้เรียกปรับเพิ่มทีหลังผ่าน handle.SetVolume (คูณทับ baseVolume อีกที)
        public float volumeScale = 1f;
        public float startTime;
        // นับรอบการใช้ช่อง ใช้เช็คว่า handle ที่ผู้เรียกถืออยู่ยังชี้มาที่เสียงเดิมหรือเปล่า
        // ถ้าช่องถูกเอาไปเล่นเสียงอื่นแล้ว handle เก่าจะสั่งหยุดเสียงใหม่ไม่ได้
        public int generation;
        // ถ้าตั้งไว้ เสียงจะตามวัตถุนี้ไปเรื่อย ๆ (เช่นเสียงย่างที่ติดอยู่กับเตา)
        public Transform follow;
        // เคยผูกกับวัตถุไหม ใช้แยกเคส "ไม่เคยผูก" ออกจาก "ผูกไว้แล้ววัตถุถูกทำลายไป"
        // สองเคสนี้ follow เป็น null เหมือนกัน แต่เคสหลังต้องสั่งหยุดเสียงวนที่ค้างอยู่
        public bool hadFollow;

        // --- ส่วนของเสียงวนแบบต่อรอบเอง ---
        // ช่องเสียงสำรอง มีเฉพาะช่องที่เคยเล่นเสียงวนแบบต่อเอง สร้างตอนใช้ครั้งแรกแล้วใช้ซ้ำตลอด
        // ต้องมีสองตัวสลับกัน เพราะต้องสั่งรอบถัดไปล่วงหน้าตั้งแต่รอบปัจจุบันยังดังอยู่
        public AudioSource sourceB;
        // รอบถัดไปจะไปออกที่ช่องสำรองหรือช่องหลัก
        public bool nextCycleUsesB;
        // กำลังต่อรอบเองอยู่ไหม (true ตั้งแต่สั่งรอบแรกจนกว่าจะโดนสั่งหยุด)
        public bool loopScheduled;
        public AudioClip loopClip;
        // จุดเริ่มและความยาวของช่วงที่เอามาวน (วินาทีในตัวไฟล์ ตัดความเงียบหัวท้ายออกแล้ว)
        public float loopBodyStart;
        public float loopBodyLength;
        public float loopPitch = 1f;
        // เวลาที่รอบถัดไปต้องเริ่ม นับด้วยนาฬิกาของระบบเสียง (dspTime) ไม่ใช่นาฬิกาเกม
        // ต้องใช้นาฬิกาเสียงเท่านั้น เพราะความคลาดเคลื่อนระดับเสี้ยวมิลลิวินาทีก็ได้ยินเป็นรอยต่อแล้ว
        public double nextCycleTime;

        // ความยาวของหนึ่งรอบเมื่อคิดเป็นเวลาจริง (pitch สูง = เล่นจบเร็วกว่าความยาวไฟล์)
        public double CycleDuration => loopPitch > 0.01f ? loopBodyLength / loopPitch : loopBodyLength;
    }

    private readonly List<Voice> voices = new List<Voice>();

    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private bool isUsingSourceA = true;
    private float bgmBaseVolume = 1f;
    private string currentBgmId;
    private Coroutine bgmRoutine;

    private void Awake()
    {
        // ตัวจัดการต้องมีตัวเดียวทั้งเกม ถ้าเผลอวางไว้หลายฉากแล้วเดินข้ามฉากมาเจอกัน ให้ตัวใหม่ตายไป
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private bool isInitialized = false;

    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (library == null)
        {
            library = Resources.Load<SoundLibrary>(ResourcesLibraryName);

            if (library == null)
            {
                Debug.LogWarning($"[SoundManager] ยังไม่ได้ผูกคลังเสียงไว้ และหาไฟล์ \"{ResourcesLibraryName}\" ในโฟลเดอร์ Resources ไม่เจอ\nเรียกเสียงด้วยชื่อจะยังไม่ดัง (เล่นด้วย AudioClip ตรง ๆ ยังใช้ได้อยู่)", this);
            }
        }

        CreateBgmSources();
        EnsureVoiceCount(initialSfxVoices);
        LoadVolumeSettings();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // ---------------------------------------------------------------------
    // การสร้างตัวจัดการอัตโนมัติ
    // ---------------------------------------------------------------------

    private static SoundManager CreateFromResources()
    {
        // ลองหา Prefab ที่ตั้งค่าไว้แล้วก่อน จะได้ใช้คลังเสียง/Mixer ที่ผูกไว้ใน Prefab
        GameObject prefab = Resources.Load<GameObject>(ResourcesPrefabName);

        if (prefab != null)
        {
            GameObject spawned = Instantiate(prefab);
            spawned.name = ResourcesPrefabName;

            SoundManager fromPrefab = spawned.GetComponent<SoundManager>();

            if (fromPrefab != null) return fromPrefab;

            // มี Prefab ชื่อตรงกันแต่ไม่มีสคริปต์ติดอยู่ ถือว่าใช้ไม่ได้ ทิ้งแล้วไปสร้างตัวเปล่าแทน
            Debug.LogWarning($"[SoundManager] เจอ Prefab \"{ResourcesPrefabName}\" ใน Resources แต่ไม่มีสคริปต์ SoundManager ติดอยู่", spawned);
            Destroy(spawned);
        }

        GameObject go = new GameObject(ResourcesPrefabName);
        return go.AddComponent<SoundManager>();
    }

    // ---------------------------------------------------------------------
    // ระดับเสียง (จำลงเครื่องผู้เล่นด้วย PlayerPrefs)
    // ---------------------------------------------------------------------

    public float MasterVolume => masterVolume;
    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;
    public bool IsMuted => isMuted;

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, defaultBgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
        isMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;

        ApplyVolumes();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        ApplyVolumes();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        ApplyVolumes();
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        ApplyVolumes();
    }

    public void ToggleMute()
    {
        SetMuted(!isMuted);
    }

    // เขียนค่าที่ตั้งไว้ลงเครื่องทันที ปกติ Unity เขียนให้ตอนปิดเกมอยู่แล้ว
    // แต่ถ้าเกมถูกปิดแบบไม่สวย (crash / ปิด Editor) ค่าจะหาย เลยเปิดให้หน้าตั้งค่าสั่งเซฟเองได้
    public void SaveVolumeSettings()
    {
        PlayerPrefs.Save();
    }

    private float EffectiveBgmVolume => isMuted ? 0f : masterVolume * bgmVolume;
    private float EffectiveSfxVolume => isMuted ? 0f : masterVolume * sfxVolume;

    private void ApplyVolumes()
    {
        if (mixer != null)
        {
            // ใช้ Mixer: ส่งค่าเป็นเดซิเบล แล้วปล่อยให้ Mixer คูณให้เอง
            SetMixerVolume(masterVolumeParameter, isMuted ? 0f : masterVolume);
            SetMixerVolume(bgmVolumeParameter, bgmVolume);
            SetMixerVolume(sfxVolumeParameter, sfxVolume);
        }

        // อัปเดตเสียงที่กำลังดังอยู่ด้วย ไม่งั้นลากสไลเดอร์แล้วเสียงที่เล่นค้างอยู่จะไม่เปลี่ยนตาม
        for (int i = 0; i < voices.Count; i++)
        {
            Voice voice = voices[i];

            if (IsVoiceActive(voice))
            {
                SetVoiceOutputVolume(voice, ResolveSfxVoiceVolume(voice.baseVolume * voice.volumeScale));
            }
        }

        ApplyBgmVolume();
    }

    private void SetMixerVolume(string parameterName, float linearValue)
    {
        if (mixer == null || string.IsNullOrEmpty(parameterName)) return;

        // Mixer คิดเป็นเดซิเบล ต้องแปลงจากค่าเชิงเส้น 0-1 ก่อน
        // กันค่า 0 ด้วยค่าน้อย ๆ เพราะ log10(0) เป็นอนันต์ลบ
        float db = Mathf.Log10(Mathf.Max(linearValue, 0.0001f)) * 20f;
        mixer.SetFloat(parameterName, db);
    }

    // ตอนใช้ Mixer ปล่อยให้ Mixer เป็นคนคูณระดับเสียงรวม ตัว AudioSource เลยใช้แค่ความดังของเสียงนั้น ๆ
    private float ResolveSfxVoiceVolume(float baseVolume)
    {
        return mixer != null ? baseVolume : baseVolume * EffectiveSfxVolume;
    }

    private void ApplyBgmVolume()
    {
        // ระหว่างไล่เสียงเข้า/ออก ปล่อยให้คอรูทีนเป็นคนคุมความดัง ไม่งั้นค่าจะกระโดด
        if (bgmRoutine != null) return;

        AudioSource active = ActiveBgmSource;
        if (active == null) return;

        active.volume = mixer != null ? bgmBaseVolume : bgmBaseVolume * EffectiveBgmVolume;
    }

    // ---------------------------------------------------------------------
    // SFX
    // ---------------------------------------------------------------------

    // เล่นเสียงจากคลัง (เสียงดังเท่ากันทั้งจอ เหมาะกับเสียง UI และเสียงทั่วไป)
    public SoundHandle PlaySFX(string id)
    {
        return PlayEntry(FindSfx(id), null, Vector3.zero, false, null);
    }

    // เล่นเสียงจากคลังที่ตำแหน่งในฉาก (ยิ่งไกลกล้องยิ่งเบา) ต้องติ๊ก Is 3D ในคลังด้วย
    public SoundHandle PlaySFX(string id, Vector3 position)
    {
        return PlayEntry(FindSfx(id), null, position, true, null);
    }

    // เล่นเสียงที่ตำแหน่งของวัตถุ เขียนสั้นกว่าแบบส่ง Vector3
    public SoundHandle PlaySFX(string id, Transform at)
    {
        if (at == null) return PlaySFX(id);
        return PlayEntry(FindSfx(id), null, at.position, true, null);
    }

    // เล่นไฟล์เสียงตรง ๆ โดยไม่ต้องมีในคลัง
    // เหมาะกับกรณีที่อยากลากไฟล์เสียงใส่ช่องใน Inspector ของสคริปต์ตัวนั้นเลย
    public SoundHandle PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return SoundHandle.None;

        SoundEntry temp = new SoundEntry
        {
            id = clip.name,
            volume = Mathf.Clamp01(volume),
            minPitch = pitch,
            maxPitch = pitch,
            minInterval = 0f,
            maxSimultaneous = 0
        };

        return PlayEntry(temp, clip, Vector3.zero, false, null);
    }

    // เล่นเสียงวนไม่รู้จบ เก็บค่าที่คืนมาไว้สั่งหยุดเอง เช่น
    //   sizzleHandle = SoundManager.Instance.PlayLoop("grill_sizzle", transform);
    //   sizzleHandle.Stop(0.3f);
    public SoundHandle PlayLoop(string id, Transform follow = null)
    {
        SoundEntry entry = FindSfx(id);
        if (entry == null) return SoundHandle.None;

        bool positional = follow != null && entry.is3D;
        Vector3 position = follow != null ? follow.position : Vector3.zero;

        return PlayEntry(entry, null, position, positional, positional ? follow : null, true);
    }

    private SoundEntry FindSfx(string id)
    {
        if (library == null)
        {
            if (warnOnMissingSound && !string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[SoundManager] เรียกเสียง \"{id}\" แต่ยังไม่มีคลังเสียงผูกไว้", this);
            }
            return null;
        }

        SoundEntry entry = library.GetSfx(id);

        if (entry == null && warnOnMissingSound && !string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"[SoundManager] ไม่มีเสียง SFX ชื่อ \"{id}\" ในคลัง (เช็คตัวสะกดในคลังเสียงอีกที)", this);
        }

        return entry;
    }

    private SoundHandle PlayEntry(SoundEntry entry, AudioClip overrideClip, Vector3 position, bool positional, Transform follow, bool forceLoop = false)
    {
        Initialize();

        if (entry == null) return SoundHandle.None;

        AudioClip clip = overrideClip != null ? overrideClip : entry.PickClip();

        if (clip == null)
        {
            if (warnOnMissingSound)
            {
                Debug.LogWarning($"[SoundManager] เสียง \"{entry.id}\" มีในคลังแต่ยังไม่ได้ใส่ไฟล์เสียงไว้", this);
            }
            return SoundHandle.None;
        }

        bool loop = forceLoop || entry.loop;

        // เสียงวนไม่ต้องกันซ้อน เพราะผู้เรียกคุมเองว่าจะเปิด/ปิดตอนไหน
        if (!loop && !CanPlayNow(entry)) return SoundHandle.None;

        Voice voice = AcquireVoice();
        if (voice == null) return SoundHandle.None;

        // ช่องนี้อาจเพิ่งเล่นเสียงวนแบบต่อรอบเองมาก่อน ต้องล้างคิวรอบที่ตั้งค้างไว้ทิ้งให้หมด
        // ไม่งั้นรอบเก่าที่ตั้งเวลาไว้จะโผล่มาดังทับเสียงใหม่ที่กำลังจะเล่น
        ClearLoopSchedule(voice);

        voice.generation++;
        voice.id = entry.id;
        voice.isLoop = loop;
        voice.baseVolume = entry.volume;
        voice.volumeScale = 1f;
        voice.startTime = Time.unscaledTime;
        voice.follow = follow;
        voice.hadFollow = follow != null;

        float pitch = entry.PickPitch();

        AudioSource source = voice.source;
        source.clip = clip;
        source.loop = loop;
        source.pitch = pitch;
        source.volume = ResolveSfxVoiceVolume(entry.volume);
        source.outputAudioMixerGroup = sfxMixerGroup;

        bool use3D = positional && entry.is3D;
        source.spatialBlend = use3D ? 1f : 0f;

        if (use3D)
        {
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = Mathf.Max(0.01f, entry.minDistance);
            source.maxDistance = Mathf.Max(source.minDistance + 0.01f, entry.maxDistance);
            voice.tf.position = position;
        }
        else
        {
            // เสียงไม่มีตำแหน่ง ดึงกลับมาไว้ที่ตัวจัดการ ไม่งั้นตำแหน่งเก่าจะค้างอยู่
            voice.tf.localPosition = Vector3.zero;
            voice.follow = null;
            voice.hadFollow = false;
        }

        entry.lastPlayTime = Time.unscaledTime;

        // เสียงวน: ต่อรอบเองด้วยนาฬิกาเสียง แทนการติ๊ก loop ให้ AudioSource
        // การวนของ AudioSource จะได้ยินเป็นช่องว่างสะดุดทุกรอบเมื่อไฟล์เป็นแบบบีบอัด
        // เพราะตัวถอดรหัสต้องวิ่งกลับไปต้นไฟล์ก่อนถึงจะเล่นต่อได้ และไฟล์บีบอัดมักมีความเงียบติดหัวท้ายมาด้วย
        if (loop && StartSeamlessLoop(voice, entry, clip, pitch))
        {
            return new SoundHandle(this, voice, voice.generation);
        }

        source.Play();

        return new SoundHandle(this, voice, voice.generation);
    }

    private bool CanPlayNow(SoundEntry entry)
    {
        // กันเสียงเดิมยิงถี่เกินไปจนกลายเป็นเสียงแตก
        if (entry.minInterval > 0f && Time.unscaledTime - entry.lastPlayTime < entry.minInterval)
        {
            return false;
        }

        if (entry.maxSimultaneous > 0)
        {
            int playing = 0;

            for (int i = 0; i < voices.Count; i++)
            {
                Voice voice = voices[i];

                if (IsVoiceActive(voice) && voice.id == entry.id)
                {
                    playing++;
                    if (playing >= entry.maxSimultaneous) return false;
                }
            }
        }

        return true;
    }

    private Voice AcquireVoice()
    {
        // หาช่องว่างก่อน (เสียงวนไม่นับว่าว่าง ถึงจะยังเล่นอยู่ก็ห้ามไปแย่ง)
        for (int i = 0; i < voices.Count; i++)
        {
            Voice voice = voices[i];

            if (voice.source != null && !voice.isLoop && !IsVoiceActive(voice))
            {
                return voice;
            }
        }

        if (voices.Count < maxSfxVoices)
        {
            return CreateVoice(voices.Count);
        }

        // เต็มเพดานแล้ว ไปแย่งช่องของเสียงที่เล่นมานานที่สุด (ยกเว้นเสียงวน)
        // เสียงที่เล่นมานานสุดคือเสียงที่ใกล้จบที่สุด ตัดทิ้งแล้วผู้เล่นสังเกตเห็นน้อยที่สุด
        Voice oldest = null;

        for (int i = 0; i < voices.Count; i++)
        {
            Voice voice = voices[i];

            if (voice.isLoop) continue;
            if (oldest == null || voice.startTime < oldest.startTime) oldest = voice;
        }

        if (oldest != null)
        {
            StopVoiceSources(oldest);
            return oldest;
        }

        // ทุกช่องเป็นเสียงวนหมด แปลว่ามีที่ไหนสักแห่งเปิดเสียงวนทิ้งไว้แล้วลืมปิด
        Debug.LogWarning("[SoundManager] ช่องเสียงเต็มไปด้วยเสียงวนทั้งหมด เช็คว่ามีตรงไหนเรียก PlayLoop แล้วลืมสั่ง Stop หรือเปล่า", this);
        return null;
    }

    private void EnsureVoiceCount(int count)
    {
        while (voices.Count < count)
        {
            CreateVoice(voices.Count);
        }
    }

    private Voice CreateVoice(int index)
    {
        GameObject go = new GameObject($"SfxVoice_{index:00}");
        go.transform.SetParent(transform, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = sfxMixerGroup;
        // ปิดดอปเปลอร์ ไม่งั้นเสียงที่ผูกกับของที่ขยับ (เช่นเสียงย่างตอนหมูเด้งขึ้นตอนพลิก หรือตอนผู้เล่นลาก)
        // จะโดนดัน pitch ขึ้นลงตามความเร็วจนฟังเหมือนเสียงเพี้ยนไม่นิ่ง
        source.dopplerLevel = 0f;

        Voice voice = new Voice
        {
            source = source,
            tf = go.transform
        };

        voices.Add(voice);
        return voice;
    }

    // ---------------------------------------------------------------------
    // เสียงวนแบบต่อรอบเอง
    //
    // ปล่อยให้ AudioSource วนเองแล้วจะได้ยินเป็นช่องว่างสะดุดทุกรอบ เพราะ
    //   1. ไฟล์บีบอัด (mp3/ogg) ต้องให้ตัวถอดรหัสวิ่งกลับไปต้นไฟล์ก่อนถึงจะเล่นต่อได้
    //   2. ตัวเข้ารหัส mp3 เติมความเงียบไว้หัวและท้ายไฟล์เสมอ ความเงียบนั้นก็ถูกวนไปด้วย
    // ตรงนี้เลยสั่งรอบถัดไปไว้ล่วงหน้าด้วยนาฬิกาของระบบเสียง (dspTime) โดยสลับใช้ AudioSource สองตัว
    // รอบใหม่จะเริ่มตรงกับที่รอบเก่าจบพอดีในระดับตัวอย่างเสียง เลยฟังต่อเนื่องเป็นเนื้อเดียว
    // ---------------------------------------------------------------------

    // สั่งรอบถัดไปล่วงหน้าเท่าไร (วินาที) ต้องมากพอให้ระบบเสียงเตรียมข้อมูลทัน แม้เฟรมจะกระตุกไปบ้าง
    private const double LoopScheduleAhead = 0.25;
    // เว้นเวลาให้รอบแรกนิดหน่อย เผื่อเฟรมนี้ยาวผิดปกติ จะได้ไม่เริ่มช้ากว่าเวลาที่สั่งไว้
    private const double LoopStartLead = 0.05;

    // คืน true เมื่อรับหน้าที่ต่อรอบเองแล้ว (ผู้เรียกไม่ต้องสั่ง Play เอง)
    // คืน false เมื่อไม่เหมาะจะต่อรอบเอง ให้กลับไปใช้การวนของ AudioSource ตามเดิม
    private bool StartSeamlessLoop(Voice voice, SoundEntry entry, AudioClip clip, float pitch)
    {
        if (!entry.seamlessLoop) return false;

        float bodyStart, bodyLength;
        entry.GetLoopBody(clip, out bodyStart, out bodyLength);

        // ไฟล์สั้นจนเกือบเป็นเสียงคลิก ต่อรอบเองไม่คุ้มกับความคลาดเคลื่อนของการตั้งเวลา
        if (bodyLength < SoundEntry.MinLoopBodyLength) return false;

        EnsureSecondSource(voice);
        if (voice.sourceB == null) return false;

        WarnIfLoopClipImportIsRisky(clip);

        // ยังไม่ได้โหลดข้อมูลเสียงไว้ (ไฟล์ที่ติ๊ก Preload Audio Data ออก) โหลดตั้งแต่ตอนนี้เลย
        // ไม่งั้นรอบแรกจะเริ่มช้ากว่าเวลาที่สั่งไว้ เพราะมัวรอโหลดอยู่
        if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();

        voice.loopScheduled = true;
        voice.loopClip = clip;
        voice.loopBodyStart = bodyStart;
        voice.loopBodyLength = bodyLength;
        voice.loopPitch = Mathf.Max(0.01f, pitch);

        // ห้ามติ๊ก loop ไว้ ไม่งั้น AudioSource จะวนของมันเองแล้วไปทับรอบที่เราสั่งไว้
        voice.source.loop = false;
        voice.sourceB.loop = false;

        CopySourceSettings(voice.source, voice.sourceB);

        double startTime = AudioSettings.dspTime + LoopStartLead;
        ScheduleLoopCycle(voice, voice.source, startTime);

        voice.nextCycleTime = startTime + voice.CycleDuration;
        voice.nextCycleUsesB = true;
        return true;
    }

    private void ScheduleLoopCycle(Voice voice, AudioSource source, double startTime)
    {
        source.clip = voice.loopClip;
        source.loop = false;
        source.pitch = voice.loopPitch;

        // ข้ามความเงียบหัวไฟล์ ต้องตั้งตอนที่ยังไม่เล่น ไม่งั้นจะกลายเป็นการกระโดดตำแหน่งของเสียงที่ดังอยู่
        source.time = voice.loopBodyStart;
        source.PlayScheduled(startTime);

        // ตัดท้ายให้จบตรงเวลาพอดี ไม่งั้นความเงียบท้ายไฟล์จะลากต่อไปทับรอบถัดไปที่เริ่มไปแล้ว
        source.SetScheduledEndTime(startTime + voice.CycleDuration);
    }

    // เรียกทุกเฟรมเพื่อสั่งรอบถัดไปล่วงหน้า ก่อนที่รอบปัจจุบันจะจบ
    private void UpdateSeamlessLoops()
    {
        double now = AudioSettings.dspTime;

        for (int i = 0; i < voices.Count; i++)
        {
            Voice voice = voices[i];

            if (!voice.loopScheduled) continue;
            if (voice.source == null || voice.sourceB == null) continue;

            double duration = voice.CycleDuration;
            if (duration <= 0d) continue;

            // ตกรอบไปไกลเกินกว่าจะไล่ตามทัน (เช่นเกมค้างยาว หรือเพิ่งเลิกหยุดเสียงทั้งเกม)
            // ไล่ตามรอบเก่าไม่มีประโยชน์ เริ่มนับรอบใหม่จากตอนนี้เลย
            if (now > voice.nextCycleTime + duration)
            {
                voice.nextCycleTime = now + LoopStartLead;
            }

            // ห้ามสั่งล่วงหน้าเกินครึ่งรอบ ไม่งั้นจะไปสั่งช่องที่ยังเล่นรอบก่อนหน้าค้างอยู่ แล้วเสียงรอบนั้นจะโดนตัดกลางคัน
            double half = duration * 0.5;
            double lead = half < LoopScheduleAhead ? half : LoopScheduleAhead;

            if (now + lead < voice.nextCycleTime) continue;

            AudioSource current = voice.nextCycleUsesB ? voice.source : voice.sourceB;
            AudioSource next = voice.nextCycleUsesB ? voice.sourceB : voice.source;

            // ลอกค่าจากตัวที่กำลังดังอยู่ เผื่อระดับเสียงถูกปรับระหว่างทาง (สไลเดอร์ตั้งค่า / handle.SetVolume)
            CopySourceSettings(current, next);
            ScheduleLoopCycle(voice, next, voice.nextCycleTime);

            voice.nextCycleUsesB = !voice.nextCycleUsesB;
            voice.nextCycleTime += duration;
        }
    }

    // เตือนครั้งเดียวต่อไฟล์ ไม่งั้นจะขึ้นรัวทุกครั้งที่วางของลงเตา
    private readonly HashSet<AudioClip> warnedLoopClips = new HashSet<AudioClip>();

    // การตั้งค่านำเข้าไฟล์มีผลกับเสียงวนมาก ต่อให้โค้ดต่อรอบให้เนียนแล้วก็ยังสะดุดได้ถ้าตั้งค่าผิด
    private void WarnIfLoopClipImportIsRisky(AudioClip clip)
    {
        if (!warnOnLoopImportSettings || clip == null) return;
        if (clip.loadType != AudioClipLoadType.Streaming) return;
        if (!warnedLoopClips.Add(clip)) return;

        Debug.LogWarning($"[SoundManager] ไฟล์เสียงวน \"{clip.name}\" ตั้ง Load Type เป็น Streaming ซึ่งจะสะดุดตอนวนกลับ\nเลือกไฟล์ใน Project แล้วตั้ง Load Type = Decompress On Load และ Compression Format = PCM (หรือ ADPCM) จะได้เสียงวนที่ต่อเนื่องที่สุด", clip);
    }

    private void EnsureSecondSource(Voice voice)
    {
        if (voice.sourceB != null || voice.tf == null) return;

        // วางไว้บน GameObject เดียวกับช่องหลัก จะได้เลื่อนตามวัตถุไปพร้อมกันโดยไม่ต้องตามตำแหน่งเพิ่ม
        AudioSource extra = voice.tf.gameObject.AddComponent<AudioSource>();
        extra.playOnAwake = false;
        extra.loop = false;
        extra.spatialBlend = 0f;
        extra.dopplerLevel = 0f;
        extra.outputAudioMixerGroup = sfxMixerGroup;

        voice.sourceB = extra;
    }

    // ล้างคิวรอบที่ตั้งไว้ล่วงหน้าทิ้ง ต้องเรียกทุกครั้งก่อนเอาช่องนี้ไปทำอย่างอื่น
    // ไม่งั้นรอบที่ตั้งเวลาไว้จะโผล่มาดังเองทีหลัง ทั้งที่เสียงวนถูกสั่งหยุดไปแล้ว
    private void ClearLoopSchedule(Voice voice)
    {
        if (voice == null) return;

        voice.loopScheduled = false;
        voice.loopClip = null;
        voice.nextCycleTime = 0d;
        voice.nextCycleUsesB = false;

        if (voice.sourceB != null) voice.sourceB.Stop();
    }

    private static void CopySourceSettings(AudioSource from, AudioSource to)
    {
        if (from == null || to == null) return;

        to.outputAudioMixerGroup = from.outputAudioMixerGroup;
        to.volume = from.volume;
        to.pitch = from.pitch;
        to.spatialBlend = from.spatialBlend;
        to.rolloffMode = from.rolloffMode;
        to.minDistance = from.minDistance;
        to.maxDistance = from.maxDistance;
        to.dopplerLevel = from.dopplerLevel;
        to.panStereo = from.panStereo;
        to.bypassEffects = from.bypassEffects;
        to.bypassListenerEffects = from.bypassListenerEffects;
        to.bypassReverbZones = from.bypassReverbZones;
    }

    // ช่องนี้ยังถือเสียงอยู่ไหม (นับรอบที่ตั้งเวลาไว้แต่ยังไม่ถึงคิวด้วย)
    private static bool IsVoiceActive(Voice voice)
    {
        if (voice == null) return false;
        if (voice.loopScheduled) return true;
        if (voice.source != null && voice.source.isPlaying) return true;

        return voice.sourceB != null && voice.sourceB.isPlaying;
    }

    // ระดับเสียงต้องเขียนลงทั้งสองช่องเสมอ เพราะรอบถัดไปอาจถูกตั้งคิวไว้ที่อีกช่องแล้ว
    private static void SetVoiceOutputVolume(Voice voice, float volume)
    {
        if (voice.source != null) voice.source.volume = volume;
        if (voice.sourceB != null) voice.sourceB.volume = volume;
    }

    private static void StopVoiceSources(Voice voice)
    {
        if (voice.source != null) voice.source.Stop();
        if (voice.sourceB != null) voice.sourceB.Stop();
    }

    private void LateUpdate()
    {
        // ต้องมาก่อนการลากตำแหน่ง เพราะรอบถัดไปต้องถูกสั่งไว้ล่วงหน้าให้ทันเสมอ
        UpdateSeamlessLoops();

        // ลากเสียงที่ผูกกับวัตถุให้ตามวัตถุไป และปล่อยช่องคืนเมื่อวัตถุถูกทำลายไปแล้ว
        for (int i = 0; i < voices.Count; i++)
        {
            Voice voice = voices[i];

            if (!voice.hadFollow) continue;

            if (voice.follow == null)
            {
                // ของที่เสียงผูกอยู่ถูกทำลายไปแล้ว (เช่นหมูโดนทิ้งลงถังขยะทั้งที่ยังย่างอยู่)
                // ต้องหยุดเสียงวนเอง ไม่งั้นมันจะดังค้างไปทั้งเกมโดยไม่มีใครสั่งหยุดได้อีก
                voice.hadFollow = false;

                if (voice.isLoop && IsVoiceActive(voice))
                {
                    StopVoice(voice, 0.15f);
                }

                continue;
            }

            voice.tf.position = voice.follow.position;
        }
    }

    // หยุดเสียงทุกช่องที่กำลังเล่นชื่อนี้อยู่ (รวมเสียงวนด้วย)
    public void StopSFX(string id, float fadeDuration = 0f)
    {
        if (string.IsNullOrEmpty(id)) return;

        for (int i = 0; i < voices.Count; i++)
        {
            Voice voice = voices[i];

            if (IsVoiceActive(voice) && voice.id == id)
            {
                StopVoice(voice, fadeDuration);
            }
        }
    }

    // หยุดเสียงประกอบทั้งหมด (ไม่ยุ่งกับเพลง) เช่นตอนกดออกจากด่านกลางคัน
    public void StopAllSFX()
    {
        for (int i = 0; i < voices.Count; i++)
        {
            Voice voice = voices[i];

            if (IsVoiceActive(voice))
            {
                StopVoice(voice, 0f);
            }
        }
    }

    private void StopVoice(Voice voice, float fadeDuration)
    {
        if (voice == null || voice.source == null) return;

        // ต้องล้าง hadFollow ด้วย ไม่ใช่แค่ follow
        // ไม่งั้น LateUpdate จะเห็นว่า "เคยผูกไว้แต่ตอนนี้ไม่มีแล้ว" แล้วสั่งหยุดซ้ำอีกรอบ
        voice.follow = null;
        voice.hadFollow = false;

        if (fadeDuration <= 0f)
        {
            // ล้างคิวรอบล่วงหน้าก่อนหยุด ไม่งั้นรอบที่ตั้งเวลาไว้จะโผล่มาดังทีหลังทั้งที่สั่งหยุดไปแล้ว
            ClearLoopSchedule(voice);
            StopVoiceSources(voice);
            voice.isLoop = false;
            // เปลี่ยนรุ่นของช่อง เพื่อให้ handle ที่ผู้เรียกถืออยู่รู้ว่าเสียงนี้จบไปแล้ว
            voice.generation++;
            return;
        }

        StartCoroutine(FadeOutVoiceRoutine(voice, voice.generation, fadeDuration));
    }

    private IEnumerator FadeOutVoiceRoutine(Voice voice, int generation, float duration)
    {
        // ระหว่างไล่เสียงลง ยังต้องต่อรอบเสียงวนไปตามปกติ ไม่งั้นจะได้ยินเป็นเสียงขาดตอนก่อนจะเงียบ
        // (UpdateSeamlessLoops ยังทำงานอยู่ เพราะ loopScheduled ยังไม่ถูกล้าง)
        float startVolume = voice.source != null ? voice.source.volume : 0f;
        float time = 0f;

        while (time < duration)
        {
            // ระหว่างไล่เสียงลง อาจมีคนไปสั่งเล่นเสียงใหม่ทับช่องนี้ ต้องเลิกทำทันที
            if (voice.generation != generation) yield break;

            // unscaledDeltaTime เพราะเกมหยุดเวลาได้ (เปิดสมุด/หน้าจอแพ้) แต่เสียงต้องไล่ลงต่อ
            time += Time.unscaledDeltaTime;
            SetVoiceOutputVolume(voice, Mathf.Lerp(startVolume, 0f, time / duration));
            yield return null;
        }

        if (voice.generation != generation) yield break;

        ClearLoopSchedule(voice);
        StopVoiceSources(voice);
        voice.isLoop = false;
        voice.generation++;
    }

    // ---------------------------------------------------------------------
    // BGM
    // ---------------------------------------------------------------------

    // ชื่อเพลงที่กำลังเล่นอยู่ ("" ถ้าไม่มีเพลงเล่นอยู่)
    public string CurrentBgmId => currentBgmId;

    private AudioSource ActiveBgmSource => isUsingSourceA ? bgmSourceA : bgmSourceB;
    private AudioSource IdleBgmSource => isUsingSourceA ? bgmSourceB : bgmSourceA;

    private void CreateBgmSources()
    {
        if (bgmSourceA == null) bgmSourceA = CreateBgmSource("BgmSourceA");
        if (bgmSourceB == null) bgmSourceB = CreateBgmSource("BgmSourceB");
    }

    private AudioSource CreateBgmSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.outputAudioMixerGroup = bgmMixerGroup;
        return source;
    }

    // เปลี่ยนเพลงพื้นหลัง (ไล่เสียงเพลงเก่าลงพร้อมไล่เพลงใหม่ขึ้น)
    // เรียกเพลงเดิมซ้ำจะไม่ทำอะไร เพลงเลยเล่นต่อเนื่องข้ามฉากได้ ไม่กระตุกกลับไปเริ่มใหม่
    public void PlayBGM(string id, float fadeDuration = -1f, bool restartIfSame = false)
    {
        Initialize();

        if (string.IsNullOrEmpty(id))
        {
            StopBGM(fadeDuration);
            return;
        }

        if (!restartIfSame && currentBgmId == id && ActiveBgmSource != null && ActiveBgmSource.isPlaying)
        {
            return;
        }

        SoundEntry entry = library != null ? library.GetBgm(id) : null;

        if (entry == null)
        {
            if (warnOnMissingSound)
            {
                Debug.LogWarning($"[SoundManager] ไม่มีเพลง BGM ชื่อ \"{id}\" ในคลัง (เช็คตัวสะกดในคลังเสียงอีกที)", this);
            }
            return;
        }

        AudioClip clip = entry.PickClip();

        if (clip == null)
        {
            if (warnOnMissingSound)
            {
                Debug.LogWarning($"[SoundManager] เพลง \"{id}\" มีในคลังแต่ยังไม่ได้ใส่ไฟล์เสียงไว้", this);
            }
            return;
        }

        currentBgmId = id;
        StartBgmCrossfade(clip, entry.volume, ResolveFade(fadeDuration));
    }

    // เล่นเพลงจากไฟล์ตรง ๆ โดยไม่ต้องมีในคลัง
    public void PlayBGM(AudioClip clip, float volume = 1f, float fadeDuration = -1f, bool restartIfSame = false)
    {
        Initialize();

        if (clip == null)
        {
            StopBGM(fadeDuration);
            return;
        }

        if (!restartIfSame && ActiveBgmSource != null && ActiveBgmSource.clip == clip && ActiveBgmSource.isPlaying)
        {
            return;
        }

        currentBgmId = clip.name;
        StartBgmCrossfade(clip, Mathf.Clamp01(volume), ResolveFade(fadeDuration));
    }

    public void StopBGM(float fadeDuration = -1f)
    {
        currentBgmId = null;
        StartBgmCrossfade(null, 1f, ResolveFade(fadeDuration));
    }

    // หยุดเพลงค้างไว้ตรงจุดเดิม (เช่นตอนเปิดหน้าตั้งค่า) แล้วค่อยเล่นต่อด้วย ResumeBGM
    public void PauseBGM()
    {
        if (ActiveBgmSource != null) ActiveBgmSource.Pause();
    }

    public void ResumeBGM()
    {
        if (ActiveBgmSource != null) ActiveBgmSource.UnPause();
    }

    private float ResolveFade(float fadeDuration)
    {
        return fadeDuration < 0f ? defaultBgmFadeDuration : fadeDuration;
    }

    private void StartBgmCrossfade(AudioClip clip, float clipVolume, float duration)
    {
        if (bgmRoutine != null)
        {
            // ตัดการไล่เสียงรอบก่อนทิ้ง ไม่งั้นสองคอรูทีนจะแย่งกันเขียนความดังของ AudioSource ตัวเดียวกัน
            StopCoroutine(bgmRoutine);
            bgmRoutine = null;
        }

        AudioSource fadeOut = ActiveBgmSource;
        AudioSource fadeIn = IdleBgmSource;

        bgmBaseVolume = clipVolume;

        if (clip != null)
        {
            fadeIn.clip = clip;
            fadeIn.volume = 0f;
            fadeIn.outputAudioMixerGroup = bgmMixerGroup;
            fadeIn.Play();

            // สลับฝั่ง: ตัวที่เพิ่งเริ่มเล่นกลายเป็นตัวหลัก
            isUsingSourceA = !isUsingSourceA;
        }

        float targetVolume = mixer != null ? clipVolume : clipVolume * EffectiveBgmVolume;

        if (duration <= 0f)
        {
            if (fadeOut != null)
            {
                fadeOut.Stop();
                fadeOut.volume = 0f;
            }

            if (clip != null) fadeIn.volume = targetVolume;
            return;
        }

        bgmRoutine = StartCoroutine(BgmCrossfadeRoutine(fadeOut, clip != null ? fadeIn : null, targetVolume, duration));
    }

    private IEnumerator BgmCrossfadeRoutine(AudioSource fadeOut, AudioSource fadeIn, float targetVolume, float duration)
    {
        float fadeOutStart = fadeOut != null ? fadeOut.volume : 0f;
        float time = 0f;

        while (time < duration)
        {
            // unscaledDeltaTime เพราะเพลงต้องเปลี่ยนได้ตอนเกมหยุดเวลา (จบวัน/เปิดสมุด/หน้าจอแพ้)
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            if (fadeOut != null) fadeOut.volume = Mathf.Lerp(fadeOutStart, 0f, t);
            if (fadeIn != null) fadeIn.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        if (fadeOut != null)
        {
            fadeOut.Stop();
            fadeOut.volume = 0f;
            // ปล่อยไฟล์เพลงเก่าทิ้ง ไม่ต้องกอดหน่วยความจำไว้ทั้งเกม
            fadeOut.clip = null;
        }

        if (fadeIn != null) fadeIn.volume = targetVolume;

        bgmRoutine = null;
    }

    // ---------------------------------------------------------------------
    // อื่น ๆ
    // ---------------------------------------------------------------------

    // หยุดเสียงทั้งเกมชั่วคราว (เสียงจะค้างอยู่ตรงจุดเดิม) ใช้ตอนกดหยุดเกมหรือสลับไปหน้าต่างอื่น
    public void SetPaused(bool paused)
    {
        AudioListener.pause = paused;
    }

    // ---------------------------------------------------------------------
    // ทางที่ SoundHandle ใช้คุยกลับมาที่ช่องเสียงของมัน
    // เช็ค generation ทุกครั้ง เพราะช่องอาจถูกเอาไปเล่นเสียงอื่นไปแล้ว
    // ถ้าไม่เช็ค handle เก่าจะไปสั่งหยุดเสียงของคนอื่นที่บังเอิญมาใช้ช่องเดียวกัน
    // ---------------------------------------------------------------------

    internal bool IsVoicePlaying(Voice voice, int generation)
    {
        if (voice == null || voice.generation != generation) return false;
        return IsVoiceActive(voice);
    }

    internal void StopVoiceFromHandle(Voice voice, int generation, float fadeDuration)
    {
        if (voice == null || voice.generation != generation) return;
        if (!IsVoiceActive(voice)) return;

        StopVoice(voice, fadeDuration);
    }

    internal void SetVoiceVolumeFromHandle(Voice voice, int generation, float volumeScale)
    {
        if (voice == null || voice.generation != generation) return;
        if (voice.source == null) return;

        voice.volumeScale = Mathf.Clamp01(volumeScale);
        SetVoiceOutputVolume(voice, ResolveSfxVoiceVolume(voice.baseVolume * voice.volumeScale));
    }
}

// ตัวอ้างอิงถึงเสียงที่กำลังเล่นอยู่ ใช้สั่งหยุด/ปรับเสียงตัวนั้นทีหลัง
// สำคัญกับเสียงวน (PlayLoop) เพราะต้องมีคนสั่งหยุดมันเสมอ
//
// เก็บ handle ไว้ในตัวแปรได้เลย ไม่ต้องเช็ค null:
//   handle.Stop() ตอน handle เป็นเสียงที่จบไปแล้วจะไม่ทำอะไร (และไม่พัง)
public class SoundHandle
{
    // handle เปล่า สำหรับตอนที่เล่นเสียงไม่สำเร็จ เรียกอะไรก็ไม่พัง
    public static readonly SoundHandle None = new SoundHandle();

    private readonly SoundManager owner;
    private readonly SoundManager.Voice voice;
    private readonly int generation;

    private SoundHandle()
    {
        owner = null;
        voice = null;
        generation = -1;
    }

    internal SoundHandle(SoundManager owner, SoundManager.Voice voice, int generation)
    {
        this.owner = owner;
        this.voice = voice;
        this.generation = generation;
    }

    // เสียงตัวนี้ยังดังอยู่ไหม (คืน false ถ้าจบไปแล้ว หรือช่องถูกเอาไปเล่นเสียงอื่นแล้ว)
    public bool IsPlaying => owner != null && owner.IsVoicePlaying(voice, generation);

    public void Stop(float fadeDuration = 0f)
    {
        if (owner == null) return;
        owner.StopVoiceFromHandle(voice, generation, fadeDuration);
    }

    // ปรับความดังของเสียงตัวนี้ระหว่างที่ยังเล่นอยู่ (0-1 เทียบกับความดังที่ตั้งไว้ในคลัง)
    public void SetVolume(float volumeScale)
    {
        if (owner == null) return;
        owner.SetVoiceVolumeFromHandle(voice, generation, volumeScale);
    }
}
