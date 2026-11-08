using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

class Program
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static string Map(IrregularSudokuRules rules) => string.Join(",", Enumerable.Range(0,81).Select(i => rules.GetRegion(i/9,i%9)));
    static int[] Flat(int[,] board) => Enumerable.Range(0,81).Select(i => board[i/9,i%9]).ToArray();
    static void Main()
    {
        var watch = Stopwatch.StartNew();
        var shapes = new HashSet<string>();
        foreach (int seed in Enumerable.Range(0,200).Concat(new[] { int.MinValue, int.MaxValue, -1 }))
        {
            var rules = new IrregularSudokuRules(seed);
            string before = Map(rules);
            Check(before == Map(new IrregularSudokuRules(seed)), "Seed reproducibility");
            shapes.Add(before);
            for (int r=0;r<9;r++)
            {
                var cells = Enumerable.Range(0,81).Where(i => rules.GetRegion(i/9,i%9)==r).ToHashSet();
                Check(cells.Count==9,"Region size");
                var reached = new HashSet<int> { cells.First() };
                var queue = new Queue<int>(reached);
                while (queue.Count>0)
                {
                    int i=queue.Dequeue();
                    foreach (int n in new[] { i-9,i+9,i-1,i+1 })
                        if (cells.Contains(n) && Math.Abs(i/9-n/9)+Math.Abs(i%9-n%9)==1 && reached.Add(n)) queue.Enqueue(n);
                }
                Check(reached.Count==9,"Region connectivity");
            }
            var solver = new IrregularSudokuSolver(rules);
            var solution = rules.CreateSolution(new Random(seed));
            Check(rules.ValidateCompleteSolution(solution),"Complete solution validation");
            var puzzle=(int[,])solution.Clone(); puzzle[0,0]=0;
            Check(solver.HasUniqueSolution(puzzle),"Uniqueness");
            Check(new IrregularSudokuDifficultyAnalyzer(rules).Analyze(puzzle).SolvedLogically,"Native analyzer");
            Check(IrregularSudokuModel.IsValidSavedPuzzle(Flat(puzzle),Flat(solution),seed,1),"Seed-aware save validation");
            Check(!IrregularSudokuModel.IsValidSavedPuzzle(Flat(puzzle),Flat(solution),seed,99),"Reject unknown version");
            var model=new IrregularSudokuModel(); model.SetPuzzleSeed(seed);
            Check(model.TryLoadSavedPuzzle(Flat(puzzle),Flat(solution),1),"Resume");
            model.AutoFillPencilCandidates();
            Check(model.PencilCandidateMasks[0,0]==(1<<solution[0,0]),"Restored candidates");
            Check(Map(rules)==before,"Other instances must not mutate layout");
        }
        Console.WriteLine($"Distinct layouts: {shapes.Count}");
        Check(shapes.Count>190,"Layout diversity");
        var legacy=new IrregularSudokuRules(123,0);
        Check(Map(legacy)==Map(new IrregularSudokuRules(999,0)),"Legacy layout");
        var oldSolution=new int[9,9];
        Check(new IrregularSudokuSolver(legacy).TryFillSolution(oldSolution,new Random(1),100000),"Legacy solution");
        var oldModel=new IrregularSudokuModel();oldModel.SetPuzzleSeed(123);
        Check(oldModel.TryLoadSavedPuzzle(Flat(oldSolution),Flat(oldSolution),0),"Legacy resume");
        Check(oldModel.LayoutVersion==0,"Preserve legacy version");
        Check(!IrregularSudokuModel.IsValidSavedPuzzle(Flat(oldSolution),Flat(oldSolution),123,1),"Reject wrong layout");
        var generator=new IrregularSudokuGenerator();
        var first=generator.Generate(42,SudokuDifficulty.Simple,100);
        var second=generator.Generate(42,SudokuDifficulty.Simple,100);
        Check(Flat(first.Puzzle).SequenceEqual(Flat(second.Puzzle)),"Puzzle reproducibility");
        Check(new IrregularSudokuSolver(42).HasUniqueSolution(first.Puzzle),"Generated puzzle uniqueness");
        Check(new IrregularSudokuRules(42).ValidateCompleteSolution(first.Solution),"Generator uses seeded layout");
        Console.WriteLine($"PASS: 203 seeds, {shapes.Count} distinct maps; connectivity, solvability, isolation, analyzer, saves, legacy resume, deterministic generation ({watch.ElapsedMilliseconds} ms).");
    }
}
// External services are deliberately excluded: these checks exercise production Irregular code.
public class Thermometer { }
public class KillerCage { }
public class GameRecord { }
public interface IScorePenalties { }
public interface IGameHistoryRepository { List<GameRecord> GetLastNRecordByDate(int n); }
public class SqliteIrregularGameHistoryRepository : IGameHistoryRepository { public List<GameRecord> GetLastNRecordByDate(int n) => new List<GameRecord>(); }
public static class ClassicSudokuModel { public static SudokuDifficulty CalculateNextDifficulty(List<GameRecord> r,SudokuDifficulty d) => d; }
namespace UnityEngine { public static class Debug { public static void Log(object o) { } public static void LogWarning(object o) { } } }
