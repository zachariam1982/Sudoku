using System.Collections.Generic;
using UnityEngine;

public class PlayerSettings : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static PlayerSettings Instance { get; private set;}
    public readonly static string    PlayerID        = "player_id";
    public readonly static string    TotalGamePlayed = "TotalGamePlayed";
    public readonly static string    TotalPoints     = "TotalPoints";
    public readonly static string    TotalWins       = "TotalWins";
    public readonly static string    BestWinTime     = "best_win_time";
    public readonly static string    CurrentStreak   = "current_streak";
    public readonly static string    TotalPossiblePoints = "total_possible_points";    
    public Dictionary<string, object> Dict { get; private set;}
    public int Version { get; private set; }
    void Awake()
    {
        if(PlayerSettings.Instance == null)
        {
            Version = 0;
            PlayerSettings.Instance = this;
            Dict = new Dictionary<string, object>();
        }

        if (!PlayerPrefs.HasKey(PlayerID))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(PlayerID, newId);
        }
        if (!PlayerPrefs.HasKey(TotalGamePlayed))
        {
            int total_played = GameDatabase.GetTotalGamesPlayed();
            PlayerPrefs.SetInt(TotalGamePlayed, total_played);
        }
        if (!PlayerPrefs.HasKey(TotalPoints))
        {
            int total_points = GameDatabase.GetTotalPoints();
            PlayerPrefs.SetInt(TotalPoints, total_points);
        }
        if (!PlayerPrefs.HasKey(TotalWins))
        {
            int total_wins = GameDatabase.GetTotalWins();
            PlayerPrefs.SetInt(TotalWins, total_wins);
        }
        if (!PlayerPrefs.HasKey(BestWinTime))
        {
            float best_win_time = GameDatabase.GetFastestWin().ElapsedSeconds;
            PlayerPrefs.SetFloat(BestWinTime, best_win_time);
        }
        if (!PlayerPrefs.HasKey(CurrentStreak))
        {
            int current_streak = 0;
            PlayerPrefs.SetInt(CurrentStreak, current_streak);
        }
        if (!PlayerPrefs.HasKey(TotalPossiblePoints))
        {
            List<GameRecord> lst = GameDatabase.GetAll();
            int total_possible_score = 0;

            foreach(var record in lst)
            {
                total_possible_score += ScoringSystem.GetAbsoluteMaximumScore((SudokuDifficulty)record.Difficulty);
            }

            PlayerPrefs.SetInt(TotalPossiblePoints, total_possible_score);
        }
        PlayerPrefs.Save();
    }

    public void SavePlayerPref(GameRecord arg)
    {
        int total_game_played = PlayerPrefs.GetInt(TotalGamePlayed, 0);
        int total_points      = PlayerPrefs.GetInt(TotalPoints, 0);
        int total_max_points  = PlayerPrefs.GetInt(TotalPossiblePoints, 0);
        int total_wins        = PlayerPrefs.GetInt(TotalWins, 0);
        float best_elapsed_t  = PlayerPrefs.GetFloat(BestWinTime, float.MaxValue);
        int streak            = PlayerPrefs.GetInt(CurrentStreak, 0);

        streak = arg.IsWon ? streak + 1 : 0;
        total_game_played++;
        total_points += arg.Points;
        total_wins = arg.IsWon ? total_wins + 1 : total_wins;
        total_max_points = ScoringSystem.GetAbsoluteMaximumScore((SudokuDifficulty)arg.Difficulty);
        if(best_elapsed_t > arg.ElapsedSeconds) best_elapsed_t = arg.ElapsedSeconds;

        PlayerPrefs.SetInt(TotalGamePlayed, total_game_played);
        PlayerPrefs.SetInt(TotalPoints, total_points);
        PlayerPrefs.SetInt(TotalWins, total_wins);
        PlayerPrefs.SetInt(CurrentStreak, streak);
        PlayerPrefs.SetInt(TotalPossiblePoints, total_max_points);
        PlayerPrefs.SetFloat(BestWinTime, best_elapsed_t); 
    }
}
