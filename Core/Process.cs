namespace MemorySimulator.Core;

public class Process
{

    public string Id { get; set; }

    public List<Segment> Segments { get; set; }

    public Process(string id, List<Segment> segments)
    {
        Id = id;
        Segments = segments;
    }

    public int TotalSize => Segments.Sum(s => s.Size);

    public override string ToString() => $"Process[{Id}] Segments={Segments.Count} TotalSize={TotalSize}";
}
