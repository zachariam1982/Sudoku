using System.Diagnostics;

public class Benchmark : System.IDisposable
{
    private string _name;
    private Stopwatch _sw;

    public Benchmark(string name)
    {
        _name = name;
        _sw = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _sw.Stop();
        UnityEngine.Debug.Log($"{_name} took {_sw.Elapsed.TotalMilliseconds} ms");
    }
}
