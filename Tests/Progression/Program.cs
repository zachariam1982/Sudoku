int checks=0;
void Equal<T>(T expected,T actual,string name) {
 checks++;
 if (!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}
List<GameRecord> Window(SudokuDifficulty tier,int wins,int total) {
 return Enumerable.Range(0,5).Select(i => new GameRecord { Id=100-i,Level=20-i,Difficulty=(int)tier,IsWon=i<wins,Points=total/5+(i<total%5?1:0) }).ToList();
}
SudokuDifficulty Next(List<GameRecord> r,SudokuDifficulty fallback=SudokuDifficulty.Moderate) => ClassicSudokuModel.CalculateNextDifficulty(r,fallback);
// Exhaust all tiers, win counts, and attainable score totals against the rules.
foreach (SudokuDifficulty tier in Enum.GetValues<SudokuDifficulty>()) {
 int possible=ScoringSystem.GetAbsoluteMaximumScore(tier)*5;
 for(int wins=0;wins<=5;wins++) for(int total=0;total<=possible;total++) {
  int expected=(int)tier;
  if(wins>=4 && total*5>=possible*4) expected=Math.Min(8,expected+1);
  else if(wins<=2 || total*20<possible*9) expected=Math.Max(0,expected-1);
  var history=Window(tier,wins,total);
  var actual=Next(history);
  Equal((SudokuDifficulty)expected,actual,"threshold/tier boundary");
  Equal(actual,Next(history,actual),"idempotence");
 }
}
Equal(SudokuDifficulty.Moderate,Next(Window(SudokuDifficulty.Advanced,2,197)),"Advanced replay demotion");
Equal(SudokuDifficulty.Moderate,Next(null),"null history");
Equal(SudokuDifficulty.Moderate,Next(new()),"empty history");
for(int count=1;count<5;count++) Equal(SudokuDifficulty.Advanced,Next(Window(SudokuDifficulty.Advanced,0,0).Take(count).ToList()),"short history restores tier");
var mixed=Window(SudokuDifficulty.Advanced,0,0); mixed[4].Difficulty=4;
Equal(SudokuDifficulty.Advanced,Next(mixed),"mixed tiers");
var extra=Window(SudokuDifficulty.Advanced,0,0); extra.Add(new GameRecord {Difficulty=0});
Equal(SudokuDifficulty.Moderate,Next(extra),"only latest five");
var invalid=Window(SudokuDifficulty.Advanced,0,0); invalid[0].Difficulty=99;
Equal(SudokuDifficulty.Moderate,Next(invalid),"invalid latest tier");
// Solver regression checks: one solution, multiple solutions, invalid givens,
// solution-count limits, and preservation of the caller's input board.
int[,] knownUniquePuzzle = {
 {5,3,0,0,7,0,0,0,0},
 {6,0,0,1,9,5,0,0,0},
 {0,9,8,0,0,0,0,6,0},
 {8,0,0,0,6,0,0,0,3},
 {4,0,0,8,0,3,0,0,1},
 {7,0,0,0,2,0,0,0,6},
 {0,6,0,0,0,0,2,8,0},
 {0,0,0,4,1,9,0,0,5},
 {0,0,0,0,8,0,0,7,9}
};
var knownPuzzleBeforeSolve=(int[,])knownUniquePuzzle.Clone();
Equal(1,ClassicSudokuSolver.CountSolutions(knownUniquePuzzle),"known puzzle has one solution");
Equal(true,ClassicSudokuSolver.HasUniqueSolution(knownUniquePuzzle),"known puzzle uniqueness");
for(int r=0;r<9;r++) for(int c=0;c<9;c++)
 Equal(knownPuzzleBeforeSolve[r,c],knownUniquePuzzle[r,c],"solver preserves input board");
var emptyPuzzle=new int[9,9];
Equal(2,ClassicSudokuSolver.CountSolutions(emptyPuzzle,2),"empty board count capped at two");
Equal(1,ClassicSudokuSolver.CountSolutions(emptyPuzzle,1),"solution count respects limit one");
Equal(0,ClassicSudokuSolver.CountSolutions(emptyPuzzle,0),"zero solution limit");
Equal(false,ClassicSudokuSolver.HasUniqueSolution(emptyPuzzle),"empty board is not unique");
var duplicateRow=new int[9,9];
duplicateRow[0,0]=4; duplicateRow[0,1]=4;
Equal(0,ClassicSudokuSolver.CountSolutions(duplicateRow),"duplicate row givens are contradictory");
var duplicateColumn=new int[9,9];
duplicateColumn[0,0]=4; duplicateColumn[1,0]=4;
Equal(0,ClassicSudokuSolver.CountSolutions(duplicateColumn),"duplicate column givens are contradictory");
var duplicateBox=new int[9,9];
duplicateBox[0,0]=4; duplicateBox[1,1]=4;
Equal(0,ClassicSudokuSolver.CountSolutions(duplicateBox),"duplicate box givens are contradictory");
// Candidate removals must be written back to the analyzer's candidate grid.
var analyzerState=new ClassicSudokuDifficultyAnalyzer.SolverState(new int[9,9]);
Equal(9,analyzerState.Candidates[0,0].Count,"empty analyzer cell has all candidates");
Equal(true,analyzerState.RemoveCandidate(0,0,5),"first candidate removal changes state");
Equal(false,analyzerState.Candidates[0,0].Contains(5),"candidate removal persists in state");
Equal(8,analyzerState.Candidates[0,0].Count,"candidate removal updates candidate count");
Equal(false,analyzerState.RemoveCandidate(0,0,5),"repeated candidate removal reports no change");
// Analyzer representation regression checks on completed and single-hole grids.
int[,] solvedReference = {
 {5,3,4,6,7,8,9,1,2},
 {6,7,2,1,9,5,3,4,8},
 {1,9,8,3,4,2,5,6,7},
 {8,5,9,7,6,1,4,2,3},
 {4,2,6,8,5,3,7,9,1},
 {7,1,3,9,2,4,8,5,6},
 {9,6,1,5,3,7,2,8,4},
 {2,8,7,4,1,9,6,3,5},
 {3,4,5,2,8,6,1,7,9}
};
var solvedRating=ClassicSudokuDifficultyAnalyzer.Analyze(solvedReference);
Equal(SudokuDifficulty.Simple,solvedRating.Difficulty,"solved board analyzer tier");
Equal(SudokuTechnique.NakedSingle,solvedRating.HardestTechnique,"solved board analyzer technique");
Equal(0,solvedRating.SolveSteps,"solved board analyzer steps");
Equal(true,solvedRating.SolvedLogically,"solved board analyzer completion");
var oneHole=(int[,])solvedReference.Clone();
oneHole[0,0]=0;
var oneHoleBefore=(int[,])oneHole.Clone();
var oneHoleRating=ClassicSudokuDifficultyAnalyzer.Analyze(oneHole);
Equal(SudokuDifficulty.Simple,oneHoleRating.Difficulty,"single-hole analyzer tier");
Equal(SudokuTechnique.NakedSingle,oneHoleRating.HardestTechnique,"single-hole analyzer technique");
Equal(1,oneHoleRating.SolveSteps,"single-hole analyzer steps");
Equal(true,oneHoleRating.SolvedLogically,"single-hole analyzer completion");
for(int r=0;r<9;r++) for(int c=0;c<9;c++)
 Equal(oneHoleBefore[r,c],oneHole[r,c],"analyzer preserves input board");
// Exercise the production generator and difficulty analyzer for every advertised tier.
bool IsSolvedBoard(int[,] board) {
 for (int row=0;row<9;row++) {
  var rowValues=new HashSet<int>(); var colValues=new HashSet<int>();
  for (int col=0;col<9;col++) {
   if(board[row,col]<1 || board[row,col]>9 || board[col,row]<1 || board[col,row]>9) return false;
   rowValues.Add(board[row,col]); colValues.Add(board[col,row]);
  }
  if(rowValues.Count!=9 || colValues.Count!=9 || rowValues.Contains(0) || colValues.Contains(0)) return false;
 }
 for(int boxRow=0;boxRow<3;boxRow++) for(int boxCol=0;boxCol<3;boxCol++) {
  var values=new HashSet<int>();
  for(int row=0;row<3;row++) for(int col=0;col<3;col++)
   values.Add(board[boxRow*3+row,boxCol*3+col]);
  if(values.Count!=9 || values.Contains(0)) return false;
 }
 return true;
}
foreach(SudokuDifficulty tier in Enum.GetValues<SudokuDifficulty>()) {
 var generated=ClassicSudokuGenerator.GenerateSudoku(1,tier);
 Equal(true,(int)generated.Difficulty<=(int)tier,$"generated difficulty does not exceed requested {tier}");
 Equal(generated.Difficulty,ClassicSudokuDifficultyAnalyzer.Analyze(generated.Puzzle).Difficulty,$"generated rating matches recorded tier {tier}");
 Equal(true,ClassicSudokuSolver.HasUniqueSolution(generated.Puzzle),$"unique solution {tier}");
 Equal(true,IsSolvedBoard(generated.Solution),$"valid complete solution {tier}");
 for(int row=0;row<9;row++) for(int col=0;col<9;col++)
  if(generated.Puzzle[row,col]!=0)
   Equal(generated.Solution[row,col],generated.Puzzle[row,col],$"given agrees with solution {tier}");
}
// Requested tiers are tried in descending order and stop at the first available tier.
var fallbackOrder=new List<SudokuDifficulty>();
var fallbackResult=ClassicSudokuGenerator.GenerateWithDifficultyFallback(
 SudokuDifficulty.Expert,
 tier => {
  fallbackOrder.Add(tier);
  return tier==SudokuDifficulty.Moderate ? new SudokuResult() : null;
 });
Equal("Expert,Hard,Advanced,Moderate",string.Join(",",fallbackOrder),"fallback tier order");
Equal(SudokuDifficulty.Moderate,fallbackResult.Difficulty,"fallback records actual tier");
bool rejectedInvalidDifficulty=false;
try { ClassicSudokuGenerator.GenerateSudoku(1,(SudokuDifficulty)99); }
catch(ArgumentOutOfRangeException) { rejectedInvalidDifficulty=true; }
Equal(true,rejectedInvalidDifficulty,"reject invalid difficulty enum");

// Diagonal Sudoku rules and persistence must include both main diagonals.
int[,] diagonalSolution = {
 {9,4,7,6,3,5,1,2,8},
 {8,5,2,1,4,7,3,6,9},
 {6,1,3,9,8,2,5,4,7},
 {7,8,9,4,5,3,6,1,2},
 {3,6,4,8,2,1,7,9,5},
 {1,2,5,7,9,6,4,8,3},
 {2,7,1,5,6,9,8,3,4},
 {5,9,8,3,1,4,2,7,6},
 {4,3,6,2,7,8,9,5,1}
};
Equal(true,DiagonalSudokuRules.ValidateCompleteSolution(diagonalSolution),"valid diagonal solution");
int[,] diagonalSingleHole=(int[,])diagonalSolution.Clone();
diagonalSingleHole[0,0]=0;
var diagonalSolver=new DiagonalSudokuSolver();
Equal(1,diagonalSolver.CountSolutions(diagonalSingleHole),"diagonal single-hole puzzle is unique");
int[,] mainDiagonalConflict=new int[9,9];
mainDiagonalConflict[4,4]=3;
Equal(false,diagonalSolver.IsValid(mainDiagonalConflict,0,0,3),"main diagonal rejects duplicate");
int[,] antiDiagonalConflict=new int[9,9];
antiDiagonalConflict[4,4]=4;
Equal(false,diagonalSolver.IsValid(antiDiagonalConflict,0,8,4),"anti-diagonal rejects duplicate");
int[,] centerDiagonalPeers=new int[9,9];
centerDiagonalPeers[0,0]=1;
centerDiagonalPeers[0,8]=2;
var centerCandidates=diagonalSolver.GetCandidates(centerDiagonalPeers,4,4);
Equal(false,centerCandidates.Contains(1),"center observes main diagonal");
Equal(false,centerCandidates.Contains(2),"center observes anti-diagonal");
Equal(true,DiagonalSudokuModel.IsValidSavedPuzzle(new int[81],FlattenBoard(diagonalSolution)),"diagonal saved solution accepted");
int[] nonDiagonalSolution=FlattenBoard(diagonalSolution);
nonDiagonalSolution[0]=nonDiagonalSolution[10];
Equal(false,DiagonalSudokuModel.IsValidSavedPuzzle(new int[81],nonDiagonalSolution),"saved solution with diagonal duplicate rejected");
Console.WriteLine($"PASS: {checks} assertions (production model, generator, and difficulty analyzer).");

int[] FlattenBoard(int[,] board) {
 var flat=new int[81];
 for(int row=0;row<9;row++) for(int col=0;col<9;col++) flat[row*9+col]=board[row,col];
 return flat;
}
