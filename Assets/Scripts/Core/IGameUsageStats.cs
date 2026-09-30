public interface IGameUsageStats
{
    int UndoUses { get; }
    int PencilUses { get; }
    int EraseUses { get; }
    int SOSUses { get; }
    int AutoFillUses { get; }

    void AddUndo();
    void AddPencil();
    void AddErase();
    void AddSOS();
    void AddAutoFill();
    void Reset();
    void Load(int undo, int pencil, int erase, int sos, int autoFill);
}
