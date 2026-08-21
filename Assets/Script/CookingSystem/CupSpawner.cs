using UnityEngine;

public class CupSpawner : MonoBehaviour, IClickable
{
    [Header("การตั้งค่าจุดกำเนิดถ้วย")]
    public GameObject cupPrefab;
    public Transform spawnPoint;

    [Header("พื้นที่ที่ถ้วยจะเด้งไปตกหลังเกิด")]
    [Tooltip("ถ้าไม่ใส่ จะใช้ spawnPoint เป็นทั้งจุดเกิดและจุดตกแทน")]
    public Transform cupArea;

    private GameObject currentCupInScene;

    public void OnClick()
    {
        if (currentCupInScene == null)
        {
            SpawnNewCup();
        }
        else
        {
            Debug.Log("มีถ้วยอยู่ในฉากแล้ว รอให้หมดก่อนค่อยเรียกใหม่");
        }
    }

    private void SpawnNewCup()
    {
        Vector3 originPos = spawnPoint.position;
        Vector3 targetPos = cupArea != null ? cupArea.position : originPos;

        currentCupInScene = Instantiate(cupPrefab, targetPos, Quaternion.identity);

        // เรียกทันทีในเฟรมเดียวกับ Instantiate เพื่อให้ทัน Start() ของ Cup เอง
        // ถ้วยจะได้โผล่ที่ spawnPoint แล้วเด้งไปตกที่ cupArea แทนที่จะร่วงลงมาตรงๆ ที่ตัวเอง
        Cup cup = currentCupInScene.GetComponent<Cup>();
        if (cup != null) cup.PlaySpawnAnimation(originPos, targetPos);
    }
}
