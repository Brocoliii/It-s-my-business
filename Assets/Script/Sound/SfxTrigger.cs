using UnityEngine;

// ใส่เสียงให้ของชิ้นหนึ่งโดยไม่ต้องเขียนโค้ดเลย
// ลากใส่ GameObject แล้วเลือกว่าให้เสียงดังตอนไหน
//
// เหมาะกับเสียงเล็ก ๆ ที่ไม่คุ้มจะไปแก้สคริปต์เกม เช่น
//   - เสียงเอฟเฟกต์ตอนโผล่ขึ้นมา (Play On Enable)
//   - เสียงตอนถูกทำลาย (Play On Destroy)
//   - เสียงเตาย่างที่ดังวนอยู่ตลอดตอนเปิดใช้งาน (Loop While Enabled)
public class SfxTrigger : MonoBehaviour
{
    [Header("เสียงที่จะเล่น")]
    [Tooltip("ชื่อเสียงในคลัง (ช่อง Id ในหมวด SFX)")]
    public string soundId;

    [Tooltip("ทางลัด: ลากไฟล์เสียงใส่ตรงนี้เลยโดยไม่ต้องเพิ่มในคลัง\nถ้าใส่ช่องนี้ ระบบจะใช้ช่องนี้แทน Sound Id ด้านบน (ใช้กับ Loop ไม่ได้)")]
    public AudioClip clip;
    [Range(0f, 1f)] public float clipVolume = 1f;

    [Tooltip("ติ๊กถูก = เสียงดังจากตำแหน่งของวัตถุนี้ (ต้องติ๊ก Is 3D ในคลังด้วย)\nติ๊กออก = ดังเท่ากันทั้งจอ")]
    public bool playAtMyPosition = false;

    [Header("ให้ดังตอนไหน")]
    public bool playOnStart = false;
    public bool playOnEnable = false;
    public bool playOnDisable = false;
    public bool playOnDestroy = false;
    [Tooltip("ดังตอนมีอะไรชนเข้ามาในพื้นที่ Trigger (ต้องมี Collider2D ที่ติ๊ก Is Trigger)")]
    public bool playOnTriggerEnter = false;
    [Tooltip("ดังตอนถูกคลิก (ต้องมี Collider2D อยู่บนวัตถุนี้)")]
    public bool playOnMouseDown = false;

    [Header("เสียงวน")]
    [Tooltip("ติ๊กถูก = เล่นเสียงวนตลอดเวลาที่วัตถุนี้เปิดใช้งานอยู่ แล้วหยุดเองตอนถูกปิด/ถูกทำลาย\n(ต้องเป็นเสียงที่ตั้งค่า Loop ไว้ในคลัง และต้องใช้ Sound Id ไม่ใช่ Clip)")]
    public bool loopWhileEnabled = false;

    [Tooltip("เวลาที่ใช้ไล่เสียงวนให้เบาลงตอนหยุด (วินาที) ใส่ 0 = ตัดทันที")]
    public float loopFadeOutDuration = 0.15f;

    private SoundHandle loopHandle = SoundHandle.None;

    private void Start()
    {
        if (playOnStart) Play();
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();

        if (loopWhileEnabled && !string.IsNullOrEmpty(soundId))
        {
            loopHandle = SoundManager.Instance.PlayLoop(soundId, playAtMyPosition ? transform : null);
        }
    }

    private void OnDisable()
    {
        // ต้องหยุดเสียงวนก่อนเช็คอย่างอื่น ไม่งั้นวัตถุถูกปิดแล้วเสียงจะดังค้างต่อไปเรื่อย ๆ
        loopHandle.Stop(loopFadeOutDuration);
        loopHandle = SoundHandle.None;

        if (playOnDisable && CanPlayNow()) Play();
    }

    private void OnDestroy()
    {
        if (playOnDestroy && CanPlayNow()) Play();
    }

    // ตอนเปลี่ยนฉากหรือปิดเกม Unity จะปิด/ทำลายทุกอย่างในฉากรวดเดียว
    // ถ้าไม่กันไว้ เสียง "ตอนถูกทำลาย" ของทั้งฉากจะดังพรึ่บพร้อมกันตอนกดเปลี่ยนด่าน
    private bool CanPlayNow()
    {
        if (!SoundManager.Exists) return false;
        return gameObject.scene.isLoaded;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playOnTriggerEnter) Play();
    }

    private void OnMouseDown()
    {
        if (playOnMouseDown) Play();
    }

    // เรียกได้จากปุ่มใน Inspector, จาก UnityEvent หรือจาก Animation Event
    public void Play()
    {
        if (clip != null)
        {
            SoundManager.Instance.PlaySFX(clip, clipVolume);
            return;
        }

        if (string.IsNullOrWhiteSpace(soundId)) return;

        if (playAtMyPosition)
        {
            SoundManager.Instance.PlaySFX(soundId, transform.position);
            return;
        }

        SoundManager.Instance.PlaySFX(soundId);
    }

    // เรียกเสียงชื่ออื่นแบบเจาะจง เผื่อผูกกับ UnityEvent หลายตัวบนวัตถุเดียวกัน
    public void PlayById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        SoundManager.Instance.PlaySFX(id);
    }
}
