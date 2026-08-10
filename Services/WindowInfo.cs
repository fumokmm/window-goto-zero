namespace WindowGotoZero.Services;

internal sealed class WindowInfo
{
    public required IntPtr Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public required int ProcessId { get; init; }
    public required int Left { get; init; }
    public required int Top { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }

    public string DisplayText =>
        string.IsNullOrWhiteSpace(ProcessName)
            ? Title
            : $"{Title}  [{ProcessName}]";

    public string PositionText => $"{Left}, {Top}  ({Width}x{Height})";
}
