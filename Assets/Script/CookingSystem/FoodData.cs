using UnityEngine;

[CreateAssetMenu(fileName = "New Food Data", menuName = "MyGame/FoodData")]
public class FoodData : ScriptableObject
{
    [Header("�����ž�鹰ҹ")]
    public string foodName;
    public Sprite foodIcon;

    [Header("�ٻ�Ҿ�����ء (Visuals)")]
    public Sprite rawSprite;
    public Sprite mediumSprite;
    public Sprite cookedSprite;
    public Sprite burntSprite;

    [Header("��õ�駤������ (�Թҷ�)")]
    public float mediumTime = 5f;
    public float cookTime = 10f;
    public float burnTime = 15f;

    [Header("�ٻ�Ҿ����ͧ��ا (Seasoning Overlays)")]

    public Sprite sauceSprite;
    public Sprite spicyLevel1Sprite;
    public Sprite spicyLevel2Sprite;
    public Sprite spicyLevel3Sprite;

    [Header("Spicy Overlays when hasSauce (Sauce + Spicy Combo)")]
    public Sprite sauceSpicyLevel1Sprite;
    public Sprite sauceSpicyLevel2Sprite;
    public Sprite sauceSpicyLevel3Sprite;
}