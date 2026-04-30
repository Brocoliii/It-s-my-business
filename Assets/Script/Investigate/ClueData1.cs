using UnityEngine;

[System.Serializable]
public class ClueData1
{
    [TextArea(2, 3)]
    public string clueText;

    [Tooltip("ระยะเวลาที่ต้องกดค้างเพื่อแอบฟังเบาะแสนี้ (วินาที)")]
    public float listenDuration = 5f;
}
