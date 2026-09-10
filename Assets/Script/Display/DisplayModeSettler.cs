using UnityEngine;

// ตัวช่วยลดอาการ "สลับโหมดจอแล้วสีเพี้ยน" (ภาพมืดลงและสีจัดเกินจริง)
//
// อาการนี้เกิดเฉพาะโปรเจกต์ที่ตั้ง Color Space เป็น Linear (โปรเจกต์นี้ตั้งไว้แบบนั้น)
// ปกติ Unity ขอ backbuffer (ที่พักภาพก่อนออกจอ) เป็นชนิด sRGB
// การ์ดจอจะได้แปลงค่าสีจาก linear เป็น sRGB ให้ตอนเขียนภาพลงไป ภาพที่ออกจอถึงจะถูกต้อง
//
// แต่ทุกครั้งที่เปลี่ยนโหมดจอ Unity ต้องรื้อ backbuffer ทิ้งแล้วสร้างใหม่
// ตัวที่สร้างใหม่มีสิทธิ์กลับมาโดยไม่มีชนิด sRGB ติดมาด้วย พอไม่มีการแปลง
// ค่า linear ดิบ ๆ ก็ถูกส่งออกจอตรง ๆ ซึ่งค่ามันต่ำกว่าค่า sRGB ของสีเดียวกัน
// ภาพเลยออกมามืดและสีจัดเกินจริง และค้างแบบนั้นจนกว่าจะมีการรื้อสร้างใหม่อีกรอบ
//
// ทำไมต้องแยกเป็นสองจังหวะ:
//   สั่งเปลี่ยนทั้งโหมดและขนาดพร้อมกันในคำสั่งเดียว = รื้อสร้างใหม่ครั้งใหญ่ครั้งเดียว
//   ซึ่งเป็นจังหวะที่เจออาการนี้บ่อยที่สุด (เช่น เต็มจอของจริงที่ 1280x720 -> ไร้ขอบที่ 1920x1080)
//   เลยแยกเป็นเปลี่ยนโหมดก่อน แล้วรออีกสองสามเฟรมค่อยเปลี่ยนขนาด
//   กลายเป็นการรื้อสร้างสองครั้งเล็ก ๆ แทน โอกาสค้างในสภาพที่ไม่มี sRGB น้อยลง
//
// *** นี่เป็นการแก้ที่ปลายเหตุ ต้นเหตุอยู่ในตัว Unity เอง ***
// ต้อง Build ออกมาทดสอบถึงจะรู้ว่าพอสำหรับเครื่อง/ไดรเวอร์นั้นหรือเปล่า
// ถ้า Build แล้วยังเพี้ยนอยู่ ให้เลิกเปลี่ยนโหมดจอระหว่างเล่น
// แล้วไปใช้วิธีให้โหมดใหม่มีผลตอนเปิดเกมครั้งถัดไปแทน (ตอนเปิดเกมยังไม่มีภาพให้เพี้ยน)
//
// ไม่ต้องวางในฉาก DisplaySettings สร้างตัวนี้ขึ้นมาเองตอนต้องใช้
public class DisplayModeSettler : MonoBehaviour
{
    private const string ObjectName = "DisplayModeSettler";

    // รอกี่เฟรมก่อนสั่งจังหวะที่สอง
    //
    // ต้องรอเป็นเฟรมจริง ๆ สั่งติดกันในเฟรมเดียวไม่ได้
    // เพราะ Unity เก็บคำสั่งเปลี่ยนขนาดจอไว้ไปทำตอนจบเฟรม
    // สั่งซ้ำก่อนที่ของเดิมจะมีผล ก็เท่ากับของเดิมถูกทิ้ง เหลือการรื้อสร้างครั้งใหญ่ครั้งเดียวเหมือนเดิม
    private const int WaitFrames = 3;

    private static DisplayModeSettler instance;

    private bool isWaiting = false;
    private int framesLeft = 0;
    private Vector2Int targetSize;
    private FullScreenMode targetMode;

    // นัดไว้ว่าอีกสองสามเฟรมค่อยสั่งขนาดนี้ เรียกจาก DisplaySettings.Apply() ตอนที่มีการเปลี่ยนโหมด
    public static void Schedule(Vector2Int size, FullScreenMode mode)
    {
        EnsureExists();
        if (instance == null) return;

        instance.isWaiting = true;
        instance.framesLeft = WaitFrames;
        instance.targetSize = size;
        instance.targetMode = mode;
    }

    // ผู้เล่นกดเลือกอย่างอื่นก่อนที่ของที่นัดไว้จะถึงคิว ของเก่าก็ไม่ต้องสั่งแล้ว
    public static void Cancel()
    {
        if (instance == null) return;
        instance.isWaiting = false;
    }

    private static void EnsureExists()
    {
        if (instance != null) return;

        GameObject go = new GameObject(ObjectName);
        instance = go.AddComponent<DisplayModeSettler>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        // กันเคสมีคนเผลอลากสคริปต์นี้ใส่ฉากเองด้วย จะได้ไม่ทำงานซ้อนกันสองตัว
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!isWaiting) return;

        framesLeft--;
        if (framesLeft > 0) return;

        isWaiting = false;

        // โหมดไม่ใช่ตัวที่นัดไว้แล้ว แปลว่ามีคำสั่งใหม่แซงไปก่อน ของเก่าก็ไม่ต้องสั่ง
        if (Screen.fullScreenMode != targetMode) return;

        // ขนาดตรงอยู่แล้วก็ไม่ต้องสั่ง (เช่นสลับโหมดโดยที่ความละเอียดเท่าเดิมทั้งสองโหมด)
        if (Screen.width == targetSize.x && Screen.height == targetSize.y) return;

        Screen.SetResolution(targetSize.x, targetSize.y, targetMode);
    }
}
