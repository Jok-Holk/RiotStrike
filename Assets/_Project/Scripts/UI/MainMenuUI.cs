using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;       // Button_Play
    [SerializeField] private Button settingButton;    // Button_Setting
    [SerializeField] private Button quitButton;       // Button_Quit

    [Header("Canvas References")]
    [SerializeField] private GameObject canvasMainMenu;   // Canvas_MainMenu (self)
    [SerializeField] private GameObject canvasLobby;      // Canvas_Lobby
    [SerializeField] private GameObject canvasSettings;   // Canvas_Settings

    void Start()
    {
        playButton.onClick.AddListener(OnPlayClick);
        settingButton.onClick.AddListener(OnSettingClick);
        quitButton.onClick.AddListener(OnQuitClick);

        // Đảm bảo chỉ MainMenu hiện lúc đầu
        ShowMainMenu();
    }

    void ShowMainMenu()
    {
        canvasMainMenu.SetActive(true);
        canvasLobby.SetActive(false);
        canvasSettings.SetActive(false);
    }

    void OnPlayClick()
    {
        canvasMainMenu.SetActive(false);
        canvasLobby.SetActive(true);
    }

    void OnSettingClick()
    {
        // Settings là overlay — MainMenu vẫn giữ nguyên phía sau
        canvasSettings.SetActive(true);
    }

    void OnQuitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}