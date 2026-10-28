using UnityEngine;

public static class HighScoreRecord
{
    private static string ScoreKey(string sceneName) => $"A4.{sceneName}.HighScore";
    private static string TimeKey(string sceneName) => $"A4.{sceneName}.BestTimeHundredths";

    public static int GetScore(string sceneName) => PlayerPrefs.GetInt(ScoreKey(sceneName), 0);
    public static int GetTimeHundredths(string sceneName) => PlayerPrefs.GetInt(TimeKey(sceneName), 0);

    public static bool SaveIfBetter(string sceneName, int score, double seconds)
    {
        // Store the same hundredths shown on the HUD, without float rounding on reload.
        int hundredths = (int)(System.Math.Max(0d, seconds) * 100d);
        bool hasRecord = PlayerPrefs.HasKey(ScoreKey(sceneName)) && PlayerPrefs.HasKey(TimeKey(sceneName));
        int previousScore = GetScore(sceneName);
        if (hasRecord && (score < previousScore ||
            (score == previousScore && hundredths >= GetTimeHundredths(sceneName)))) return false;

        PlayerPrefs.SetInt(ScoreKey(sceneName), score);
        PlayerPrefs.SetInt(TimeKey(sceneName), hundredths);
        PlayerPrefs.Save();
        return true;
    }
}
