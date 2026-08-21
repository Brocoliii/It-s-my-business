using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class OrderData
{
    public List<FoodData> wantedFoods;
    public int wantedSpicyLevel;
    public bool wantedSauce;
}

public class Customer : MonoBehaviour
{
    [Header("ข้อมูลลูกค้า")]
    public float maxPatience = 90f;
    private float currentPatience;

    [Tooltip("เปอร์เซ็นต์ความอดทนที่เริ่มเปลี่ยนเป็นสปริทตอนใกล้หมด")]
    [Range(0f, 1f)] public float lowPatienceThreshold = 0.35f;

    [Header("สปริทลูกค้า")]
    [Tooltip("คอมโพเนนต์ SpriteRenderer ของลูกค้า (ถ้าไม่มีจะไม่เปลี่ยนสปริท)")]
    public SpriteRenderer customerRenderer;
    [Tooltip("สปริทตอนปกติ")]
    public Sprite normalSprite;
    [Tooltip("สปริทตอนใกล้หมดความอดทน")]
    public Sprite lowPatienceSprite;
    [Tooltip("สปริทตอนความอดทนหมดหรือกำลังจากไป")]
    public Sprite exhaustedSprite;
    [Tooltip("สปริทตอนลูกค้าพอใจเมื่อรับอาหารถูก")]
    public Sprite correctReactionSprite;
    [Tooltip("สปริทตอนลูกค้าไม่พอใจเมื่อรับอาหารผิด")]
    public Sprite wrongReactionSprite;
    [Tooltip("ระยะเวลาที่แสดงสปริทตอบสนองก่อนจากไป")]
    public float reactionDuration = 1.5f;

    [Header("Investigate Hide Effect")]
    [Tooltip("ระยะเวลาที่สปริทลูกค้าค่อยๆ ดำลง ก่อนจะเริ่มจางหาย")]
    public float blackenDuration = 0.25f;
    [Tooltip("ระยะเวลาที่สปริทลูกค้า (ตอนดำแล้ว) ค่อยๆ จางหายไป")]
    public float fadeOutDuration = 0.35f;

    [Header("การเดินเข้ามา (ตอนลูกค้าปรากฏตัว)")]
    [Tooltip("ความเร็วในการเดินเข้ามายังจุดยืน")]
    public float walkInSpeed = 5f;
    [Tooltip("ความสูงของการเด้งขึ้นลงตอนเดินเข้ามา (ใส่ 0 ถ้าไม่ต้องการเด้ง)")]
    public float walkBobHeight = 0.15f;
    [Tooltip("ความเร็วของการเด้งขึ้นลงตอนเดินเข้ามา (ยิ่งมากยิ่งเดินถี่)")]
    public float walkBobSpeed = 10f;

    [Header("UI ออเดอร์ (ลากจากในลูกของตัวเองมาใส่)")]
    [Tooltip("ถ้าใส่ Animator นี้ไว้ จะสั่ง Trigger \"Show\" ตอน Canvas ปรากฏ แทนการเล่นแอนิเมชันขยายจากโค้ด")]
    public Animator bubbleAnimator;
    [Tooltip("Canvas ที่แสดงออเดอร์เหนือหัวลูกค้า จะถูกซ่อนตอนผู้เล่นกำลังสืบสวน (กดค้างที่กลุ่มเป้าหมาย)")]
    public GameObject customerCanvas;
    [Tooltip("ระยะเวลาแอนิเมชันขยาย Canvas ออเดอร์ตอนลูกค้าเดินมาถึงจุดยืน (ใช้ตอนไม่มี bubbleAnimator)")]
    public float canvasPopDuration = 0.3f;

    [Tooltip("พรีแฟบรูปไอคอนอาหาร (ลากจาก Project มาใส่)")]
    public GameObject foodIconPrefab;
    [Tooltip("กล่องที่จะเอาไอคอนอาหารไปเรียงใส่ (ลากตัวแบคกราวด์กรอบออเดอร์มาใส่)")]
    public Transform foodContainer;

    [Tooltip("ตำแหน่งของไอคอนอาหาร (สูงสุด 3 ตำแหน่ง)")]
    public Vector3[] foodIconPositions = new Vector3[3];

