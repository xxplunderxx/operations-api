namespace Operations.Api.Endpoints;

internal static class EndpointProblem
{
    public static IResult NotFound(string code, string detail) => Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Resource not found",
        detail: detail,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
