int checks=0;
void Equal<T>(T expected,T actual,string name) {
 checks++;
 if (!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}
List<GameRecord> Window(SudokuDifficulty tier,int wins,int total) {
 return Enumerable.Range(0,5).Select(i => new GameRecord { Id=100-i,Level=20-i,Difficulty=(int)tier,IsWon=i<wins,Points=total/5+(i<total%5?1:0) }).ToList();
}
SudokuDifficulty Next(List<GameRecord> r,SudokuDifficulty fallback=SudokuDifficulty.Moderate) => SudokuModel.CalculateNextDifficulty(r,fallback);
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
foreach(bool historical in new[]{false,true}) {
 var model=new SudokuModel(); model.SetLevel(12); model.SetDifficulty(SudokuDifficulty.Advanced);
 GameDatabase.Records=Window(SudokuDifficulty.Advanced,0,0);
 var vm=new SudokuViewModel(model);
 if(historical) vm.RetryGameData=(12,12,5,0);
 var machine=new GameStateMachine(); var state=new LoseState(vm,machine); machine.Current=state; state.Enter();
 vm.RetryGameRequested.Value=true;
 Equal(12,model.CurrentLevel,"retry level"); Equal(SudokuDifficulty.Advanced,model.CurrentDifficulty,"retry difficulty");
 Equal(historical?12:-1,vm.RetryGameData.id,"retry identity"); Equal(1,machine.Transitions,"retry transition");
}
foreach(bool win in new[]{false,true}) {
 var model=new SudokuModel(); model.SetLevel(7); model.SetDifficulty(SudokuDifficulty.Moderate);
 int max=ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Advanced);
 // Latest loss after four perfect wins must still promote at exactly 80%.
 GameDatabase.Records=Window(SudokuDifficulty.Advanced,win?4:0,win?4*max:0);
 if(win) { GameDatabase.Records[0].IsWon=false; GameDatabase.Records[4].IsWon=true; }
 var vm=new SudokuViewModel(model); vm.RetryGameData=(7,7,4,0);
 var machine=new GameStateMachine(); IGameState state=win?new WinState(vm,machine):new LoseState(vm,machine);
 machine.Current=state; state.Enter(); vm.NewGameRequested.Value=true;
 Equal(win?SudokuDifficulty.Hard:SudokuDifficulty.Moderate,model.CurrentDifficulty,"replay exit difficulty");
 Equal(-1,vm.RetryGameData.id,"replay cleared"); Equal(1,machine.Transitions,"new game transition");
 model.decreaseDifficulty(); Equal(win?SudokuDifficulty.Hard:SudokuDifficulty.Moderate,model.CurrentDifficulty,"repeat loss evaluator");
 model.increaseDifficulty(); Equal(win?SudokuDifficulty.Hard:SudokuDifficulty.Moderate,model.CurrentDifficulty,"repeat win evaluator");
}
Console.WriteLine($"PASS: {checks} assertions (production model and win/lose state handlers).");
