using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private Button levelOneButton;
    [SerializeField] private Button levelTwoButton;
    [SerializeField] private string levelOneScene = "Level01";
    [SerializeField] private string levelTwoScene = "InnovationScene";

    private bool isLoading;

    private void OnEnable()
    {
        levelOneButton.onClick.AddListener(OpenLevelOne);
        levelTwoButton.onClick.AddListener(OpenLevelTwo);
    }

    private void OnDisable()
    {
        levelOneButton.onClick.RemoveListener(OpenLevelOne);
        levelTwoButton.onClick.RemoveListener(OpenLevelTwo);
    }

    private void OpenLevelOne()
    {
        OpenLevel(levelOneScene);
    }

    private void OpenLevelTwo()
    {
        // The innovation scene is optional until the final assessment band.
        if (!Application.CanStreamedLevelBeLoaded(levelTwoScene))
        {
            return;
        }

        OpenLevel(levelTwoScene);
    }

    private void OpenLevel(string sceneName)
    {
        if (isLoading)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return;
        }

        isLoading = true;
        levelOneButton.interactable = false;
        levelTwoButton.interactable = false;
        SceneManager.LoadScene(sceneName);
    }
}
