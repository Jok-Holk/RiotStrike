using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
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

        // Nếu mở menu: Hiện chuột (None). Nếu đóng menu: Khóa chuột vào tâm (Locked)
        Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;

        // Chỉ hiện con trỏ chuột khi menu đang mở
        Cursor.visible = _isOpen;
    }

    public void OnContinue() => Toggle();
    public void OnSettings() => settingsPanel.SetActive(true);
    public void OnLeaveRoom()
    {
        SceneManager.LoadScene(0);
        Debug.Log("exit_lobby");
    }
}
