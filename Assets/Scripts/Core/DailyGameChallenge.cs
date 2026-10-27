using System;
using SQLite;

[Table("DailyGame_challenge")]
public class DailyGameChallenge
{
    public DailyGameChallenge()
    {
        CurrentDate = DateTime.UtcNow;
        IsAttempted = false;
        Started = false;
    }

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public DateTime CurrentDate { get; set; }

    [NotNull]
    public int Difficulty { get; set; }

    [NotNull]
    public float ElapsedSeconds { get; set; }

    [NotNull]
    public int LivesRemaining { get; set; }

    [NotNull]
    public int Stars { get; set; }

    [NotNull]
    public bool IsWon { get; set; }
    [NotNull]
    public bool IsAttempted { get; set; }
    [NotNull]
    public bool Started { get; set; }

    public string CompletedAt { get; set; }

    [NotNull]
    public int UndoUses { get; set; }

    [NotNull]
    public int PencilUses { get; set; }

    [NotNull]
    public int EraseUses { get; set; }

    [NotNull]
    public int SOSUses { get; set; }

    [NotNull]
    public int AutofillUses { get; set; }
}
