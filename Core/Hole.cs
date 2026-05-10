namespace MemorySimulator.Core;

public class Hole
{
    public int StartingAddress { get; set; }

    public int Size { get; set; }

    public int EndAddress => StartingAddress + Size;

    public Hole(int startingAddress, int size)
    {
        StartingAddress = startingAddress;
        Size = size;
    }

    public Hole Clone() => new Hole(StartingAddress, Size);

    public override string ToString() => $"Hole[{StartingAddress}..{EndAddress}) Size={Size}";
}
