/// <summary>
/// Pure data model for a Sudoku puzzle.
/// No Unity dependencies.
/// </summary>
public class SudokuModel
{
    public int[,]  Board     { get; private set; } = new int[9, 9];
    public bool[,] GivenMask { get; private set; } = new bool[9, 9];


    private readonly int[,] startingBoard = new int[9, 9]
    {
        { 5, 3, 0,  0, 7, 0,  0, 0, 0 },
        { 6, 0, 0,  1, 9, 5,  0, 0, 0 },
        { 0, 9, 8,  0, 0, 0,  0, 6, 0 },
        { 8, 0, 0,  0, 6, 0,  0, 0, 3 },
        { 4, 0, 0,  8, 0, 3,  0, 0, 1 },
        { 7, 0, 0,  0, 2, 0,  0, 0, 6 },
        { 0, 6, 0,  0, 0, 0,  2, 8, 0 },
        { 0, 0, 0,  4, 1, 9,  0, 0, 5 },
        { 0, 0, 0,  0, 8, 0,  0, 7, 9 }
    };

    public void LoadStartingPuzzle()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int value       = startingBoard[row, col];
                Board[row, col]     = value;
                GivenMask[row, col] = value != 0;
            }
    }

    public bool SetValue(int row, int col, int value)
    {
        if (GivenMask[row, col]) return false;
        Board[row, col] = value;
        return true;
    }

    public bool IsGiven(int row, int col) => GivenMask[row, col];
    public int  GetValue(int row, int col) => Board[row, col];

    /// <summary>
    /// Returns true if the value at (row, col) appears more than once
    /// in its row, column, or 3x3 box.
    /// Called immediately after SetValue to decide which animation to play.
    /// </summary>
    public bool HasConflict(int row, int col)
    {
        int value = Board[row, col];
        if (value == 0) return false;

        // Check row
        for (int c = 0; c < 9; c++)
            if (c != col && Board[row, c] == value) return true;

        // Check column
        for (int r = 0; r < 9; r++)
            if (r != row && Board[r, col] == value) return true;

        // Check 3x3 box
        int boxRow = (row / 3) * 3;
        int boxCol = (col / 3) * 3;
        for (int r = boxRow; r < boxRow + 3; r++)
            for (int c = boxCol; c < boxCol + 3; c++)
                if ((r != row || c != col) && Board[r, c] == value) return true;

        return false;
    }

    public bool Validate()
    {
        for (int i = 0; i < 9; i++)
        {
            bool[] rowSeen = new bool[10];
            bool[] colSeen = new bool[10];
            for (int j = 0; j < 9; j++)
            {
                int rv = Board[i, j]; int cv = Board[j, i];
                if (rv != 0) { if (rowSeen[rv]) return false; rowSeen[rv] = true; }
                if (cv != 0) { if (colSeen[cv]) return false; colSeen[cv] = true; }
            }
        }
        for (int br = 0; br < 3; br++)
            for (int bc = 0; bc < 3; bc++)
            {
                bool[] seen = new bool[10];
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                    {
                        int v = Board[br * 3 + r, bc * 3 + c];
                        if (v != 0) { if (seen[v]) return false; seen[v] = true; }
                    }
            }
        return true;
    }

    public bool IsComplete()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (Board[row, col] == 0) return false;
        return true;
    }
}