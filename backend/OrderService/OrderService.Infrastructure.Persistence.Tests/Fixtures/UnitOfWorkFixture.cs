using DotNetCore.CAP;
using Moq;
using Npgsql;
using OrderService.Infrastructure.Persistence.Provider;

namespace OrderService.Infrastructure.Persistence.Tests.Fixtures;

public class UnitOfWorkFixture(PostgreSqlFixture dbFixture)
{
    private readonly PostgreSqlFixture _dbFixture = dbFixture ?? throw new ArgumentNullException(nameof(dbFixture));
    private UnitOfWork.UnitOfWork? _unitOfWork;
    private Mock<ICapPublisher> _capPublisherMock = null!;

    public UnitOfWork.UnitOfWork UnitOfWork => _unitOfWork ?? throw new InvalidOperationException("UnitOfWork not initialized");

    public void Initialize()
    {
        _capPublisherMock = new Mock<ICapPublisher>();

        var connectionFactoryMock = new Mock<IPostgresConnectionFactory>();
        connectionFactoryMock
            .Setup(f => f.GetConnection())
            .Returns(() => new NpgsqlConnection(_dbFixture.ConnectionString));

        _unitOfWork = new UnitOfWork.UnitOfWork(connectionFactoryMock.Object, _capPublisherMock.Object);
    }
}
