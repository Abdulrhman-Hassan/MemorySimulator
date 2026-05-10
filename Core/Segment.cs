namespace MemorySimulator.Core;

public class Segment
{

    public string Name { get; set; }

    public int Size { get; set; }

    public int BaseAddress { get; set; } = -1;

    public int Limit => Size;

    public string ProcessId { get; set; }

    public int EndAddress => BaseAddress + Size;

    public Segment(string name, int size, string processId)
    {
        Name = name;
        Size = size;
        ProcessId = processId;
    }

    public override string ToString() => $"Segment[{ProcessId}.{Name}] Base={BaseAddress} Size={Size} Limit={Limit}";
}
