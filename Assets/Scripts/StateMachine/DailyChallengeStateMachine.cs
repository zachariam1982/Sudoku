using System;
using System.Collections;
using UnityEngine;

/// <summary>Independent state machine for Daily Challenge play.</summary>
public sealed class DailyChallengeStateMachine : MonoBehaviour, ISudokuStateMachine
{
    public DailyChallengeIdleState Idle { get; private set; }
    public DailyChallengePlayingState Playing { get; private set; }
    public DailyChallengePausedState Paused { get; private set; }
    public DailyChallengeValidatingState Validating { get; private set; }
    public DailyChallengeWinState Win { get; private set; }
    public DailyChallengeLoseState Lose { get; private set; }

    public IGameState CurrentState { get; private set; }
    public bool IsPlaying => CurrentState is DailyChallengePlayingState;
    public bool IsIdle => CurrentState is DailyChallengeIdleState;

    private DailyChallengeViewModel _viewModel;
    private Coroutine _dayBoundaryTimer;
    private bool _suspended = true;

    public void Initialise(DailyChallengeViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.AttachStateMachine(this);
        Idle = new DailyChallengeIdleState(this, viewModel);
        Playing = new DailyChallengePlayingState(this, viewModel);
        Paused = new DailyChallengePausedState(this, viewModel);
        Validating = new DailyChallengeValidatingState(this, viewModel);
        Win = new DailyChallengeWinState(viewModel);
        Lose = new DailyChallengeLoseState(viewModel);
        TransitionTo(Idle);
    }

    public void StartPlaying() => TransitionTo(Playing);
    public void StartChallenge() => TransitionTo(Idle);
    public void SetSuspended(bool suspended) => _suspended = suspended;

    public void TransitionTo(IGameState next)
    {
        if (IsPlayOrPauseState(next) && IsChallengePastItsDate())
            next = Lose;

        StopDayBoundaryTimer();
        CurrentState?.Exit();
        CurrentState = next;
        CurrentState.Enter();
        _viewModel.CurrentStateName.Value = next.GetType().Name;

        if (IsPlayOrPauseState(CurrentState))
            ScheduleDayBoundaryTimer();
    }

    private void Update()
    {
        if (!_suspended) CurrentState?.Update(Time.deltaTime);
    }

    private bool IsPlayOrPauseState(IGameState state) => state == Playing || state == Paused;

    private bool IsChallengePastItsDate()
    {
        return _viewModel != null &&
               _viewModel.HasActiveChallenge &&
               _viewModel.ChallengeRecord != null &&
               DateTime.Today > _viewModel.ChallengeRecord.CurrentDate.Date;
    }

    private void ScheduleDayBoundaryTimer()
    {
        if (!IsPlayOrPauseState(CurrentState) ||
            _viewModel == null ||
            !_viewModel.HasActiveChallenge)
            return;

        if (IsChallengePastItsDate())
        {
            TransitionTo(Lose);
            return;
        }

        DateTime nextLocalDay = DateTime.Today.AddDays(1);
        float secondsUntilBoundary = Mathf.Max(
            0.01f,
            (float)(nextLocalDay - DateTime.Now).TotalSeconds);
        _dayBoundaryTimer = StartCoroutine(WaitForDayBoundary(secondsUntilBoundary));
    }

    private IEnumerator WaitForDayBoundary(float secondsUntilBoundary)
    {
        yield return new WaitForSecondsRealtime(secondsUntilBoundary);
        _dayBoundaryTimer = null;

        if (!IsPlayOrPauseState(CurrentState) ||
            _viewModel == null ||
            !_viewModel.HasActiveChallenge)
            yield break;

        if (IsChallengePastItsDate())
            TransitionTo(Lose);
        else
            ScheduleDayBoundaryTimer();
    }

    private void StopDayBoundaryTimer()
    {
        if (_dayBoundaryTimer == null) return;
        StopCoroutine(_dayBoundaryTimer);
        _dayBoundaryTimer = null;
    }
}
