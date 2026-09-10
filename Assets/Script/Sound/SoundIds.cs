// รายชื่อเสียงทั้งหมดที่โค้ดในเกมเรียกใช้
//
// มีไว้กันพิมพ์ชื่อผิด ถ้าเรียก SoundManager.Instance.PlaySFX(SoundIds.UiClick)
// แล้วพิมพ์ผิด คอมไพเลอร์จะฟ้องตั้งแต่ตอนคอมไพล์ ไม่ใช่ไปเงียบเอาตอนเล่นเกม
//
// ค่าข้างในนี้ต้อง "ตรงกับช่อง Id ในไฟล์คลังเสียง (SoundLibrary)" เป๊ะ ๆ
// เสียงไหนยังไม่ได้ใส่ในคลัง เกมจะไม่พัง แค่เงียบไปเฉย ๆ (และขึ้นเตือนใน Console)
// เพราะงั้นค่อย ๆ ทยอยใส่ไฟล์เสียงทีละตัวได้ ไม่ต้องหาให้ครบก่อนถึงจะเล่นเกมได้
public static class SoundIds
{
    // ---------- เพลงพื้นหลัง (BGM) ----------
    public const string BgmMainMenu = "bgm_main_menu";
    public const string BgmIntro = "bgm_intro";
    public const string BgmStage = "bgm_stage";
    public const string BgmEndOfDay = "bgm_end_of_day";
    public const string BgmNotebook = "bgm_notebook";   // ดังตอนสมุดหลักฐานเปิดขึ้นมา (ช่วงเลือกคนร้าย)
    public const string BgmWin = "bgm_win";
    public const string BgmLose = "bgm_lose";

    // ---------- UI ----------
    public const string UiClick = "ui_click";
    public const string UiHover = "ui_hover";
    public const string UiBack = "ui_back";
    public const string UiCountdownTick = "ui_countdown_tick";
    public const string UiNotebookOpen = "ui_notebook_open";
    public const string UiNotebookClose = "ui_notebook_close";
    public const string UiPageTurn = "ui_page_turn";   // ดังตอนหน้าประวัติผู้ต้องสงสัยไถลเข้ามา (กดการ์ดในสมุด)
    public const string UiStamp = "ui_stamp";
    public const string UiShutter = "ui_shutter";
    public const string UiDayComplete = "ui_day_complete";   // ดังตอนป้าย DAY COMPLETE! เด้งขึ้นจอ (ก่อนม่านปิด)
    public const string UiReady = "ui_ready";   // ดังตอนไอคอน Ready? เด้งขึ้นจอ
    public const string UiGo = "ui_go";         // ดังตอนไอคอน Go!!! เด้งขึ้นจอ

    // ---------- ระบบทำอาหาร ----------
    public const string FoodPickup = "food_pickup";
    public const string FoodDrop = "food_drop";
    public const string FoodPlaceGrill = "food_place_grill";
    public const string FoodFlip = "food_flip";
    public const string GrillSizzle = "grill_sizzle";     // เสียงวน ต้องเล่นด้วย PlayLoop
    public const string FoodCooked = "food_cooked";
    public const string FoodBurnt = "food_burnt";
    public const string TrashDrop = "trash_drop";
    public const string CupSpawn = "cup_spawn";
    public const string FoodIntoCup = "food_into_cup";                // ดังตอนอาหารตกลงถึงก้นถ้วยพอดี (ไม่ใช่ตอนปล่อยเมาส์)
    public const string SauceBrush = "sauce_brush";                  // ดังทุกจังหวะที่ปาดแปรงบนอาหาร ยกเว้นจังหวะสุดท้ายที่ซอสติด (จังหวะนั้นเป็นของ sauce_done)
    public const string SauceDone = "sauce_done";                    // ดังตอนซอสติดบนอาหารสำเร็จเท่านั้น
    public const string ChiliShake = "chili_shake";                  // ดังตอนพริกลงบนอาหารสำเร็จเท่านั้น
    public const string ChiliBottleShake = "chili_bottle_shake";     // ดังทุกจังหวะที่เขย่าขวดแต่พริกยังไม่ลง (คนละตัวกับ chili_shake ที่ดังตอนใส่พริกสำเร็จ)
    public const string ChiliBottlePickup = "chili_bottle_pickup";   // ตอนคว้าขวดพริกขึ้นมา (คนละตัวกับ chili_bottle_shake ที่ดังตอนเขย่า)
    public const string SauceBrushPickup = "sauce_brush_pickup";     // ตอนคว้าแปรงซอสขึ้นมา (คนละตัวกับ sauce_brush ที่ดังตอนปาดแปรง)
    public const string SeasoningPlace = "seasoning_place";

    // ---------- บทสนทนา (Intro) ----------
    public const string DialogueArrive = "dialogue_arrive";   // ตอน NPC เริ่มเดินเข้ามาในฉาก
    public const string DialogueType = "dialogue_type";       // เสียงพิมพ์ตัวอักษรตอน NPC กำลังพูด
    public const string DialogueLeave = "dialogue_leave";     // ตอน NPC พูดจบแล้วเดินกลับออกไป

    // ---------- ลูกค้า ----------
    public const string CustomerArrive = "customer_arrive";
    public const string CustomerOrder = "customer_order";
    public const string CustomerHappy = "customer_happy";
    public const string CustomerAngry = "customer_angry";
    public const string CustomerLeave = "customer_leave";

    // ---------- ระบบสืบสวน ----------
    public const string InvestigateAppear = "investigate_appear";     // ดังตอนกลุ่มโผล่ขึ้นมาในฉาก (คนละตัวกับ investigate_start ที่ดังตอนเริ่มแอบฟัง)
    public const string InvestigateStart = "investigate_start";
    public const string InvestigateCorrect = "investigate_correct";
    public const string InvestigateWrong = "investigate_wrong";
    public const string InvestigateSuccess = "investigate_success";
    public const string InvestigateTimeout = "investigate_timeout";   // ดังตอนหมดเวลาแล้วฟังไม่จบ เบาะแสหลุด (คนละตัวกับ investigate_success)
    public const string BinocularsOn = "binoculars_on";
    public const string BinocularsOff = "binoculars_off";

    // ---------- ผลของเกม ----------
    public const string Mistake = "mistake";
    public const string StageWin = "stage_win";
    public const string StageLose = "stage_lose";
}