    public Image spicyIcon;
    public Image sauceIcon;
    public Image patienceFill;

    [Header("สีแถบความอดทน (Patience Bar Colors)")]
    [Tooltip("สีตอนความอดทนเหลือมากกว่า 50%")]
    public Color highPatienceColor = Color.green;
    [Tooltip("สีตอนความอดทนเหลือ 25%-50%")]
    public Color medPatienceColor = new Color(1f, 0.5f, 0f);
    [Tooltip("สีตอนความอดทนเหลือน้อยกว่า 25%")]
    public Color lowPatienceColor = Color.red;

    [Header("ฐานข้อมูลรูปภาพ (ลากรูปจาก Project มาใส่)")]
    public Sprite[] spicySprites;
    public Sprite sauceSprite;
    public Sprite noSauceSprite;

    [HideInInspector] public OrderData myOrder;
    [HideInInspector] public int mySlotIndex;
    private CustomerManager manager;
    private bool isLeaving = false;
    private bool isWalkingIn = false;
    private bool isShowingReaction = false;
    private Color normalRendererColor = Color.white;
    private Coroutine visibilityCoroutine;
    private bool isHiddenForInvestigation = false;
    private Vector3 spawnPosition;

    // standPosition คือจุดยืนจริงของลูกค้า (ตำแหน่งช่อง) ส่วน transform.position ตอนเรียกเมธอดนี้
    // ควรถูกตั้งเป็นจุดที่ลูกค้าจะโผล่ออกมาก่อน (เช่น นอกจอ) แล้วค่อยเดินเข้ามายัง standPosition
    public void Init(OrderData order, int slotIndex, CustomerManager mgr, Vector3 standPosition)
    {
        myOrder = order;
        mySlotIndex = slotIndex;
        manager = mgr;
        currentPatience = maxPatience;
        isLeaving = false;
        isWalkingIn = true;
        spawnPosition = transform.position;

        if (customerRenderer != null) normalRendererColor = customerRenderer.color;
        if (customerCanvas != null) customerCanvas.SetActive(false);

        string sauceText = order.wantedSauce ? "ทาซอสด้วย" : "ไม่เอาซอส";

        if (foodIconPrefab != null && foodContainer != null)
        {
            int foodIndex = 0;
            foreach (FoodData food in order.wantedFoods)
            {
                GameObject newIconObj = Instantiate(foodIconPrefab, foodContainer, false);
                Image iconImage = newIconObj.GetComponent<Image>();

                if (iconImage != null && food.foodIcon != null)
                {
                    iconImage.sprite = food.foodIcon;
                }

                // ตั้งตำแหน่งของไอคอน
                if (foodIndex < foodIconPositions.Length)
                {
                    RectTransform rectTransform = newIconObj.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localPosition = foodIconPositions[foodIndex];
                    }
                }

                foodIndex++;
            }
        }

        if (spicyIcon != null && spicySprites.Length > 0)
        {
            if (order.wantedSpicyLevel == 0)
            {
                spicyIcon.gameObject.SetActive(false);
            }
            else
            {
                spicyIcon.gameObject.SetActive(true);
                int spriteIndex = Mathf.Clamp(order.wantedSpicyLevel - 1, 0, spicySprites.Length - 1);
                spicyIcon.sprite = spicySprites[spriteIndex];
            }
        }

        if (sauceIcon != null)
        {
            sauceIcon.gameObject.SetActive(true);

            if (order.wantedSauce)
            {
                if (sauceSprite != null) sauceIcon.sprite = sauceSprite;
            }
            else
            {
                if (noSauceSprite != null) sauceIcon.sprite = noSauceSprite;
            }
        }

        string foodNames = "";
        foreach (var f in order.wantedFoods) foodNames += f.foodName + " ";

        ApplyCustomerSprite(false);
        StartCoroutine(WalkInAnimation(standPosition));

