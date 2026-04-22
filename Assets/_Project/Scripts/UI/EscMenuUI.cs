using UnityEngine;
using UnityEngine.SceneManagement;

public class EscMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject escPanel;
    [SerializeField] private GameObject settingsPanel;
    private bool _isOpen;

    void Awake()
    {
        if (escPanel) escPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        escPanel.SetActive(_isOpen);

        Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = _isOpen;
    }

    public void OpenSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    public void QuitToLobby()
    {
        var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
        if (runner != null) runner.Shutdown();
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
