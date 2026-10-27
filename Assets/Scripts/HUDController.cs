using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Button exitButton;
    [SerializeField] private string startScene = "StartScene";
    [SerializeField] private Text gameTimer;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text ghostTimer;
    [SerializeField] private GameObject countdownOverlay;
    [SerializeField] private Text countdownText;

    private bool isExiting;
    private long displayedHundredths = -1;
    private int displayedGhostSeconds = -1;

    public void SetGhostTime(double seconds)
    {
        int remaining = (int)System.Math.Ceiling(System.Math.Max(0d, seconds));
        if (remaining == displayedGhostSeconds) return;
        displayedGhostSeconds = remaining;
        ghostTimer.gameObject.SetActive(remaining > 0);
        ghostTimer.text = remaining.ToString();
    }

    public void SetScore(int score)
    {
        scoreText.text = $"Score: {score:000000}";
    }

    public void ShowCountdown(string text)
    {
        countdownText.text = text;
        countdownOverlay.SetActive(true);
        exitButton.interactable = false;
    }

    public void HideCountdown()
    {
        countdownOverlay.SetActive(false);
        exitButton.interactable = !isExiting;
    }

    public void SetGameTime(double seconds)
    {
        long hundredths = (long)(System.Math.Max(0d, seconds) * 100d);
        if (hundredths == displayedHundredths) return;
        displayedHundredths = hundredths;
        gameTimer.text = $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}:{hundredths % 100:00}";
    }

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
