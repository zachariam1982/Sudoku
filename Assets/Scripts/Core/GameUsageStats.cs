public class GameUsageStats
{
    public int UndoUses     { get; private set; }
    public int PencilUses   { get; private set; }
    public int EraseUses    { get; private set; }
    public int SOSUses      { get; private set; }
    public int AutoFillUses { get; private set; }

    public void AddUndo()     => UndoUses++;
    public void AddPencil()   => PencilUses++;
    public void AddErase()    => EraseUses++;
    public void AddSOS()      => SOSUses++;
    public void AddAutoFill() => AutoFillUses++;

    public void Reset()
    {
        UndoUses     = 0;
        PencilUses   = 0;
        EraseUses    = 0;
        SOSUses      = 0;
        AutoFillUses = 0;
    }

    public void Load(
        int undo,
        int pencil,
        int erase,
        int sos,
        int autoFill)
    {
        UndoUses     = undo;
        PencilUses   = pencil;
        EraseUses    = erase;
        SOSUses      = sos;
        AutoFillUses = autoFill;
    }
}