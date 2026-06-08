using System.Data;
using OrderService.Domain.Interfaces.Persistence;

namespace OrderService.Infrastructure.Persistence.Tests.Fixtures;

public class UnitOfWorkFixture(PostgreSqlFixture dbFixture)
{
    private readonly PostgreSqlFixture _dbFixture = dbFixture ?? throw new ArgumentNullException(nameof(dbFixture));
    private IUnitOfWork? _unitOfWork;

    public IUnitOfWork UnitOfWork => _unitOfWork ?? throw new InvalidOperationException("UnitOfWork not initialized");
    

    public void Initialize()
    {
        var connection = _dbFixture.GetConnection();
        _unitOfWork = new UnitOfWork(connection);
    }
}
