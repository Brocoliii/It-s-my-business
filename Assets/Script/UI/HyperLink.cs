using UnityEngine;

public class HyperLink : MonoBehaviour
{
    public void OpenLink(string link)
    {
        Application.OpenURL(link);
    }

}
