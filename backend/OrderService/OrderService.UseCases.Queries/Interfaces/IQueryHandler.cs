namespace OrderService.UseCases.Queries.Interfaces;

public interface IQueryHandler<TQuery, TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
