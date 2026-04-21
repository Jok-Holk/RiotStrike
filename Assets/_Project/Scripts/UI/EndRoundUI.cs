
using UnityEngine.UI;
using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.SceneManagement; // Thư viện quan trọng nhất cho đoạn code này
public class EndRoundUI : MonoBehaviour
{
    public static EndRoundUI instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI scoreText;
    //[SerializeField] private Button restartButton;


    void Awake() => instance = this;

    // void Start()
    // {
    //     var runner = FindFirstObjectByType<NetworkRunner>();
    //     if (restartButton != null)
    //         restartButton.gameObject.SetActive(runner != null && runner.IsServer);
    // }

    public void ShowResult(string result, int scoreA, int scoreB)
    {
        panel.SetActive(true);
        if (resultText) resultText.text = result;
        if (scoreText) scoreText.text = $"{scoreA} - {scoreB}";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnBackToLobby() => _ = FindFirstObjectByType<NetworkRunner>()?.Shutdown();

    public void OnRestart()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null && runner.IsServer)
            runner.LoadScene(SceneRef.FromIndex(
                SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/Game.unity")));
    }
}
