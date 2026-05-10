namespace MemorySimulator.Core;

public class MemoryManager
{

    public int TotalMemorySize { get; private set; }

    public List<Hole> FreePartitions { get; private set; } = new();

    public List<Segment> AllocatedPartitions { get; private set; } = new();

    public Dictionary<string, Process> ActiveProcesses { get; private set; } = new();

    public bool IsInitialized { get; private set; }

    public AllocationResult Initialize(int totalMemorySize, List<Hole> initialHoles)
    {
        
        var sizeResult = Validators.ValidateMemorySize(totalMemorySize);
        if (!sizeResult.Success) return sizeResult;

        
        var holesResult = Validators.ValidateHoles(totalMemorySize, initialHoles);
        if (!holesResult.Success) return holesResult;

        
        TotalMemorySize = totalMemorySize;
        FreePartitions = initialHoles.Select(h => h.Clone()).ToList();
        FreePartitions.Sort((a, b) => a.StartingAddress.CompareTo(b.StartingAddress));
        AllocatedPartitions.Clear();
        ActiveProcesses.Clear();
        IsInitialized = true;

        return AllocationResult.Ok("Memory initialized successfully.");
    }

    public void Reset()
    {
        TotalMemorySize = 0;
        FreePartitions.Clear();
        AllocatedPartitions.Clear();
        ActiveProcesses.Clear();
        IsInitialized = false;
    }

    public AllocationResult AllocateProcess(Process process, AllocationMethod method)
    {
        if (!IsInitialized)
            return AllocationResult.Fail("Memory has not been initialized. Please set up memory first.");

        
        var validationResult = Validators.ValidateProcess(process);
        if (!validationResult.Success) return validationResult;

        
        if (ActiveProcesses.ContainsKey(process.Id))
            return AllocationResult.Fail($"Process '{process.Id}' is already allocated. Choose a different ID.");

        
        
        var simulatedHoles = FreePartitions.Select(h => h.Clone()).ToList();
        var assignments = new List<(Segment segment, int baseAddress)>();

        foreach (var segment in process.Segments)
        {
            int holeIndex = FindHole(simulatedHoles, segment.Size, method);

            if (holeIndex == -1)
            {
                
                return AllocationResult.Fail(
                    $"Process '{process.Id}' cannot be allocated: " +
                    $"Segment '{segment.Name}' (size {segment.Size}) does not fit in any available hole.\n" +
                    $"No partial allocations were made.");
            }

            
            var chosenHole = simulatedHoles[holeIndex];
            int baseAddress = chosenHole.StartingAddress;
            assignments.Add((segment, baseAddress));

            
            if (chosenHole.Size == segment.Size)
            {
                
                simulatedHoles.RemoveAt(holeIndex);
            }
            else
            {
                
                chosenHole.StartingAddress += segment.Size;
                chosenHole.Size -= segment.Size;
            }
        }

        
        
        FreePartitions = simulatedHoles;

        foreach (var (segment, baseAddress) in assignments)
        {
            segment.BaseAddress = baseAddress;
            AllocatedPartitions.Add(segment);
        }

        ActiveProcesses[process.Id] = process;

        
        AllocatedPartitions.Sort((a, b) => a.BaseAddress.CompareTo(b.BaseAddress));

        return AllocationResult.Ok($"Process '{process.Id}' allocated successfully using {method}.");
    }

    public AllocationResult DeallocateProcess(string processId)
    {
        if (!IsInitialized)
            return AllocationResult.Fail("Memory has not been initialized.");

        if (!ActiveProcesses.ContainsKey(processId))
            return AllocationResult.Fail($"Process '{processId}' is not currently allocated.");

        var process = ActiveProcesses[processId];

        
        foreach (var segment in process.Segments)
        {
            FreePartitions.Add(new Hole(segment.BaseAddress, segment.Size));
            AllocatedPartitions.Remove(segment);
        }

        
        ActiveProcesses.Remove(processId);

        
        CoalesceHoles();

        return AllocationResult.Ok($"Process '{processId}' deallocated successfully. Adjacent holes merged.");
    }

