using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;

    [SerializeField] private GameObject settingsPanel;

    [Header("Scene to Load")]
    [SerializeField] private string gameplaySceneName = "GameScene";

    [Header("เฟดดำตอนกดเริ่มเกม")]
    [Tooltip("ผูก ScreenFader ในฉากเองก็ได้ ถ้าเว้นว่างไว้ระบบจะหา/สร้างให้เองตอนเริ่มฉาก")]
    [SerializeField] private ScreenFader screenFader;

    [Header("ปุ่มย้อนกลับในแผงตั้งค่า")]
    [Tooltip("ปุ่ม \"ย้อนกลับ\" ที่อยู่ในแผง Settings ลากมาใส่ช่องนี้ช่องเดียว\nไม่ต้องไปผูก On Click ที่ตัวปุ่มเอง ระบบผูกให้ตอนเริ่มฉาก")]
    [SerializeField] private Button settingsBackButton;

    [Header("ปุ่มโหลดเกม")]
    [Tooltip("ปุ่ม \"โหลดเกม\" ในเมนูหลัก ลากมาใส่ช่องนี้ช่องเดียว\nไม่ต้องไปผูก On Click ที่ตัวปุ่มเอง ระบบผูกให้ตอนเริ่มฉาก\nแผงเลือกช่องเซฟถูกสร้างให้เองตอนกด ไม่ต้องเตรียมอะไรในฉาก")]
    [SerializeField] private Button loadButton;
    [Tooltip("ติ๊กถูก = ปิดปุ่มโหลดไว้ตอนที่ยังไม่มีเซฟสักช่อง ผู้เล่นจะได้ไม่กดแล้วเจอแผงเปล่า")]
    [SerializeField] private bool disableLoadButtonWhenEmpty = true;

    // กันกดปุ่มเริ่มซ้ำระหว่างที่ม่านกำลังเฟดดำอยู่
    private bool isStarting;

    private void Awake()
    {
        // ผูกที่ Awake เพราะแผงตั้งค่าเริ่มมาแบบปิดอยู่ ปุ่มข้างในเลยยังไม่ทำงานถ้าไปรอผูกทีหลัง
        // ผูกจากโค้ดแทนการผูกใน Inspector จะได้ไม่มีเคสลืมผูกแล้วปุ่มกดไม่ติด
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(OnClickBack);
        if (loadButton != null) loadButton.onClick.AddListener(OnClickLoad);

        // เตรียมม่านดำไว้ตั้งแต่ต้นฉาก จะได้ไม่ต้องเสียเวลาสร้างตอนกดปุ่มแล้วเฟดกระตุก
        if (screenFader == null) screenFader = ScreenFader.FindOrCreate();
    }

    private void Start()
    {
        ShowMainMenu();
    }



    // กดเริ่มเกม: เฟดดำจนมืดสนิทก่อน แล้วค่อยโหลดฉากด่าน 1
    public void OnClickStart()
    {
        if (isStarting) return;

        if (screenFader == null)
        {
            // ไม่ควรเกิด เพราะ Awake สร้างให้แล้ว ถ้าหลุดมาถึงตรงนี้จะได้รู้ว่าทำไมมันไม่เฟด
            Debug.LogWarning("[MainMenu] ไม่มี ScreenFader เลยเข้าด่านแบบไม่เฟดดำ");
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        isStarting = true;
        screenFader.FadeOutAndLoadScene(gameplaySceneName);
    }

    // กดโหลดเกม: เปิดแผงเลือกช่องเซฟ (ช่องอัตโนมัติ + ช่องผู้เล่น)
    // แผงสร้างตัวเองตอนถูกเรียกครั้งแรก ไม่ต้องเตรียมอะไรไว้ในฉาก
    public void OnClickLoad()
    {
        if (isStarting) return;

        SaveSlotPanel.OpenLoadPanel();
    }

    public void OnClickSetting()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }



    // กลับจากแผงตั้งค่ามาหน้าเมนูหลัก
    public void OnClickBack()
    {
        ShowMainMenu();
    }



    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // เช็คทุกครั้งที่กลับมาหน้าเมนูหลัก ไม่ใช่แค่ตอนเริ่มฉาก
        // เผื่อผู้เล่นเพิ่งลบเซฟทิ้งหรือเพิ่งเซฟมาจากที่อื่นในรอบเดียวกัน
        if (loadButton != null && disableLoadButtonWhenEmpty)
        {
            loadButton.interactable = SaveManager.HasAnySave();
        }
    }

    private void OnDestroy()
    {
        if (settingsBackButton != null) settingsBackButton.onClick.RemoveListener(OnClickBack);
        if (loadButton != null) loadButton.onClick.RemoveListener(OnClickLoad);
    }
}