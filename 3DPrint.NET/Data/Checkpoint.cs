namespace _3DPrint.NET.Data;

public record struct Checkpoint(string Name, CheckpointStatus Status);

public enum CheckpointStatus {
    Unchecked,
    Current,
    Checked
}