        Debug.Log($"<color=orange>[ออเดอร์ช่อง {slotIndex}]</color> ลูกค้าสั่ง: <b>{foodNames}</b> | เผ็ด: <b>{order.wantedSpicyLevel}</b> | <b>{sauceText}</b>");
    }

    private void Update()
    {
        if (isLeaving || isWalkingIn || isShowingReaction) return;

        currentPatience -= Time.deltaTime;

        if (patienceFill != null)
        {
            patienceFill.fillAmount = currentPatience / maxPatience;

            if (patienceFill.fillAmount > 0.5f) patienceFill.color = highPatienceColor;
            else if (patienceFill.fillAmount > 0.25f) patienceFill.color = medPatienceColor;
            else patienceFill.color = lowPatienceColor;
        }

        if (currentPatience <= 0)
        {
            StartCoroutine(ShowExhaustedAndLeave());
        }
        else
        {
            ApplyCustomerSprite(false);
        }
    }

    // animate = false ใช้ตอนลูกค้าเพิ่งเกิดขึ้นมาระหว่างที่ผู้เล่นกำลังสืบสวนอยู่แล้ว
    // ต้องซ่อนทันทีเลย ไม่งั้นจะโผล่มาเป็นสปริทปกติแวบนึงก่อนค่อยเล่นแอนิเมชันจางหาย
    public void SetCustomerVisible(bool isVisible, bool animate = true)
    {
        isHiddenForInvestigation = !isVisible;

        // ถ้าเลิกสืบสวนแต่ลูกค้ายังเดินไม่ถึงจุดยืน อย่าเพิ่งโชว์ Canvas
        // ให้ WalkInAnimation เป็นคนสั่งโชว์เองตอนเดินถึง (พร้อมแอนิเมชันปรากฏ) แทน
        bool shouldSkipCanvasChange = isVisible && isWalkingIn;
        if (customerCanvas != null && !shouldSkipCanvasChange) customerCanvas.SetActive(isVisible);

        if (customerRenderer == null) return;

        if (visibilityCoroutine != null)
        {
            StopCoroutine(visibilityCoroutine);
            visibilityCoroutine = null;
        }

        if (!animate)
        {
            if (isVisible)
            {
                customerRenderer.enabled = true;
                customerRenderer.color = normalRendererColor;
            }
            else
            {
                customerRenderer.enabled = false;
            }
            return;
        }

        visibilityCoroutine = StartCoroutine(isVisible ? FadeInRoutine() : FadeOutRoutine());
    }

    // ค่อยๆ ดำลงก่อน แล้วค่อยจางหาย (ตอนผู้เล่นเริ่มสืบสวน)
    private IEnumerator FadeOutRoutine()
    {
        Color blackOpaque = new Color(0f, 0f, 0f, normalRendererColor.a);
        yield return LerpRendererColor(customerRenderer.color, blackOpaque, blackenDuration);

        Color blackTransparent = new Color(0f, 0f, 0f, 0f);
        yield return LerpRendererColor(blackOpaque, blackTransparent, fadeOutDuration);

        customerRenderer.enabled = false;
        visibilityCoroutine = null;
    }

    // ค่อยๆ ปรากฏขึ้นมาเป็นเงาดำก่อน แล้วค่อยคืนสีปกติ (ตอนผู้เล่นเลิกสืบสวน)
    private IEnumerator FadeInRoutine()
    {
        Color blackTransparent = new Color(0f, 0f, 0f, 0f);
        customerRenderer.color = blackTransparent;
        customerRenderer.enabled = true;

        Color blackOpaque = new Color(0f, 0f, 0f, normalRendererColor.a);
        yield return LerpRendererColor(blackTransparent, blackOpaque, fadeOutDuration);

        yield return LerpRendererColor(blackOpaque, normalRendererColor, blackenDuration);

        visibilityCoroutine = null;
    }

    private IEnumerator LerpRendererColor(Color from, Color to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            customerRenderer.color = Color.Lerp(from, to, time / duration);
            yield return null;
        }

        customerRenderer.color = to;
    }

    public void ReceiveCup(Cup cup)
    {
        if (isLeaving || isWalkingIn || cup.contents.Count == 0) return;

        if (cup.contents.Count != myOrder.wantedFoods.Count)
        {
            Debug.Log("ทำผิด! จำนวนอาหารไม่ตรงกับที่สั่ง");
            Destroy(cup.gameObject);
            StartCoroutine(ShowReactionAndLeave(false));
            return;
        }

        List<FoodData> checklist = new List<FoodData>(myOrder.wantedFoods);
        bool isCorrect = true;

        foreach (var itemInCup in cup.contents)
        {

            if (itemInCup.state != FoodInstance.CookState.Cooked ||
                itemInCup.spicy != myOrder.wantedSpicyLevel ||
                itemInCup.sauce != myOrder.wantedSauce)
            {
                isCorrect = false;
                break;
            }

            if (checklist.Contains(itemInCup.data))
            {
                checklist.Remove(itemInCup.data);
            }
            else
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect && checklist.Count == 0)
        {
            Debug.Log("ถูกต้อง!");
            Destroy(cup.gameObject);
            StartCoroutine(ShowReactionAndLeave(true));
        }
        else
        {
            Debug.Log("ทำผิด!");
            Destroy(cup.gameObject);
            StartCoroutine(ShowReactionAndLeave(false));
        }
    }

    private void ApplyCustomerSprite(bool isExhausted)
    {
        if (customerRenderer == null || isShowingReaction) return;

        if (isExhausted)
        {
            if (exhaustedSprite != null)
            {
                customerRenderer.sprite = exhaustedSprite;
            }
            return;
        }

        if (currentPatience <= maxPatience * lowPatienceThreshold && lowPatienceSprite != null)
        {
            customerRenderer.sprite = lowPatienceSprite;
        }
        else if (normalSprite != null)
        {
            customerRenderer.sprite = normalSprite;
        }
    }

    private IEnumerator ShowReactionAndLeave(bool isSatisfied)
    {
        if (isLeaving) yield break;

        isShowingReaction = true;
        Sprite reactionSprite = isSatisfied ? correctReactionSprite : wrongReactionSprite;

        if (customerRenderer != null && reactionSprite != null)
        {
            customerRenderer.sprite = reactionSprite;
        }

        yield return new WaitForSeconds(reactionDuration);
        isShowingReaction = false;
        Leave(isSatisfied);
    }

    // ตอนความอดทนหมด ให้ค้างสปริท exhausted ไว้ครู่นึงก่อนค่อยเดินจากไป เหมือนตอนรับอาหารถูก/ผิด
    private IEnumerator ShowExhaustedAndLeave()
    {
        ApplyCustomerSprite(true);
        isShowingReaction = true;

        yield return new WaitForSeconds(reactionDuration);
        isShowingReaction = false;
        Leave(false);
    }

    private void Leave(bool isSatisfied)
    {
        isLeaving = true;
        if (!isSatisfied) GameManager.Instance.AddMistake();
        else Debug.Log("ลูกค้าพอใจ จ่ายเงิน!");

        manager.OnCustomerLeft(mySlotIndex);
        StartCoroutine(WalkOutAnimation());
    }

    // ลูกค้าเดินเข้ามาจากตำแหน่งที่เกิด (นอกจอ) ยัง standPosition (จุดยืนในช่อง)
    // ชะลอความเร็วตอนใกล้ถึง (ease-out) พร้อมเด้งขึ้นลงตอนเดิน เหมือนกับ NPC ใน IntroDialogue
    private IEnumerator WalkInAnimation(Vector3 standPosition)
    {
        yield return WalkTo(standPosition);

        isWalkingIn = false;

        // ถ้าผู้เล่นกำลังสืบสวนอยู่พอดีตอนเดินมาถึง ห้ามโชว์ Canvas ออกมา
        // ต้องรอให้ SetCustomerVisible(true) จาก CustomerManager สั่งแสดงตอนเลิกสืบสวนแทน
        if (customerCanvas != null && !isHiddenForInvestigation) StartCoroutine(ShowOrderCanvasAnimation());
    }

    // หลังจากลูกค้าถือสปริทตอบสนอง (ถูก/ผิด) ค้างไว้ครบ reactionDuration แล้ว
    // ให้เดินกลับไปยังจุดที่เกิด (spawnPosition) ก่อนค่อยหายไปจากฉาก
    private IEnumerator WalkOutAnimation()
    {
        if (customerCanvas != null && customerCanvas.activeSelf) StartCoroutine(HideOrderCanvasAnimation());

        yield return WalkTo(spawnPosition);

        Destroy(gameObject);
    }

    // เดินจากตำแหน่งปัจจุบันไปยัง targetPosition แบบชะลอความเร็วตอนใกล้ถึง (ease-out)
    // พร้อมเด้งขึ้นลงตอนเดิน ใช้ร่วมกันทั้งตอนเดินเข้ามาและเดินกลับออกไป
    private IEnumerator WalkTo(Vector3 targetPosition)
    {
        Vector3 logicalPos = transform.position;
        float totalDistance = Vector3.Distance(logicalPos, targetPosition);
        float easeZone = Mathf.Max(totalDistance * 0.3f, 0.01f);
        float bobTimer = 0f;

        while (Vector3.Distance(logicalPos, targetPosition) > 0.01f)
        {
            float remaining = Vector3.Distance(logicalPos, targetPosition);
            float easeFactor = Mathf.Clamp01(remaining / easeZone);
            float currentSpeed = walkInSpeed * Mathf.Lerp(0.25f, 1f, easeFactor);
            logicalPos = Vector3.MoveTowards(logicalPos, targetPosition, currentSpeed * Time.deltaTime);

            bobTimer += Time.deltaTime * walkBobSpeed;
            // คูณด้วย easeFactor ให้การเด้งค่อยๆ แผ่วลงจนเหลือ 0 พอดีตอนถึงจุดหมาย
            // ไม่งั้นพอลูปจบแล้วบังคับ position ให้ตรงจุดยืนเป๊ะ จะเห็นเป็นการกระตุกสะดุดลงกะทันหัน
            float bobOffset = Mathf.Abs(Mathf.Sin(bobTimer)) * walkBobHeight * easeFactor;
            transform.position = logicalPos + Vector3.up * bobOffset;

            yield return null;
        }
        transform.position = targetPosition;
    }

    // แสดง Canvas ออเดอร์พร้อมแอนิเมชันตอนลูกค้าเดินมาถึงจุดยืน
    // ถ้ามี bubbleAnimator จะสั่ง Trigger "Show" ให้ Animator Controller เล่นเอง
    // ถ้าไม่มีจะเล่นแอนิเมชันขยายจาก 0 ขึ้นมาแบบเด้ง (ease-out back) จากโค้ดแทน
    private IEnumerator ShowOrderCanvasAnimation()
    {
        customerCanvas.SetActive(true);

        if (bubbleAnimator != null)
        {
            bubbleAnimator.SetTrigger("Show");
            yield break;
        }

        Transform canvasTransform = customerCanvas.transform;
        Vector3 targetScale = canvasTransform.localScale;
        canvasTransform.localScale = Vector3.zero;

        float time = 0f;
        while (time < canvasPopDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / canvasPopDuration);
            float overshoot = 1.70158f;
            float easedT = t - 1f;
            float easeOutBack = 1f + (overshoot + 1f) * Mathf.Pow(easedT, 3f) + overshoot * Mathf.Pow(easedT, 2f);
            canvasTransform.localScale = targetScale * easeOutBack;
            yield return null;
        }
        canvasTransform.localScale = targetScale;
    }

    // ซ่อน Canvas ออเดอร์พร้อมแอนิเมชันตอนลูกค้ากำลังจะจากไป (อิ่มแล้ว/โดนดุ/หมดความอดทน)
    // ถ้ามี bubbleAnimator จะสั่ง Trigger "Hide" ให้ Animator Controller เล่นเอง
    // ถ้าไม่มีจะเล่นแอนิเมชันหดขนาดลงเหลือ 0 จากโค้ดแทน
    private IEnumerator HideOrderCanvasAnimation()
    {
        if (bubbleAnimator != null)
        {
            bubbleAnimator.SetTrigger("Hide");
            yield break;
        }

        Transform canvasTransform = customerCanvas.transform;
        Vector3 startScale = canvasTransform.localScale;

        float time = 0f;
        while (time < canvasPopDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / canvasPopDuration);
            canvasTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        canvasTransform.localScale = Vector3.zero;

        customerCanvas.SetActive(false);
    }

    private IEnumerator PopOutAnimation()
    {
        float duration = 0.3f;
        float time = 0f;
        Vector3 startScale = transform.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = Mathf.Lerp(1f, 0f, t * t);
            transform.localScale = startScale * scaleFactor;
            yield return null;
        }
        Destroy(gameObject);
    }
}