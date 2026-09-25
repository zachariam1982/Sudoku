public interface IScorePenalties
{
    int Mistakes { get; }
    int SOSEmptyCells { get; }
    int SOSWrongCells { get; }

    void AddMistake();
    void AddSOSEmptyCell();
    void AddSOSWrongCell();
    int TotalPenalty();
    void Reset();
    void Load(int mistakes, int sosEmptyCells, int sosWrongCells);
}
