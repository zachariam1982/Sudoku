using System;

/// <summary>A connected Killer Sudoku cage with a target sum and flat board cell indexes.</summary>
[Serializable]
public sealed class KillerCage
{
    public int TargetSum;
    public int[] Cells = Array.Empty<int>();

    public KillerCage() { }

    public KillerCage(int targetSum, int[] cells)
    {
        TargetSum = targetSum;
        Cells = cells == null ? Array.Empty<int>() : (int[])cells.Clone();
    }
}
