using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    void Start()
    {
        startButton.onClick.AddListener(SceneLoader.LoadGame);
        backButton.onClick.AddListener(SceneLoader.LoadMainMenu);
    }
}