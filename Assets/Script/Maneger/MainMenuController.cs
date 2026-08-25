using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;

    [SerializeField] private GameObject settingsPanel;

    [Header("Scene to Load")]
    [SerializeField] private string gameplaySceneName = "GameScene";

    private void Start()
    {
        ShowMainMenu();
    }

    

    public void OnClickStart()
    {
        SceneManager.LoadScene(gameplaySceneName);
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

    

    public void OnClickBack()
    {
        ShowMainMenu();
    }

    

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}