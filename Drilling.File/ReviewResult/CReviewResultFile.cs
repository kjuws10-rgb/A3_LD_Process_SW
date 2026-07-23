using System.Globalization;
using Drilling.Common.Review;
using Drilling.File.Parser;

namespace Drilling.File.ReviewResult;

public sealed class CReviewResultFile(string configRoot) : IReviewResultFile
{
    private static readonly IReadOnlyList<string> Headers =
    [
        "SAVED_AT",
        "RECIPE_ID",
        "HOLE_KEY",
        "HEAD_NO",
        "CELL_NO",
        "ERROR_X",
        "ERROR_Y",
        "JUDGE"
    ];

    private readonly string _reviewResultRoot = Path.Combine(
        Directory.GetParent(configRoot)?.FullName ?? configRoot,
        "Data",
        "ReviewResult");

    public Task Save(
        ST_REVIEW_RESULT_DATA result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = result.Results.Count > 0
            ? result.Results
            : result.Plan.ReviewPoints;

        CCsvParser.Write(
            GetResultPath(result),
            Headers,
            rows.Select(point => ToRow(result, point)));

        return Task.CompletedTask;
    }

    private string GetResultPath(ST_REVIEW_RESULT_DATA result)
    {
        var savedAt = result.SavedAt;
        var recipeId = SanitizeFileName(result.Plan.RecipeId);

        return Path.Combine(
            _reviewResultRoot,
            savedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            $"ReviewResult_{recipeId}_{savedAt:HHmmss}.csv");
    }

    private static IReadOnlyDictionary<string, string> ToRow(
        ST_REVIEW_RESULT_DATA result,
        ST_REVIEW_PLAN_POINT point)
    {
        var plan = result.Plan;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SAVED_AT"] = FormatDate(result.SavedAt),
            ["RECIPE_ID"] = plan.RecipeId,
            ["HOLE_KEY"] = $"CELL{point.CellNo}_{point.HoleName}",
            ["HEAD_NO"] = point.HeadNo.ToString(CultureInfo.InvariantCulture),
            ["CELL_NO"] = point.CellNo.ToString(CultureInfo.InvariantCulture),
            ["ERROR_X"] = FormatDouble(point.ErrorX),
            ["ERROR_Y"] = FormatDouble(point.ErrorY),
            ["JUDGE"] = point.Judge
        };
    }

    private static string FormatDate(DateTimeOffset? value)
    {
        return value?.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) ?? "";
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.000000", CultureInfo.InvariantCulture);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalidChars.Contains(character) ? '_' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "RECIPE" : sanitized;
    }
}
