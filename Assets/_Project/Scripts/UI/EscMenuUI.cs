using UnityEngine;
using Fusion;
public class EscMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject escPanel;
    [SerializeField] private GameObject settingsPanel;
    private bool _isOpen;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        escPanel.SetActive(_isOpen);
        Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = _isOpen;
    }

    public void OnContinue() => Toggle();
    public void OnSettings() => settingsPanel.SetActive(true);
    public void OnLeaveRoom() => _ = FindFirstObjectByType<NetworkRunner>()?.Shutdown();
}
