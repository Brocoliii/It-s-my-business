using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้าตาของ "แถวช่องเซฟ" หนึ่งแถว
//
// มีไว้ให้ทำ Prefab ช่องเซฟเองได้ตามใจ แทนที่จะใช้แถวสี่เหลี่ยมเรียบ ๆ ที่โค้ดสร้างให้
//
// วิธีทำ Prefab ช่องเซฟเอง
//   1. สร้าง UI > Button (หรือจัดกล่องของตัวเองแล้วใส่ Button ไว้ที่ตัวนอกสุด)
//   2. ใส่ข้อความ TextMeshPro ไว้ข้างในสองอัน: บรรทัดชื่อช่อง กับบรรทัดรายละเอียด
//   3. ลากสคริปต์นี้ใส่ตัวนอกสุดของ Prefab
//   4. ลากเอา Prefab ไปใส่ช่อง "Slot Row Prefab" ของ SaveSlotPanel ในฉาก
//
// ช่องต่าง ๆ ข้างล่างไม่ผูกก็ได้ ถ้าเว้นว่างไว้ระบบจะไปหาให้เองจากข้างใน Prefab
// (ปุ่มตัวแรก, ภาพพื้นหลังของปุ่ม, ข้อความตัวแรก = ชื่อช่อง, ข้อความตัวที่สอง = รายละเอียด)
//
// เรื่องความสูงของแถว: แถวไปเรียงอยู่ใน Vertical Layout Group ซึ่งเป็นคนกำหนดความสูงให้
// อยากคุมเองให้ใส่ Layout Element (Preferred Height) ไว้ที่ตัวนอกสุดของ Prefab
// ไม่ใส่ก็ได้ ระบบจะใส่ให้เองโดยยึดความสูงที่ตั้งไว้ใน Prefab เป็นค่าตั้งต้น
//
// ข้อความที่ถูกยัดลงมาเป็นภาษาอังกฤษทั้งหมด เพราะฟอนต์ TextMeshPro ที่โปรเจกต์นี้ใช้ไม่มีตัวอักษรไทย
[DisallowMultipleComponent]
public class SaveSlotRowView : MonoBehaviour
{
    [Header("ชิ้นส่วนของแถว (เว้นว่างได้ จะหาให้เองจากข้างใน)")]
    [Tooltip("ปุ่มที่ผู้เล่นกดเพื่อเลือกช่องนี้")]
    public Button button;
    [Tooltip("ภาพพื้นหลังของแถว ใช้ตอนเปลี่ยนสีตามสถานะ")]
    public Image background;
    [Tooltip("บรรทัดบน: ชื่อช่อง + เวลาที่บันทึก เช่น \"SLOT 1     2026-09-05  14:32\"")]
    public TextMeshProUGUI labelText;
    [Tooltip("บรรทัดล่าง: ความคืบหน้าที่เซฟไว้ หรือคำว่า Empty")]
    public TextMeshProUGUI detailText;

    [Header("ของที่จะเปิด/ปิดตามสถานะ (ไม่ใส่ก็ได้)")]
    [Tooltip("โผล่เฉพาะตอนช่องนี้ยังว่าง เช่นภาพจาง ๆ หรือเส้นประ")]
    public GameObject emptyState;
    [Tooltip("โผล่เฉพาะตอนช่องนี้มีเซฟอยู่ เช่นกรอบรูป ไอคอน หรือแถบสี")]
    public GameObject filledState;

    [Header("สีพื้นหลังตามสถานะ")]
    [Tooltip("ติ๊กออก = ไม่ให้ระบบไปแตะสีพื้นหลังเลย (ใช้ตอนทำ Prefab ที่มีอาร์ตของตัวเองแล้ว)")]
    public bool tintBackground = true;
    [Tooltip("สีตอนกดเลือกช่องนี้ได้")]
    public Color normalColor = new Color(0.16f, 0.18f, 0.23f, 1f);
    [Tooltip("สีตอนกดไม่ได้ (ช่องว่างตอนโหลด หรือช่องอัตโนมัติตอนเซฟ)")]
    public Color lockedColor = new Color(0.11f, 0.12f, 0.15f, 1f);
    [Tooltip("สีตอนรอให้กดยืนยันเขียนทับ")]
    public Color confirmColor = new Color(0.55f, 0.28f, 0.18f, 1f);

    [Header("สีข้อความตามสถานะ")]
    [Tooltip("ติ๊กออก = ไม่ให้ระบบไปแตะสีข้อความเลย")]
    public bool tintText = true;
    public Color labelColor = Color.white;
    public Color detailColor = new Color(1f, 1f, 1f, 0.62f);
    [Tooltip("สีข้อความตอนแถวนี้กดไม่ได้")]
    public Color lockedTextColor = new Color(1f, 1f, 1f, 0.32f);

    // ช่องไหนของ SaveManager ที่แถวนี้แทนอยู่ ตัวแผงเป็นคนใส่ให้ตอนสร้างแถว
    [HideInInspector] public bool isAuto;
    [HideInInspector] public int slotIndex;

    private bool hasResolved = false;

    // ช่องไหนไม่ได้ผูกไว้ใน Inspector ให้ไปหาเอาเองจากข้างใน Prefab
    // คนทำ Prefab จะได้ไม่ต้องผูกอะไรเลยถ้าโครงเป็นแบบมาตรฐาน (ปุ่ม + ข้อความสองบรรทัด)
    public void ResolveMissingReferences()
    {
        if (hasResolved) return;
        hasResolved = true;

        if (button == null) button = GetComponent<Button>();
        if (button == null) button = GetComponentInChildren<Button>(true);

        if (background == null && button != null) background = button.targetGraphic as Image;
        if (background == null) background = GetComponent<Image>();

        if (labelText == null || detailText == null)
        {
            // (true) = เอาตัวที่ปิดอยู่มาด้วย เพราะแถวถูกสร้างตอนที่แผงยังปิดอยู่
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

            if (labelText == null && texts.Length > 0) labelText = texts[0];
            if (detailText == null && texts.Length > 1) detailText = texts[1];
        }

        if (labelText == null)
        {
            Debug.LogWarning("[SaveSlotRowView] แถวช่องเซฟ '" + name + "' ไม่มีข้อความ TextMeshPro เลย ชื่อช่องจะไม่ขึ้น", this);
        }
    }

    // ตัวแผงเรียกทุกครั้งที่ต้องวาดแถวใหม่ (ตอนเปิดแผง / หลังเซฟ / ตอนเข้า-ออกสถานะยืนยันเขียนทับ)
    public void Apply(string label, string detail, bool interactable, bool isConfirming, bool hasSave)
    {
        if (button != null) button.interactable = interactable;

        if (background != null && tintBackground)
        {
            background.color = isConfirming ? confirmColor : (interactable ? normalColor : lockedColor);
        }

        if (labelText != null)
        {
            labelText.text = label;
            if (tintText) labelText.color = interactable ? labelColor : lockedTextColor;
        }

        if (detailText != null)
        {
            detailText.text = detail;
            if (tintText) detailText.color = interactable ? detailColor : lockedTextColor;
        }

        if (emptyState != null) emptyState.SetActive(!hasSave);
        if (filledState != null) filledState.SetActive(hasSave);
    }
}
