using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ใส่เสียงกดปุ่ม/เสียงเลื่อนเมาส์ผ่านปุ่ม โดยไม่ต้องไปแก้โค้ดปุ่มทีละตัว
//
// ใช้ได้ 2 แบบ:
//   1) ลากใส่ที่ปุ่มตัวเดียว        -> ปุ่มนั้นมีเสียง
//   2) ลากใส่ที่ Canvas หรือแผงแม่ + ติ๊ก Include Children
//      -> ปุ่มทุกตัวที่อยู่ข้างใน (รวมตัวที่ปิดอยู่) มีเสียงหมดในทีเดียว
//
// วิธีที่ 2 คือวิธีที่เร็วที่สุด: ลากใส่ Canvas ของแต่ละฉากอันเดียว จบ
public class UIButtonSound : MonoBehaviour
{
    [Header("เสียง")]
    [Tooltip("ชื่อเสียงตอนกดปุ่ม ปล่อยว่าง = ไม่มีเสียงกด")]
    public string clickSoundId = SoundIds.UiClick;

    [Tooltip("ชื่อเสียงตอนเลื่อนเมาส์ไปโดนปุ่ม ปล่อยว่าง = ไม่มีเสียงตอนเลื่อนผ่าน")]
    public string hoverSoundId = SoundIds.UiHover;

    [Header("ขอบเขต")]
    [Tooltip("ติ๊กถูก = ใส่เสียงให้ปุ่มทุกตัวที่อยู่ใต้ GameObject นี้ (รวมตัวที่ปิดอยู่)\nติ๊กออก = ใส่ให้เฉพาะปุ่มที่อยู่บน GameObject นี้ตัวเดียว")]
    public bool includeChildren = true;

    [Tooltip("ปุ่มที่ไม่อยากให้มีเสียง (เช่นปุ่มที่มีเสียงเฉพาะของตัวเองอยู่แล้ว)")]
    public Button[] ignoredButtons;

    private void Start()
    {
        if (includeChildren)
        {
            // (true) = เอาปุ่มที่ปิดอยู่มาด้วย เพราะแผงตั้งค่า/แผงแพ้ มักจะปิดไว้ตอนเริ่มฉาก
            Button[] buttons = GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                Bind(buttons[i]);
            }

            return;
        }

        Bind(GetComponent<Button>());
    }

    private void Bind(Button button)
    {
        if (button == null) return;
        if (IsIgnored(button)) return;

        // ตัวส่งเสียงจริงติดอยู่กับปุ่มแต่ละตัว เพราะเสียงตอนเลื่อนเมาส์ผ่าน
        // ต้องมีตัวรับ event อยู่บน GameObject ของปุ่มนั้นเอง จะฝากไว้ที่แผงแม่ไม่ได้
        UIButtonSoundHook hook = button.GetComponent<UIButtonSoundHook>();

        if (hook == null) hook = button.gameObject.AddComponent<UIButtonSoundHook>();

        hook.Setup(button, clickSoundId, hoverSoundId);
    }

    private bool IsIgnored(Button button)
    {
        if (ignoredButtons == null) return false;

        for (int i = 0; i < ignoredButtons.Length; i++)
        {
            if (ignoredButtons[i] == button) return true;
        }

        return false;
    }
}

// ตัวส่งเสียงที่ติดอยู่กับปุ่มแต่ละตัว ปกติไม่ต้องลากใส่เอง UIButtonSound จะใส่ให้ตอนรันไทม์
public class UIButtonSoundHook : MonoBehaviour, IPointerEnterHandler
{
    private Button button;
    private string clickSoundId;
    private string hoverSoundId;
    private bool isBound = false;

    public void Setup(Button targetButton, string click, string hover)
    {
        clickSoundId = click;
        hoverSoundId = hover;

        if (isBound) return;

        button = targetButton;

        if (button != null)
        {
            // ใช้ AddListener ไม่ใช่ RemoveAllListeners เพราะปุ่มมีงานของตัวเองผูกไว้อยู่แล้ว
            // และผูกแค่ครั้งเดียวตลอดอายุปุ่ม (isBound) ไม่งั้นเปิด/ปิดแผงหลายรอบแล้วเสียงจะซ้อนกัน
            button.onClick.AddListener(PlayClick);
            isBound = true;
        }
    }

    private void PlayClick()
    {
        if (string.IsNullOrEmpty(clickSoundId)) return;

        SoundManager.Instance.PlaySFX(clickSoundId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(hoverSoundId)) return;

        // ปุ่มที่กดไม่ได้ (เช่นยังไม่ถึงเงื่อนไข) ไม่ควรมีเสียงตอบสนอง
        if (button != null && !button.interactable) return;

        SoundManager.Instance.PlaySFX(hoverSoundId);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
    }
}
