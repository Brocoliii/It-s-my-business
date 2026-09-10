using UnityEngine;

// วางไว้ตัวเดียวในแต่ละฉาก เพื่อบอกว่าฉากนี้ให้เล่นเพลงอะไร
// ไม่ต้องเขียนโค้ดเพิ่ม แค่ลากใส่ GameObject เปล่า ๆ ในฉากแล้วพิมพ์ชื่อเพลง
//
// เรียกเพลงเดิมซ้ำจะไม่เริ่มใหม่ เพลงเลยเล่นต่อเนื่องข้ามฉากได้
// (เช่นด่าน 1 กับด่าน 2 ใช้เพลงเดียวกัน เพลงจะไม่กระตุกกลับไปเริ่มต้นตอนเปลี่ยนฉาก)
public class SceneMusic : MonoBehaviour
{
    [Header("เพลงของฉากนี้")]
    [Tooltip("ชื่อเพลงในคลังเสียง (ช่อง Id ในหมวด BGM) เช่น \"bgm_stage\"\nปล่อยว่างแล้วไปใส่ Clip ด้านล่างแทนก็ได้")]
    public string bgmId = SoundIds.BgmStage;

    [Tooltip("ทางลัด: ลากไฟล์เพลงใส่ตรงนี้เลยโดยไม่ต้องเพิ่มในคลัง\nถ้าใส่ช่องนี้ ระบบจะใช้ช่องนี้แทน Bgm Id ด้านบน")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float clipVolume = 1f;

    [Header("การไล่เสียง")]
    [Tooltip("เวลาที่ใช้ไล่เพลงเก่าลงและเพลงใหม่ขึ้น (วินาที) ใส่ -1 = ใช้ค่ากลางที่ตั้งไว้ใน SoundManager")]
    public float fadeDuration = -1f;

    [Tooltip("ติ๊กถูก = เริ่มเพลงใหม่ตั้งแต่ต้นเสมอ แม้จะเป็นเพลงเดิมที่เล่นค้างมาจากฉากก่อน")]
    public bool restartIfSame = false;

    [Header("จังหวะที่เริ่มเล่น")]
    [Tooltip("ติ๊กออกถ้าอยากสั่งเล่นเองจากโค้ด/ปุ่ม ด้วยการเรียก Play()")]
    public bool playOnStart = true;

    [Tooltip("หน่วงกี่วินาทีก่อนเพลงจะเริ่ม (เผื่อฉากมีแอนิเมชันเปิดฉากก่อน)")]
    public float delay = 0f;

    private void Start()
    {
        if (!playOnStart) return;

        if (delay > 0f)
        {
            Invoke(nameof(Play), delay);
            return;
        }

        Play();
    }

    // ผูกกับปุ่มใน Inspector ได้เลย หรือเรียกจากโค้ดก็ได้
    public void Play()
    {
        if (bgmClip != null)
        {
            SoundManager.Instance.PlayBGM(bgmClip, clipVolume, fadeDuration, restartIfSame);
            return;
        }

        if (string.IsNullOrWhiteSpace(bgmId)) return;

        SoundManager.Instance.PlayBGM(bgmId, fadeDuration, restartIfSame);
    }

    public void Stop()
    {
        // ไม่ใช้ Instance ตรง ๆ เพราะถ้ายังไม่มีตัวจัดการอยู่ ก็ไม่มีเพลงให้หยุดอยู่แล้ว
        // จะได้ไม่ไปสร้างตัวจัดการขึ้นมาเปล่า ๆ แค่เพื่อสั่งหยุดเพลงที่ไม่มีอยู่
        if (!SoundManager.Exists) return;

        SoundManager.Instance.StopBGM(fadeDuration);
    }
}
