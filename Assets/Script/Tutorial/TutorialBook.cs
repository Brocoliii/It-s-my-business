using UnityEngine;

public class TutorialBook : MonoBehaviour, IClickable
{
    public void OnClick()
    {
        if (TutorialBookUI.Instance != null)
        {
            TutorialBookUI.Instance.OpenBook();
        }
        else
        {
            Debug.LogWarning("Missing");
        }
    }
}