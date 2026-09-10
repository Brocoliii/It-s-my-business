using UnityEngine;
using System.Collections.Generic;

// เสียง 1 รายการในคลัง (ใช้ได้ทั้ง SFX และ BGM)
// ผูกไฟล์เสียงหลายอันไว้ในรายการเดียวได้ เวลาเล่นจะสุ่มมา 1 อัน จะได้ไม่ซ้ำจนน่ารำคาญ
[System.Serializable]
public class SoundEntry
{
    [Tooltip("ชื่อที่ใช้เรียกเสียงนี้จากโค้ด เช่น \"ui_click\", \"grill_sizzle\"\nห้ามซ้ำกับรายการอื่นในลิสต์เดียวกัน")]
    public string id;

    [Tooltip("ไฟล์เสียง ใส่ได้หลายอัน เวลาเล่นจะสุ่มมา 1 อัน\nใส่อันเดียวก็ได้ถ้าไม่ต้องการสุ่ม")]
    public AudioClip[] clips;

    [Header("ระดับเสียง / ระดับเสียงสูงต่ำ")]
    [Tooltip("ความดังของเสียงนี้โดยเฉพาะ (จะถูกคูณกับ SFX Volume และ Master Volume อีกที)")]
    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("ช่วงสุ่ม pitch ตั้งให้ต่างกันเล็กน้อย (เช่น 0.95 - 1.05) เสียงที่เล่นรัว ๆ จะฟังไม่ซ้ำกันเป๊ะ\nตั้งเท่ากันทั้งคู่ = ไม่สุ่ม")]
    [Range(0.1f, 3f)] public float minPitch = 1f;
    [Range(0.1f, 3f)] public float maxPitch = 1f;

    [Header("พฤติกรรม")]
    [Tooltip("เล่นวนไม่รู้จบ (เช่นเสียงเตาย่าง) ต้องเล่นด้วย SoundManager.PlayLoop และเก็บ handle ไว้สั่งหยุดเอง")]
    public bool loop = false;

    [Header("เสียงวน (ใช้เฉพาะตอนติ๊ก Loop)")]
    [Tooltip("ต่อรอบเองด้วยนาฬิกาของระบบเสียง แทนการปล่อยให้ Unity วนให้\nการวนของ Unity มักมีช่องว่างสะดุดตอนวนกลับ โดยเฉพาะไฟล์บีบอัด (mp3/ogg) หรือไฟล์ที่ตั้ง Load Type เป็น Streaming\nปกติติ๊กไว้ ติ๊กออกเมื่ออยากกลับไปใช้การวนแบบเดิม")]
    public bool seamlessLoop = true;

    [Tooltip("ตัดหัวไฟล์ทิ้งกี่วินาทีตอนวน\nใช้เมื่อไฟล์มีความเงียบนำหน้า (ไฟล์ mp3 มักมีติดมาจากตัวเข้ารหัส) ซึ่งจะได้ยินเป็นจังหวะขาดตอนทุกรอบ")]
    public float loopStartTrim = 0f;

    [Tooltip("ตัดท้ายไฟล์ทิ้งกี่วินาทีตอนวน\nใช้เมื่อไฟล์มีความเงียบต่อท้าย ซึ่งเป็นสาเหตุที่พบบ่อยที่สุดของเสียงวนที่ฟังแล้วมีดีเลย์")]
    public float loopEndTrim = 0f;

    [Tooltip("เสียงมีตำแหน่งในฉาก (ยิ่งไกลยิ่งเบา) ต้องเล่นด้วยเวอร์ชันที่ส่งตำแหน่งเข้าไป\nติ๊กออก = เสียงดังเท่ากันทั้งจอ เหมาะกับเสียง UI")]
    public bool is3D = false;

    [Tooltip("ระยะที่เสียงเริ่มเบาลง (หน่วยเดียวกับระยะในฉาก) ใช้เฉพาะตอนติ๊ก Is 3D")]
    public float minDistance = 2f;
    [Tooltip("ระยะที่เสียงเงียบสนิท ใช้เฉพาะตอนติ๊ก Is 3D")]
    public float maxDistance = 20f;

    [Header("กันเสียงซ้อน")]
    [Tooltip("เว้นระยะขั้นต่ำระหว่างการเล่นเสียงนี้ 2 ครั้ง (วินาที)\nกันเคสที่โค้ดยิงเสียงเดิมทุกเฟรมจนกลายเป็นเสียงแตก ใส่ 0 = ไม่กัน")]
    public float minInterval = 0.02f;

    [Tooltip("จำนวนเสียงนี้ที่ดังพร้อมกันได้สูงสุด เกินกว่านี้จะไม่เล่นเพิ่ม ใส่ 0 = ไม่จำกัด")]
    public int maxSimultaneous = 4;

    [Tooltip("ห้ามสุ่มได้ไฟล์เดิมซ้ำติดกัน (ใช้ได้เมื่อใส่ clips มากกว่า 1 อัน)")]
    public bool avoidRepeat = true;

    // จำไฟล์ที่สุ่มได้ล่าสุด เอาไว้กันสุ่มซ้ำติดกัน
    [System.NonSerialized] private int lastClipIndex = -1;

    // เวลาที่เล่นเสียงนี้ครั้งล่าสุด (unscaledTime) เอาไว้เช็ค minInterval
    // ใช้ unscaled เพราะเสียงต้องกันซ้อนได้แม้ตอนเกมหยุดเวลา (เปิดสมุด/หน้าจอแพ้)
    [System.NonSerialized] public float lastPlayTime = -999f;

    public bool HasClip
    {
        get
        {
            if (clips == null) return false;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) return true;
            }

            return false;
        }
    }

    // สุ่มไฟล์เสียงมา 1 อัน ข้ามช่องที่เว้นว่างไว้ใน Inspector ไปเลย
    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;

        // เก็บเฉพาะช่องที่ใส่ไฟล์ไว้จริง ไม่งั้นสุ่มโดนช่องว่างแล้วจะเงียบไปดื้อ ๆ
        int validCount = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null) validCount++;
        }

        if (validCount == 0) return null;
        if (validCount == 1)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) return clips[i];
            }
        }

        int index = Random.Range(0, clips.Length);
        int guard = 0;

        // วนหาช่องที่ใช้ได้ และเลี่ยงช่องเดิมถ้าตั้ง avoidRepeat ไว้
        // guard กันลูปไม่รู้จบเวลาช่องที่ใช้ได้มีน้อยกว่าที่คิด
        while (guard < clips.Length * 4)
        {
            if (clips[index] != null && !(avoidRepeat && index == lastClipIndex && validCount > 1))
            {
                lastClipIndex = index;
                return clips[index];
            }

            index = (index + 1) % clips.Length;
            guard++;
        }

        // หาไม่เจอจริง ๆ เอาช่องแรกที่ใช้ได้ไปก่อน ดีกว่าเงียบ
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                lastClipIndex = i;
                return clips[i];
            }
        }

        return null;
    }

    public float PickPitch()
    {
        if (Mathf.Approximately(minPitch, maxPitch)) return minPitch;
        return Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
    }

    // ช่วงที่สั้นที่สุดที่ยังต่อรอบเองไหว สั้นกว่านี้ให้ปล่อยให้ Unity วนไปตามเดิม
    public const float MinLoopBodyLength = 0.05f;

    // ช่วงของไฟล์ที่เอามาวนจริง ๆ หลังตัดความเงียบหัวท้ายออกแล้ว (หน่วยเป็นวินาทีของตัวไฟล์ ยังไม่คิด pitch)
    public void GetLoopBody(AudioClip clip, out float start, out float length)
    {
        start = 0f;
        length = 0f;

        if (clip == null) return;

        float total = clip.length;

        // ไฟล์สั้นกว่าช่วงต่ำสุดอยู่แล้ว ตัดอะไรไม่ได้ ใช้ทั้งไฟล์ไปเลย
        if (total <= MinLoopBodyLength)
        {
            length = total;
            return;
        }

        start = Mathf.Clamp(loopStartTrim, 0f, total - MinLoopBodyLength);

        float end = total - Mathf.Max(0f, loopEndTrim);
        end = Mathf.Clamp(end, start + MinLoopBodyLength, total);

        length = end - start;
    }
}

