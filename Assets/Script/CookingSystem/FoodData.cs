using UnityEngine;

[CreateAssetMenu(fileName = "New Food Data", menuName = "MyGame/FoodData")]
public class FoodData : ScriptableObject
{
    [Header("ข้อมูลพื้นฐาน")]
    public string foodName;
    public Sprite foodIcon;

    [Header("รูปภาพความสุก (Visuals)")]
    public Sprite rawSprite;     
    public Sprite mediumSprite;  
    public Sprite cookedSprite;  
    public Sprite burntSprite;   

    [Header("การตั้งค่าเวลา (วินาที)")]
    public float mediumTime = 5f; 
    public float cookTime = 10f;  
    public float burnTime = 15f;

    [Header("รูปภาพเครื่องปรุง (Seasoning Overlays)")]
    public Sprite sauceSprite;
    public Sprite spicyLevel1Sprite;
    public Sprite spicyLevel2Sprite;
    public Sprite spicyLevel3Sprite;
}