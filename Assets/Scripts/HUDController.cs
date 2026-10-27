using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Button exitButton;
    [SerializeField] private string startScene = "StartScene";

    private bool isExiting;

    private void OnEnable()
    {
        exitButton.onClick.AddListener(ExitLevel);
    }

    private void OnDisable()
    {
        exitButton.onClick.RemoveListener(ExitLevel);
    }

    private void ExitLevel()
    {
        if (isExiting)
        {
            return;
        }

        isExiting = true;
        exitButton.interactable = false;
        SceneManager.LoadScene(startScene);
    }
}
