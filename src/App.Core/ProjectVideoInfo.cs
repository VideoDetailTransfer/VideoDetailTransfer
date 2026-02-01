namespace VideoDetailTransfer.Core;


public sealed class ProjectVideoInfo
{
    public VideoDescriptor? ReferenceOriginal { get; set; }
    public VideoDescriptor? TargetOriginal { get; set; }
    public VideoDescriptor? ReferenceCanonical { get; set; }
}
