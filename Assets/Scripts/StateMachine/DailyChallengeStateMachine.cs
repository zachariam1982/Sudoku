using System;
using System.Collections;
using UnityEngine;

/// <summary>Independent state machine for Daily Challenge play.</summary>
public sealed class DailyChallengeStateMachine : MonoBehaviour, ISudokuStateMachine
{
    public IGameState CurrentState { get; private set; }
    public bool IsPlaying => CurrentState is DailyChallengePlayingState;
    public bool IsIdle => CurrentState is DailyChallengeIdleState;
    public IGameState Idle { get; private set; }
    public IGameState Playing { get; private set; }
    public IGameState Paused { get; private set; }
    public IGameState Validating { get; private set; }
    public IGameState Win { get; private set; }
    public IGameState Lose { get; private set; }

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
        Win = new DailyChallengeWinState(this, viewModel);
        Lose = new DailyChallengeLoseState(this, viewModel);
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

    private bool IsPlayOrPauseState(IGameState state)
    {
        return state == Playing || state == Paused;
    }

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

    private sealed class DailyChallengeIdleState : IGameState
    {
        private readonly DailyChallengeStateMachine m; private readonly DailyChallengeViewModel v;
        public DailyChallengeIdleState(DailyChallengeStateMachine machine, DailyChallengeViewModel vm) { m=machine; v=vm; }
        public void Enter() { v.FirstCellTapped.OnChanged += OnTap; }
        public void Update(float dt) { }
        public void Exit() { v.FirstCellTapped.OnChanged -= OnTap; v.FirstCellTapped.Value=false; }
        private void OnTap(bool tapped) { if(tapped) m.TransitionTo(m.Playing); }
    }

    private sealed class DailyChallengePlayingState : IGameState
    {
        private readonly DailyChallengeStateMachine m; private readonly DailyChallengeViewModel v;
        public DailyChallengePlayingState(DailyChallengeStateMachine machine, DailyChallengeViewModel vm) { m=machine; v=vm; }
        public void Enter() { v.LastEnteredCell.OnChanged += OnCell; v.IsComplete.OnChanged += OnComplete; v.PauseRequested.OnChanged += OnPause; v.LivesRemaining.OnChanged += OnLives; v.IsTimerRunning.Value=true; }
        public void Update(float dt) { v.ElapsedSeconds.Value += dt; }
        public void Exit() { v.LastEnteredCell.OnChanged -= OnCell; v.IsComplete.OnChanged -= OnComplete; v.PauseRequested.OnChanged -= OnPause; v.LivesRemaining.OnChanged -= OnLives; v.IsTimerRunning.Value=false; }
        private void OnCell((int row,int col,bool hasConflict) cell) { }
        private void OnComplete(bool complete) { if(complete) m.TransitionTo(m.Validating); }
        private void OnPause(bool pause) { if(pause) m.TransitionTo(m.Paused); }
        private void OnLives(int lives) { if(lives<=0) m.TransitionTo(m.Lose); }
    }

    private sealed class DailyChallengePausedState : IGameState
    {
        private readonly DailyChallengeStateMachine m; private readonly DailyChallengeViewModel v;
        public DailyChallengePausedState(DailyChallengeStateMachine machine, DailyChallengeViewModel vm) { m=machine; v=vm; }
        public void Enter() => v.ResumeRequested.OnChanged += OnResume;
        public void Update(float dt) { }
        public void Exit() { v.ResumeRequested.OnChanged -= OnResume; v.PauseRequested.Value=false; v.ResumeRequested.Value=false; }
        private void OnResume(bool requested) { if(requested) m.TransitionTo(m.Playing); }
    }

    private sealed class DailyChallengeValidatingState : IGameState
    {
        private readonly DailyChallengeStateMachine m; private readonly DailyChallengeViewModel v; private float elapsed;
        public DailyChallengeValidatingState(DailyChallengeStateMachine machine, DailyChallengeViewModel vm) { m=machine; v=vm; }
        public void Enter() { elapsed=0f; v.IsValidating.Value=true; }
        public void Update(float dt) { elapsed+=dt; if(elapsed<0.8f)return; v.IsValidating.Value=false; if(v.IsBoardValid.Value){v.Finish(true);m.TransitionTo(m.Win);}else m.TransitionTo(m.Playing); }
        public void Exit() => v.IsValidating.Value=false;
    }

    private sealed class DailyChallengeWinState : IGameState
    {
        private readonly DailyChallengeViewModel v;
        public DailyChallengeWinState(DailyChallengeStateMachine machine, DailyChallengeViewModel vm) { v=vm; }
        public void Enter() { v.IsWon.Value=true; }
        public void Update(float dt) { }
        public void Exit() { v.IsWon.Value=false; }
    }

    private sealed class DailyChallengeLoseState : IGameState
    {
        private readonly DailyChallengeViewModel v;
        public DailyChallengeLoseState(DailyChallengeStateMachine machine, DailyChallengeViewModel vm) { v=vm; }
        public void Enter() { v.Finish(false); v.IsLost.Value=true; }
        public void Update(float dt) { }
        public void Exit() { v.IsLost.Value=false; }
    }
}
