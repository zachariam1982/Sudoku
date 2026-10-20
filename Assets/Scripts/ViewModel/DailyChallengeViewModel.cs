using System;
using UnityEngine;

/// <summary>Owns the dated puzzle and gameplay-only state for Daily Challenge.</summary>
public sealed class DailyChallengeViewModel : BaseViewModel
{
    private DailyChallengeStateMachine DailyMachine => (DailyChallengeStateMachine)StateMachine;
    private int _dateSeed;
    public DailyChallengeRecord ChallengeRecord { get; private set; }

    public BindableProperty<float> ElapsedSeconds { get; } = new BindableProperty<float>(0f);
    public BindableProperty<int> LivesRemaining { get; } = new BindableProperty<int>(3);
    public BindableProperty<bool> IsTimerRunning { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsLost { get; } = new BindableProperty<bool>(false);
    public ICommand NewGameCommand { get; }
    public ICommand RetryCommand { get; }

    public DailyChallengeViewModel(IGameUsageStats usageStats, IScorePenalties penalties)
        : base(usageStats, penalties)
    {
        NewGameCommand = new RelayCommand(_ => NotifyGameCompleted());
        RetryCommand = new RelayCommand(_ =>
            ShowMessage.Value = ("Challenge already attempted",
                "Today's challenge is already attempted.", ""));
    }

    public void Prepare(DailyChallengeRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        ChallengeRecord = record;
        _dateSeed = DailyChallengeRepository.GetPuzzleSeed(record.CurrentDate);
        SetGameLevelAndDifficulty(_dateSeed, (SudokuDifficulty)record.Difficulty);
        Model.SetPuzzleSeed(_dateSeed);
        ElapsedSeconds.Value = 0f;
        LivesRemaining.Value = 3;
        IsLost.Value = false;
        IsWon.Value = false;
        PauseRequested.Value = false;
        ResumeRequested.Value = false;
        DailyMachine.StartChallenge();
    }

    public override void ResetPuzzle()
    {
        if (_dateSeed <= 0) return;
        Model.SetPuzzleSeed(_dateSeed);
        base.ResetPuzzle();
        ElapsedSeconds.Value = 0f;
        LivesRemaining.Value = 3;
        IsLost.Value = false;
        IsWon.Value = false;
    }

    protected override void OnConflict()
    {
        LivesRemaining.Value = Mathf.Max(0, LivesRemaining.Value - 1);
    }

    public void Finish(bool won)
    {
        if (ChallengeRecord == null || ChallengeRecord.CompletedAt != null) return;
        ChallengeRecord.IsWon = won;
        ChallengeRecord.ElapsedSeconds = ElapsedSeconds.Value;
        ChallengeRecord.LivesRemaining = LivesRemaining.Value;
        ChallengeRecord.Stars = won ? 3 : 0;
        ChallengeRecord.CompletedAt = DateTime.Now.ToString("o");
        ChallengeRecord.UndoUses = UsageStats.UndoUses;
        ChallengeRecord.PencilUses = UsageStats.PencilUses;
        ChallengeRecord.EraseUses = UsageStats.EraseUses;
        ChallengeRecord.SOSUses = UsageStats.SOSUses;
        ChallengeRecord.AutofillUses = UsageStats.AutoFillUses;
        DailyChallengeRepository.Update(ChallengeRecord);
    }
}
