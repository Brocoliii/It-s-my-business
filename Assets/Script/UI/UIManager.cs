using UnityEngine;
using TMPro; // สำคัญ: ต้องใส่เพื่อใช้ TextMeshPro
using System.Collections;

public class UIManager : MonoBehaviour
{
    // ทำเป็น Singleton เพื่อให้ไฟล์อื่นเรียกใช้ UI ได้ง่ายๆ
    public static UIManager Instance { get; private set; }

    [Header("หน้าจอหลัก (HUD)")]
    public TextMeshProUGUI clueCounterText; // ข้อความบอกจำนวนเบาะแส
    public TextMeshProUGUI centerWarningText; // ตัวหนังสือเตือนกลางจอ (เช่น เวลานับถอยหลัง)

    [Header("ระบบ UI แอบฟัง")]
    public Canvas listeningCanvas;
    public UnityEngine.UI.Image listeningFill;

    [Header("หน้าต่างป๊อปอัป")]
    public GameObject endOfDayPanel; // หน้าจอทึบๆ ที่จะเด้งขึ้นมาตอนจบวัน

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // ปิดข้อความเตือน และหน้าจอจบวันไว้ก่อนตอนเริ่มเกม
        if (centerWarningText != null) centerWarningText.gameObject.SetActive(false);
        if (endOfDayPanel != null) endOfDayPanel.SetActive(false);
        ShowListeningBar(false);
    }

    // ฟังก์ชันสำหรับอัปเดตหลอดเบาะแส
    public void UpdateClueText(int current, int total)
    {
        if (clueCounterText != null)
        {
            clueCounterText.text = $"เบาะแส: {current} / {total}";
        }
    }

    // ฟังก์ชันนับถอยหลังกลางจอ (เรียกจาก GameManager)
    public void StartCountdownDisplay(int seconds)
    {
        StartCoroutine(CountdownRoutine(seconds));
    }

    private IEnumerator CountdownRoutine(int seconds)
    {
        centerWarningText.gameObject.SetActive(true);

        for (int i = seconds; i > 0; i--)
        {
            centerWarningText.text = $"จบวันใน... {i}";
            yield return new WaitForSeconds(1f);
        }

        centerWarningText.text = "หมดเวลา!";
        yield return new WaitForSeconds(1f);

        centerWarningText.gameObject.SetActive(false);

        // เด้งหน้าจอจบวันขึ้นมา
        if (endOfDayPanel != null) endOfDayPanel.SetActive(true);


    }

    public void ShowListeningBar(bool isVisible)
    {
        if (listeningCanvas != null)
        {
            listeningCanvas.enabled = isVisible;
        }

        // ถ้าระบบสั่งเปิด ให้รีเซ็ตหลอดกลับเป็น 0 ด้วย
        if (isVisible && listeningFill != null)
        {
            listeningFill.fillAmount = 0f;
        }
    }

    public void UpdateListeningBar(float progress)
    {
        if (listeningFill != null)
        {
            listeningFill.fillAmount = Mathf.Clamp01(progress);
        }
    }
}
