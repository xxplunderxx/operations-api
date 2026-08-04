using System.ComponentModel.DataAnnotations;

namespace Operations.Api.Options;

public sealed class CsvDataOptions
{
    public const string SectionName = "CsvData";

    [Required]
    public string DataDirectory { get; init; } = "Data";
}