// คลังเสียงทั้งเกม เป็นไฟล์ Asset (คลิกขวาใน Project > Create > MyGame > Sound Library)
// ผูกไฟล์เสียงไว้ที่นี่ที่เดียว โค้ดฝั่งเกมเรียกด้วย "ชื่อ" อย่างเดียว ไม่ต้องรู้จักไฟล์
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "MyGame/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("เสียงประกอบ (SFX)")]
    [Tooltip("เสียงสั้น ๆ ทั้งหมดในเกม เช่น เสียงกดปุ่ม เสียงย่างหมู เสียงลูกค้าเดินมา")]
    public List<SoundEntry> sfx = new List<SoundEntry>();

    [Header("เพลงประกอบ (BGM)")]
    [Tooltip("เพลงพื้นหลัง ปกติควรติ๊ก Loop ไว้ทุกเพลง")]
    public List<SoundEntry> bgm = new List<SoundEntry>();

    private Dictionary<string, SoundEntry> sfxLookup;
    private Dictionary<string, SoundEntry> bgmLookup;

    public SoundEntry GetSfx(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (sfxLookup == null) sfxLookup = BuildLookup(sfx, "SFX");

        SoundEntry entry;
        return sfxLookup.TryGetValue(id, out entry) ? entry : null;
    }

    public SoundEntry GetBgm(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (bgmLookup == null) bgmLookup = BuildLookup(bgm, "BGM");

        SoundEntry entry;
        return bgmLookup.TryGetValue(id, out entry) ? entry : null;
    }

    // เรียกตอนแก้คลังกลางเกม (หรือกด Play ใหม่ใน Editor) ให้ไปสร้างตารางค้นหาใหม่
    public void RebuildLookup()
    {
        sfxLookup = null;
        bgmLookup = null;
    }

    private Dictionary<string, SoundEntry> BuildLookup(List<SoundEntry> source, string label)
    {
        Dictionary<string, SoundEntry> lookup = new Dictionary<string, SoundEntry>();

        if (source == null) return lookup;

        for (int i = 0; i < source.Count; i++)
        {
            SoundEntry entry = source[i];

            if (entry == null || string.IsNullOrEmpty(entry.id)) continue;

            if (lookup.ContainsKey(entry.id))
            {
                // เตือนตั้งแต่ตอนสร้างตาราง ไม่งั้นจะไปงงตอนเล่นแล้วได้เสียงผิดตัว
                Debug.LogWarning($"[SoundLibrary] มีชื่อเสียง {label} ซ้ำกัน: \"{entry.id}\" ระบบจะใช้อันแรกที่เจอเท่านั้น", this);
                continue;
            }

            lookup.Add(entry.id, entry);
        }

        return lookup;
    }

    private void OnDisable()
    {
        // Unity เคลียร์ ScriptableObject ตอน reload domain ต้องล้างตารางทิ้งด้วย
        // ไม่งั้นตารางเก่าจะชี้ไปที่ SoundEntry ที่ตายไปแล้ว
        RebuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildLookup();
    }
#endif
}
