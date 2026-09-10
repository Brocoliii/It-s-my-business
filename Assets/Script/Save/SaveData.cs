using System;
using UnityEngine;

// ข้อมูลหนึ่งช่องเซฟ (หนึ่งไฟล์ .json ในเครื่องผู้เล่น)
//
// *** ห้ามลากไฟล์นี้ไปใส่ GameObject *** ไม่ใช่ MonoBehaviour เป็นแค่ที่เก็บข้อมูล
//
// เก็บ "จุดที่จะเล่นต่อ" เป็นหลัก (ฉากไหน + ด่านไหน) ส่วนตัวเลขที่เหลือเก็บไว้โชว์บนการ์ดช่องเซฟ
// ให้ผู้เล่นดูออกว่าช่องไหนคืออันไหน ไม่ได้เอาไปคืนค่าตอนโหลด
//
// ทำไมไม่คืนค่าพวกออเดอร์/ความผิดพลาด: ตัวเลขพวกนั้นเป็นของ "วันนั้น" ที่เล่นจบไปแล้ว
// เซฟเกิดตอนจบวัน แล้วโหลดกลับมาเริ่มที่ต้นวันถัดไป ซึ่งต้องเริ่มนับใหม่จากศูนย์อยู่แล้ว
[Serializable]
public class SaveData
{
    // ขึ้นเลขนี้เมื่อไรที่โครงสร้างข้อมูลเปลี่ยนจนอ่านของเก่าไม่ได้
    // ไฟล์เซฟที่เวอร์ชันไม่ตรงจะถูกมองว่าเป็นช่องว่าง แทนที่จะโหลดเข้าไปแล้วพังกลางเกม
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    [Header("จุดที่จะเล่นต่อ")]

    // ชื่อฉากที่จะโหลดตอนกดเล่นต่อจากช่องนี้
    public string sceneName = "";

    // ด่านที่จะเล่นต่อใน stages ของ GameManager ในฉากนั้น
    // ใช้ก็ต่อเมื่อ applyStageIndex = true เท่านั้น (ดูคำอธิบายข้างล่าง)
    public int stageIndex = 0;

    // ปกติเกมนี้แยกด่านเป็นคนละฉาก ฉากปลายทางเลยตั้ง Current Stage Index ของตัวเองไว้ถูกอยู่แล้ว
    // ถ้าเราไปเขียนทับด้วยเลขจากฉากก่อนหน้า จะกลายเป็นเล่นผิดด่าน
    // จะจริงก็ต่อเมื่อเซฟนี้เล่นต่อใน "ฉากเดิม" แล้วเลื่อนไปด่านถัดไปในลิสต์เท่านั้น
    public bool applyStageIndex = false;

    [Header("ไว้โชว์บนการ์ดช่องเซฟ")]

    // ชื่อด่านที่เพิ่งเล่นผ่าน (ตอนเซฟ) ไม่ใช่ด่านที่จะเล่นต่อ
    public string clearedStageName = "";
    public int completedOrders = 0;
    public int mistakes = 0;
    public int maxMistakes = 0;
    public int cluesCollected = 0;
    public int clueTarget = 0;

    // เวลาที่กดเซฟ เก็บเป็น Ticks แบบ UTC จะได้ไม่เพี้ยนตามโซนเวลา/รูปแบบวันที่ของเครื่อง
    public long savedAtTicks = 0;

    public bool isAutoSave = false;

    // ช่องเซฟที่ใช้ได้จริง ต้องรู้ว่าจะไปฉากไหนต่อ ไม่งั้นกดเล่นต่อก็ไปไหนไม่ได้
    public bool IsValid
    {
        get { return version == CurrentVersion && !string.IsNullOrEmpty(sceneName); }
    }

    // ข้อความบนการ์ด ต้องเป็นภาษาอังกฤษเท่านั้น
    // ฟอนต์ TextMeshPro ที่โปรเจกต์นี้ใช้ไม่มีตัวอักษรไทย ใส่ไทยแล้ว Editor ค้าง
    public string DescribeProgress()
    {
        string stage = string.IsNullOrWhiteSpace(clearedStageName) ? "Day cleared" : clearedStageName;
        return string.Format("{0} cleared  ·  Orders {1}  ·  Clues {2}/{3}  ·  Mistakes {4}/{5}",
            stage, completedOrders, cluesCollected, clueTarget, mistakes, maxMistakes);
    }

    public string DescribeTime()
    {
        // Ticks เพี้ยน (ไฟล์โดนแก้มือ) จะทำให้ตัวสร้าง DateTime โยน exception ทิ้ง เลยกันไว้ก่อน
        if (savedAtTicks <= 0 || savedAtTicks > DateTime.MaxValue.Ticks) return "";

        DateTime local = new DateTime(savedAtTicks, DateTimeKind.Utc).ToLocalTime();

        // ประกอบเองทีละช่อง ไม่ใช้รูปแบบวันที่ของเครื่อง เพราะบางเครื่องคืนเป็นปีพุทธศักราช/ตัวเลขไทย
        // ซึ่งฟอนต์ในเกมไม่มีตัวอักษรพวกนั้น
        return string.Format("{0:D4}-{1:D2}-{2:D2}  {3:D2}:{4:D2}",
            local.Year, local.Month, local.Day, local.Hour, local.Minute);
    }
}
