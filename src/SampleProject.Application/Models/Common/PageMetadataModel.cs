namespace SampleProject.Application.Models.Common;

public sealed record PageMetadataModel(
    int TotalPagesCount,
    int CurrentPage,
    int TotalItemsCount);