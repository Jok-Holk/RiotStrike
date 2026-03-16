using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    void Start()
    {
        playButton.onClick.AddListener(SceneLoader.LoadLobby);
        quitButton.onClick.AddListener(Application.Quit);
    }
}