using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private Button levelOneButton;
    [SerializeField] private Button levelTwoButton;
    [SerializeField] private string levelOneScene = "Level01";
    [SerializeField] private string levelTwoScene = "InnovationScene";
    [SerializeField] private Text levelOneScore;
    [SerializeField] private Text levelOneTime;
    [SerializeField] private Text levelTwoScore;
    [SerializeField] private Text levelTwoTime;

    private bool isLoading;

    private void OnEnable()
    {
        ShowRecord(levelOneScene, levelOneScore, levelOneTime);
        ShowRecord(levelTwoScene, levelTwoScore, levelTwoTime);
        levelOneButton.onClick.AddListener(OpenLevelOne);
        levelTwoButton.onClick.AddListener(OpenLevelTwo);
    }

    private static void ShowRecord(string sceneName, Text score, Text time)
    {
        score.text = HighScoreRecord.GetScore(sceneName).ToString("000000");
        int hundredths = HighScoreRecord.GetTimeHundredths(sceneName);
        time.text = $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}:{hundredths % 100:00}";
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
