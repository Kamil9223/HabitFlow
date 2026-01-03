using HabitFlow.Api.Contracts.Today;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Features.Today;

namespace HabitFlow.Api.Endpoints;

public static class TodayEndpoints
{
    public static void MapTodayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/today")
            .WithTags("Today")
            .RequireAuthorization();

        group.MapGet("/", async (
            DateOnly? date,
            IQueryDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            // TODO: Get real UserId from authenticated user context
            var userId = "temp-user-id";

            var query = new GetTodayQuery(userId, date);
            var result = await dispatcher.Dispatch(query, cancellationToken);

            return result.ToHttpResult(dto => Results.Ok(new TodayResponse(
                dto.Date,
                dto.Items.Select(item => new TodayItem(
                    item.HabitId,
                    item.Title,
                    item.Type,
                    item.CompletionMode,
                    item.TargetValue,
                    item.TargetUnit,
                    item.IsPlanned,
                    item.HasCheckin
                )).ToList()
            )));
        })
            .WithName("GetToday")
            .Produces<TodayResponse>(200)
            .Produces(401);
    }
}
