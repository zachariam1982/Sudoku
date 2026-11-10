using System;
/// <summary>Ordered cell indexes, from bulb to tip. Digits strictly increase.</summary>
[Serializable]
public sealed class Thermometer
{
    public int[] Cells = Array.Empty<int>();
    public Thermometer() { }
    public Thermometer(int[] cells) { Cells = cells == null ? Array.Empty<int>() : (int[])cells.Clone(); }
}
