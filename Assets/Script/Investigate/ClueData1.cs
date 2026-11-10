using UnityEngine;
using UnityEngine.Localization;

[System.Serializable]
public class ClueData1
{
    [TextArea(2, 3)]
    public string clueText;

    [Tooltip("ข้อความเบาะแสสำหรับแสดงในสมุดโน้ต รองรับหลายภาษา ถ้าปล่อยว่างจะใช้ Clue Text")]
    public LocalizedString clueTextLocalized;

    [Tooltip("�������ҷ���ͧ����ҧ�����ͺ�ѧ����ʹ�� (�Թҷ�)")]
    public float listenDuration = 5f;
}
