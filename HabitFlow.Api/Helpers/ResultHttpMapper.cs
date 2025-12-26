using HabitFlow.Core.Abstractions;

namespace HabitFlow.Api.Helpers;

public static class ResultHttpMapper
{
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value);

        if (result.Errors.All(e => e.Title == ErrorTitles.ValidationError))
        {
            var dict = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Description).ToArray());

            return Results.ValidationProblem(dict, statusCode: 400);
        }

        return result.Error.Title switch
        {
            ErrorTitles.Conflict => Results.Conflict(),
            ErrorTitles.NotFound => Results.NotFound(),
            _ => Results.Problem(statusCode: 500)
        };
    }
}
