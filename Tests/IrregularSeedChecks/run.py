"""Run with Python 3 and .NET 8 SDK (dotnet on PATH); Unity is not required."""
from pathlib import Path
import subprocess, tempfile
repo = Path(__file__).resolve().parents[2]
scripts = repo / 'Assets/Scripts'
with tempfile.TemporaryDirectory() as temp:
    temp = Path(temp)
    files = list((scripts / 'Model/Irregular').glob('*.cs'))
    files.append(scripts / 'Model/ISudokuServices.cs')
    includes = ''.join(f'<Compile Include="{p}" />' for p in files)
    (temp / 'Checks.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup>' + includes + '</ItemGroup></Project>')
    # Use the production shared type definitions, without pulling Unity/SQLite into the harness.
    classic = (scripts / 'Model/Classic/ClassicSudokuModel.cs').read_text()
    analyzer = (scripts / 'Model/Classic/ClassicSudokuDifficultyAnalyzer.cs').read_text()
    types = classic[classic.index('public enum SudokuDifficulty'):classic.index('public class ScoringSystem')]
    types += analyzer[analyzer.index('public enum SudokuTechnique'):analyzer.index('public class ClassicSudokuDifficultyAnalyzer')]
    (temp / 'SharedTypes.cs').write_text(types)
    (temp / 'Program.cs').write_text((Path(__file__).parent / 'Program.cs').read_text())
    subprocess.run(['dotnet', 'run', '--project', str(temp / 'Checks.csproj'), '-c', 'Release'], check=True)
