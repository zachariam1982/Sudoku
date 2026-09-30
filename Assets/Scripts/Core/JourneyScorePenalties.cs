public sealed class JourneyScorePenalties : IScorePenalties
{
    public int Mistakes { get; private set; }
    public int SOSEmptyCells { get; private set; }
    public int SOSWrongCells { get; private set; }

    public void AddMistake() => Mistakes++;
    public void AddSOSEmptyCell() => SOSEmptyCells++;
    public void AddSOSWrongCell() => SOSWrongCells++;

    public int TotalPenalty() =>
        Mistakes * ScoringSystem.PenaltyPerMistake +
        SOSEmptyCells * ScoringSystem.PenaltySOSFillsEmpty +
        SOSWrongCells * ScoringSystem.PenaltySOSFixesWrong;

    public void Reset()
    {
        Mistakes = 0;
        SOSEmptyCells = 0;
        SOSWrongCells = 0;
    }

    public void Load(int mistakes, int sosEmptyCells, int sosWrongCells)
    {
        Mistakes = mistakes;
        SOSEmptyCells = sosEmptyCells;
        SOSWrongCells = sosWrongCells;
    }
}
