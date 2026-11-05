// Minimal infrastructure substitutes; progression model and result states are production code.
namespace UnityEngine {
 public static class Debug { public static void Log(object message) {} }
 public static class Mathf {
  public static int RoundToInt(float n) => (int)Math.Round(n);
  public static int Max(int a,int b) => Math.Max(a,b);
  public static float Exp(float n) => (float)Math.Exp(n);
 }
}
namespace SQLite {
 public class TableAttribute : Attribute { public TableAttribute(string name) {} }
 public class PrimaryKeyAttribute : Attribute {}
 public class AutoIncrementAttribute : Attribute {}
 public class NotNullAttribute : Attribute {}
}
public static class GameDatabase {
 public static List<GameRecord> Records = new();
 public static List<GameRecord> GetLastNRecordByDate(int n) => Records.Take(n).ToList();
}
public class GameStats {}
public sealed class SqliteGameHistoryRepository : IGameHistoryRepository {
 public void Insert(GameRecord record) {}
 public void Update(GameRecord record) {}
 public GameStats GetGameStats() => null;
 public int GetTotalPossiblePoints(GameStats stats) => 0;
 public List<GameRecord> GetAll() => GameDatabase.Records;
 public List<GameRecord> GetNextSet(int offset) => GameDatabase.Records.Skip(offset).Take(10).ToList();
 public List<GameRecord> GetLastNRecordByDate(int count) => GameDatabase.GetLastNRecordByDate(count);
 public GameRecord GetLastRecord() => GameDatabase.Records.OrderByDescending(record => record.Id).FirstOrDefault();
 public GameRecord GetFastestWin() => null;
 public int GetTotalPoints() => 0;
 public int GetTotalWins() => 0;
 public int GetTotalGamesPlayed() => 0;
 public void RebuildGameStats() {}
}
public class Observable<T> {
 T currentValue;
 public event Action<T> OnChanged;
 public T Value { get => currentValue; set { currentValue=value; OnChanged?.Invoke(value); } }
}
public class Command {
 readonly Action action;
 public Command(Action action) { this.action=action; }
 public void Execute() => action();
}
public class SudokuViewModel {
 public Observable<bool> IsTimerRunning = new(), IsLost = new(), IsWon = new(), NewGameRequested = new(), RetryGameRequested = new();
 public Observable<float> ElapsedSeconds = new();
 public (int id,int level,int difficulty,int points) RetryGameData = (-1,-1,-1,-1);
 public Command NextLevel, IncreaseDifficulty, DecreaseDifficulty;
 public SudokuViewModel(ClassicSudokuModel model) {
  NextLevel = new(() => model.SetLevel((GameDatabase.Records.FirstOrDefault()?.Level ?? model.CurrentLevel)+1));
  IncreaseDifficulty = new(() => model.increaseDifficulty());
  DecreaseDifficulty = new(() => model.decreaseDifficulty());
 }
}
public class GameStateMachine {
 public IGameState Current;
 public object Idle = new();
 public int Transitions;
 public void TransitionTo(object state) { Current.Exit(); Transitions++; }
}
public class User {
 public static User Instance = new();
 public int Saves;
 public void SaveNow() { Saves++; }
}
