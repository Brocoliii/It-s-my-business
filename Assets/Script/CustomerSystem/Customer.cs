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

    [Header("UI ออเดอร์ (ลากจากในลูกของตัวเองมาใส่)")]
    public Animator bubbleAnimator;

    [Tooltip("พรีแฟบรูปไอคอนอาหาร (ลากจาก Project มาใส่)")]
    public GameObject foodIconPrefab;
    [Tooltip("กล่องที่จะเอาไอคอนอาหารไปเรียงใส่ (ลากตัวแบคกราวด์กรอบออเดอร์มาใส่)")]
    public Transform foodContainer;

    public Image spicyIcon;
    public Image sauceIcon;
    public Image patienceFill;

    [Header("ฐานข้อมูลรูปภาพ (ลากรูปจาก Project มาใส่)")]
    public Sprite[] spicySprites;
    public Sprite sauceSprite;
    public Sprite noSauceSprite;  

    [HideInInspector] public OrderData myOrder;
    [HideInInspector] public int mySlotIndex;
    private CustomerManager manager;
    private bool isLeaving = false;

    public void Init(OrderData order, int slotIndex, CustomerManager mgr)
    {
        myOrder = order;
        mySlotIndex = slotIndex;
        manager = mgr;
        currentPatience = maxPatience;
        isLeaving = false;

        string sauceText = order.wantedSauce ? "ทาซอสด้วย" : "ไม่เอาซอส";

        if (foodIconPrefab != null && foodContainer != null)
        {
            foreach (FoodData food in order.wantedFoods)
            {
                GameObject newIconObj = Instantiate(foodIconPrefab, foodContainer, false);
                Image iconImage = newIconObj.GetComponent<Image>();

                if (iconImage != null && food.foodIcon != null)
                {
                    iconImage.sprite = food.foodIcon;
                }
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

        StartCoroutine(PopInAnimation());

        Debug.Log($"<color=orange>[ออเดอร์ช่อง {slotIndex}]</color> ลูกค้าสั่ง: <b>{foodNames}</b> | เผ็ด: <b>{order.wantedSpicyLevel}</b> | <b>{sauceText}</b>");
    }

    private void Update()
    {
        if (isLeaving) return;

        currentPatience -= Time.deltaTime;

        if (patienceFill != null)
        {
            patienceFill.fillAmount = currentPatience / maxPatience;

            if (patienceFill.fillAmount > 0.5f) patienceFill.color = Color.green;
            else if (patienceFill.fillAmount > 0.25f) patienceFill.color = new Color(1f, 0.5f, 0f);
            else patienceFill.color = Color.red;
        }

        if (currentPatience <= 0)
        {
            Leave(false);
        }
    }

    public void ReceiveCup(Cup cup)
    {
        if (isLeaving || cup.contents.Count == 0) return;

        if (cup.contents.Count != myOrder.wantedFoods.Count)
        {
            Debug.Log("ทำผิด! จำนวนอาหารไม่ตรงกับที่สั่ง");
            Destroy(cup.gameObject);
            Leave(false);
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
            Leave(true);
        }
        else
        {
            Debug.Log("ทำผิด!");
            Destroy(cup.gameObject);
            Leave(false);
        }
    }

    private void Leave(bool isSatisfied)
    {
        isLeaving = true;
        if (!isSatisfied) GameManager.Instance.AddMistake();
        else Debug.Log("ลูกค้าพอใจ จ่ายเงิน!");

        manager.OnCustomerLeft(mySlotIndex);
        Destroy(gameObject, 0.5f);
    }

    private IEnumerator PopInAnimation()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
        float duration = 0.4f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            transform.localScale = originalScale * scaleFactor;
            yield return null;
        }
        transform.localScale = originalScale;
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