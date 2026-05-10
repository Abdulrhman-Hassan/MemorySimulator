namespace MemorySimulator.Core;

public class MemoryBlock
{
    public int StartAddress { get; set; }

    public int Size { get; set; }

    public bool IsAllocated { get; set; }

    public string? SegmentName { get; set; }

    public string? ProcessId { get; set; }

    public int EndAddress => StartAddress + Size;

    public string DisplayLabel => IsAllocated
        ? $"{ProcessId}: {SegmentName}"
        : "Free";

    public string AddressRange => $"{StartAddress} - {EndAddress}";

    public string SizeLabel => $"{Size}";
}
