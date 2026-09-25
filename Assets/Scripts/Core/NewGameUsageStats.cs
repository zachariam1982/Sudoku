public sealed class NewGameUsageStats : IGameUsageStats
{
    public int UndoUses => 0;
    public int PencilUses => 0;
    public int EraseUses => 0;
    public int SOSUses => 0;
    public int AutoFillUses => 0;

    public void AddUndo() { }
    public void AddPencil() { }
    public void AddErase() { }
    public void AddSOS() { }
    public void AddAutoFill() { }
    public void Reset() { }
    public void Load(int undo, int pencil, int erase, int sos, int autoFill) { }
}
