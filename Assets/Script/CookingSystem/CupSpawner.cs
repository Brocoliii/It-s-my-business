using UnityEngine;

public class CupSpawner : MonoBehaviour, IClickable
{
    [Header("การตั้งค่าการเสก")]
    public GameObject cupPrefab;     
    public Transform spawnPoint;     

    private GameObject currentCupInScene; 

    public void OnClick()
    {
        if (currentCupInScene == null)
        {
            SpawnNewCup();
        }
        else
        {
            Debug.Log("มีถ้วยวางอยู่บนโต๊ะแล้วนะ!");
        }
    }

    private void SpawnNewCup()
    {
        currentCupInScene = Instantiate(cupPrefab, spawnPoint.position, Quaternion.identity);
    }
}