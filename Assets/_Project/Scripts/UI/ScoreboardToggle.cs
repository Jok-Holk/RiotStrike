using UnityEngine;

public class ScoreboardToggle : MonoBehaviour
{
    [SerializeField] private GameObject scoreboardPanel;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            scoreboardPanel.SetActive(true);
        if (Input.GetKeyUp(KeyCode.Tab))
            scoreboardPanel.SetActive(false);
    }
}
