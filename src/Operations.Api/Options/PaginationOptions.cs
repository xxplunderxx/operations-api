using System.ComponentModel.DataAnnotations;

namespace Operations.Api.Options;

public sealed class PaginationOptions
{
    public const string SectionName = "Pagination";
    public const int AbsoluteMaximumPageSize = 500;

    [Range(1, AbsoluteMaximumPageSize)]
    public int DefaultPageSize { get; init; } = 100;

    [Range(1, AbsoluteMaximumPageSize)]
    public int MaxPageSize { get; init; } = AbsoluteMaximumPageSize;

    public bool IsValidDefault => DefaultPageSize <= MaxPageSize;
}
