namespace MemorySimulator.Core;

public class AllocationResult
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public static AllocationResult Ok(string? message = null) =>
        new AllocationResult { Success = true, Message = message };

    public static AllocationResult Fail(string message) =>
        new AllocationResult { Success = false, Message = message };
}
