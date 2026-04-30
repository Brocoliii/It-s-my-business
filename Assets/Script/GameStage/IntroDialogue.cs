using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class IntroDialogue : MonoBehaviour
{
    [Header("การเดินของ NPC")]
    public Transform startPoint;
    public Transform stopPoint;
    public float walkSpeed = 5f;

    [Header("UI กรอบข้อความ")]
    public Transform dialogBubble;
    public TMP_Text dialogText;
    [Tooltip("ความเร็วในการพิมพ์ตัวอักษร (ยิ่งน้อยยิ่งพิมพ์เร็ว)")]
    public float typeSpeed = 0.05f; // ✨ เพิ่มช่องปรับความเร็วการพิมพ์

    private string[] dialogPages;
    private Vector3 savedBubbleScale;

    // ตัวแปรเช็คสถานะการคลิก
    private bool isTyping = false;
    private bool skipTyping = false;
    private bool isWaitingForNextPage = false;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            StageConfig currentStage = GameManager.Instance.GetCurrentStage();
            if (currentStage != null && !string.IsNullOrEmpty(currentStage.introMessage))
            {
                dialogPages = currentStage.introMessage.Split('\n');
            }
            else
            {
                dialogPages = new string[] { "ภารกิจวันนี้... เอ่อ... ฉันลืมบทน่ะ!" };
            }
        }

        savedBubbleScale = dialogBubble.localScale;
        dialogBubble.localScale = Vector3.zero;
        transform.position = startPoint.position;

        StartCoroutine(IntroSequence());
    }

    private void Update()
    {
        // ระบบรับการคลิกเมาส์
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isTyping)
            {
                // ถ้ากำลังพิมพ์อยู่ ให้ข้ามไปแสดงข้อความเต็มๆ ทันที
                skipTyping = true;
            }
            else if (isWaitingForNextPage)
            {
                // ถ้าพิมพ์เสร็จแล้ว ให้กดเพื่อไปหน้าถัดไป
                isWaitingForNextPage = false;
            }
        }
    }

    private IEnumerator IntroSequence()
    {
        // 1. เดินเข้ามาจากนอกจอ
        while (Vector3.Distance(transform.position, stopPoint.position) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, stopPoint.position, walkSpeed * Time.deltaTime);
            yield return null;
        }

        // 2. เด้งกรอบข้อความ (ตอนแรกจะยังไม่มีข้อความ)
        dialogText.text = "";
        yield return StartCoroutine(PopInAnimation(dialogBubble));

        // 3. เริ่มลูปแสดงข้อความทีละหน้า
        for (int i = 0; i < dialogPages.Length; i++)
        {
            string sentence = dialogPages[i].Trim();
            if (string.IsNullOrEmpty(sentence)) continue;

            // ✨ เริ่มกระบวนการพิมพ์ทีละตัวอักษร
            isTyping = true;
            skipTyping = false;
            dialogText.text = "";

            foreach (char letter in sentence.ToCharArray())
            {
                if (skipTyping) // ถ้าผู้เล่นกดคลิกข้าม
                {
                    dialogText.text = sentence; // โชว์ข้อความทั้งหมดทันที
                    break;
                }

                dialogText.text += letter;
                yield return new WaitForSeconds(typeSpeed); // รอเวลาตามความเร็วที่ตั้งไว้
            }

            // พิมพ์จบแล้ว รอผู้เล่นคลิกเพื่อไปหน้าต่อไป
            isTyping = false;
            isWaitingForNextPage = true;

            while (isWaitingForNextPage)
            {
                yield return null;
            }
        }

        // 4. หดกรอบหายไป
        yield return StartCoroutine(PopOutAnimation(dialogBubble));

        // 5. เดินกลับออกไปนอกจอ
        while (Vector3.Distance(transform.position, startPoint.position) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPoint.position, walkSpeed * Time.deltaTime);
            yield return null;
        }

        // 6. สั่งให้เกมเริ่ม และทำลายตัวเองทิ้ง
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        }
        Destroy(gameObject);
    }

    // ==========================================
    // 🎨 ฟังก์ชันช่วยทำแอนิเมชันตอนเด้ง
    // ==========================================
    private IEnumerator PopInAnimation(Transform target)
    {
        Vector3 originalScale = savedBubbleScale;
        target.localScale = Vector3.zero;
        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            target.localScale = originalScale * scaleFactor;
            yield return null;
        }
        target.localScale = originalScale;
    }

    private IEnumerator PopOutAnimation(Transform target)
    {
        float duration = 0.2f;
        float time = 0f;
        Vector3 startScale = target.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = Mathf.Lerp(1f, 0f, t * t);
            target.localScale = startScale * scaleFactor;
            yield return null;
        }
        target.localScale = Vector3.zero;
    }
}