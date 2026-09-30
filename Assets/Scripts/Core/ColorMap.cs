using System.Collections.Generic;

public class ColorMap
{
    private static Dictionary<SudokuDifficulty, (string bg, string outline, string txtClr)> _colormap_1 = new Dictionary<SudokuDifficulty, (string bg, string outline, string textClr)>()
    {
        { SudokuDifficulty.Simple,    ("#1D2438", "#2E6FA380", "#4682FF") },
        { SudokuDifficulty.Beginner,  ("#1D283F", "#3580B880", "#4682FF") },      
        { SudokuDifficulty.Easy,      ("#1D2B47", "#3E92CC80", "#4682FF") },
        { SudokuDifficulty.Novice,    ("#1D3040", "#4FB0A580", "#4682FF") },
        { SudokuDifficulty.Moderate,  ("#1D3630", "#5FBF6B80", "#4682FF") },
        { SudokuDifficulty.Advanced,  ("#2B2E20", "#C9C24C80", "#4682FF") },
        { SudokuDifficulty.Hard,      ("#332615", "#E39A3D80", "#4682FF") },
        { SudokuDifficulty.Expert,    ("#3B221D", "#FF624680", "#4682FF") },
        { SudokuDifficulty.Hardest,   ("#3A1A20", "#E23B5C80", "#4682FF") },
    };
    private static Dictionary<SudokuDifficulty, (string bg, string outline, string txtClr)> _colormap_2 = new Dictionary<SudokuDifficulty, (string bg, string outline, string textClr)>()
    {
        { SudokuDifficulty.Simple,    ("#1D2B47", "#4A90E280", "#4A90E280") },
        { SudokuDifficulty.Beginner,  ("#1D2B47", "#3AADB380", "#3AADB380") },      
        { SudokuDifficulty.Easy,      ("#1D2B47", "#44BB8680", "#44BB8680") },
        { SudokuDifficulty.Novice,    ("#1D2B47", "#52C41A80", "#52C41A80") },
        { SudokuDifficulty.Moderate,  ("#1D2B47", "#D4B10680", "#D4B10680") },
        { SudokuDifficulty.Advanced,  ("#1D2B47", "#E67E2280", "#E67E2280") },
        { SudokuDifficulty.Hard,      ("#1D2B47", "#E74C3C80", "#E74C3C80") },
        { SudokuDifficulty.Expert,    ("#1D2B47", "#C0392B80", "#C0392B80") },
        { SudokuDifficulty.Hardest,   ("#1D2B47", "#9B59B680", "#9B59B680") },
    };
    private static Dictionary<SudokuDifficulty, (string bg, string outline, string txtClr)> _colormap_3 = new Dictionary<SudokuDifficulty, (string bg, string outline, string textClr)>()
    {
        { SudokuDifficulty.Simple,    ("#1D2438", "#2E6FA380", "#4A90E280") },
        { SudokuDifficulty.Beginner,  ("#1D283F", "#3580B880", "#3AADB380") },      
        { SudokuDifficulty.Easy,      ("#1D2B47", "#3E92CC80", "#44BB8680") },
        { SudokuDifficulty.Novice,    ("#1D3040", "#4FB0A580", "#52C41A80") },
        { SudokuDifficulty.Moderate,  ("#1D3630", "#5FBF6B80", "#D4B10680") },
        { SudokuDifficulty.Advanced,  ("#2B2E20", "#C9C24C80", "#E67E2280") },
        { SudokuDifficulty.Hard,      ("#332615", "#E39A3D80", "#E74C3C80") },
        { SudokuDifficulty.Expert,    ("#3B221D", "#FF624680", "#C0392B80") },
        { SudokuDifficulty.Hardest,   ("#3A1A20", "#E23B5C80", "#9B59B680") },
    };
    public static Dictionary<SudokuDifficulty, (string bg, string outline, string txtClr)> Color_Map = _colormap_3;
}