    public List<MemoryBlock> GetMemoryLayout()
    {
        if (!IsInitialized) return new List<MemoryBlock>();

        var blocks = new List<MemoryBlock>();

        
        foreach (var seg in AllocatedPartitions)
        {
            blocks.Add(new MemoryBlock
            {
                StartAddress = seg.BaseAddress,
                Size = seg.Size,
                IsAllocated = true,
                SegmentName = seg.Name,
                ProcessId = seg.ProcessId
            });
        }

        foreach (var hole in FreePartitions)
        {
            blocks.Add(new MemoryBlock
            {
                StartAddress = hole.StartingAddress,
                Size = hole.Size,
                IsAllocated = false,
                SegmentName = null,
                ProcessId = null
            });
        }

        
        blocks.Sort((a, b) => a.StartAddress.CompareTo(b.StartAddress));

        
        
        var filledBlocks = new List<MemoryBlock>();
        int currentAddress = 0;

        foreach (var block in blocks)
        {
            if (block.StartAddress > currentAddress)
            {
                
                filledBlocks.Add(new MemoryBlock
                {
                    StartAddress = currentAddress,
                    Size = block.StartAddress - currentAddress,
                    IsAllocated = true,
                    SegmentName = "Reserved",
                    ProcessId = "OS"
                });
            }
            filledBlocks.Add(block);
            currentAddress = block.EndAddress;
        }

        
        if (currentAddress < TotalMemorySize)
        {
            filledBlocks.Add(new MemoryBlock
            {
                StartAddress = currentAddress,
                Size = TotalMemorySize - currentAddress,
                IsAllocated = true,
                SegmentName = "Reserved",
                ProcessId = "OS"
            });
        }

        return filledBlocks;
    }

    public List<Hole> GetFreePartitionsSnapshot() =>
        FreePartitions.Select(h => h.Clone()).ToList();

    public List<Segment> GetAllocatedPartitionsSnapshot() =>
        AllocatedPartitions.ToList();

    public List<string> GetActiveProcessIds() =>
        ActiveProcesses.Keys.OrderBy(k => k).ToList();

    

    private int FindHole(List<Hole> holes, int requiredSize, AllocationMethod method)
    {
        switch (method)
        {
            case AllocationMethod.FirstFit:
                return FindFirstFit(holes, requiredSize);

            case AllocationMethod.BestFit:
                return FindBestFit(holes, requiredSize);

            default:
                return -1;
        }
    }

    private int FindFirstFit(List<Hole> holes, int requiredSize)
    {
        
        var sorted = holes.OrderBy(h => h.StartingAddress).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].Size >= requiredSize)
            {
                
                return holes.IndexOf(sorted[i]);
            }
        }
        return -1;
    }

    private int FindBestFit(List<Hole> holes, int requiredSize)
    {
        int bestIndex = -1;
        int bestSize = int.MaxValue;

        for (int i = 0; i < holes.Count; i++)
        {
            if (holes[i].Size >= requiredSize && holes[i].Size < bestSize)
            {
                bestIndex = i;
                bestSize = holes[i].Size;
            }
        }

        return bestIndex;
    }

    private void CoalesceHoles()
    {
        if (FreePartitions.Count <= 1) return;

        
        FreePartitions.Sort((a, b) => a.StartingAddress.CompareTo(b.StartingAddress));

        var merged = new List<Hole> { FreePartitions[0] };

        for (int i = 1; i < FreePartitions.Count; i++)
        {
            var last = merged[merged.Count - 1];
            var current = FreePartitions[i];

            if (last.EndAddress == current.StartingAddress)
            {
                
                last.Size += current.Size;
            }
            else
            {
                merged.Add(current);
            }
        }

        FreePartitions = merged;
    }
}
