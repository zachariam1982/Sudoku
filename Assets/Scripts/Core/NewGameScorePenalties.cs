public sealed class NewGameScorePenalties : IScorePenalties
{
    public int Mistakes => 0;
    public int SOSEmptyCells => 0;
    public int SOSWrongCells => 0;

    public void AddMistake() { }
    public void AddSOSEmptyCell() { }
    public void AddSOSWrongCell() { }
    public int TotalPenalty() => 0;
    public void Reset() { }
    public void Load(int mistakes, int sosEmptyCells, int sosWrongCells) { }
}
