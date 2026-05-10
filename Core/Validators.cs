namespace MemorySimulator.Core;

public static class Validators
{

    public static AllocationResult ValidateMemorySize(int totalMemorySize)
    {
        if (totalMemorySize <= 0)
            return AllocationResult.Fail("Total memory size must be a positive integer.");
        return AllocationResult.Ok();
    }

    public static AllocationResult ValidateHoles(int totalMemorySize, List<Hole> holes)
    {
        for (int i = 0; i < holes.Count; i++)
        {
            var hole = holes[i];

            if (hole.StartingAddress < 0)
                return AllocationResult.Fail(
                    $"Hole #{i + 1}: Starting address ({hole.StartingAddress}) must be >= 0.");

            if (hole.Size <= 0)
                return AllocationResult.Fail(
                    $"Hole #{i + 1}: Size ({hole.Size}) must be a positive integer.");

            if (hole.StartingAddress + hole.Size > totalMemorySize)
                return AllocationResult.Fail(
                    $"Hole #{i + 1}: Hole exceeds total memory. " +
                    $"Start ({hole.StartingAddress}) + Size ({hole.Size}) = {hole.StartingAddress + hole.Size} " +
                    $"> Total Memory ({totalMemorySize}).");
        }

        
        var sorted = holes.OrderBy(h => h.StartingAddress).ToList();
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (sorted[i].EndAddress > sorted[i + 1].StartingAddress)
                return AllocationResult.Fail(
                    $"Holes overlap: Hole at address {sorted[i].StartingAddress} " +
                    $"(size {sorted[i].Size}, ends at {sorted[i].EndAddress}) overlaps with " +
                    $"hole at address {sorted[i + 1].StartingAddress} (size {sorted[i + 1].Size}).");
        }

        return AllocationResult.Ok();
    }

    public static AllocationResult ValidateProcess(Process process)
    {
        if (string.IsNullOrWhiteSpace(process.Id))
            return AllocationResult.Fail("Process ID cannot be empty.");

        if (process.Segments == null || process.Segments.Count == 0)
            return AllocationResult.Fail($"Process '{process.Id}' must have at least one segment.");

        for (int i = 0; i < process.Segments.Count; i++)
        {
            var seg = process.Segments[i];

            if (string.IsNullOrWhiteSpace(seg.Name))
                return AllocationResult.Fail(
                    $"Process '{process.Id}', Segment #{i + 1}: Segment name cannot be empty.");

            if (seg.Size <= 0)
                return AllocationResult.Fail(
                    $"Process '{process.Id}', Segment '{seg.Name}': Size ({seg.Size}) must be a positive integer.");
        }

        
        var names = process.Segments.Select(s => s.Name).ToList();
        var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            return AllocationResult.Fail(
                $"Process '{process.Id}': Duplicate segment names found: {string.Join(", ", duplicates)}.");

        return AllocationResult.Ok();
    }
}
