using ERP.Application.Common.Interfaces;
using ERP.Application.Modules.Finance.Commands;
using ERP.Domain.Modules.Finance;
using ERP.Shared.Contracts.Finance;
using NSubstitute;
using Xunit;

namespace ERP.Tests.Application;

public class CreateTransactionHandlerTests
{
    private readonly IFinancialTransactionRepository _transactions =
        Substitute.For<IFinancialTransactionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateTransactionHandler Handler => new(_transactions, _uow);

    [Fact]
    public async Task Valid_income_is_saved()
    {
        var cmd = new CreateTransactionCommand(new CreateTransactionRequest(
            "Mədaxil", "İcarə gəliri", 250m, new DateOnly(2026, 7, 23), "Nağd"));

        var result = await Handler.Handle(cmd, default);

        Assert.True(result.IsSuccess);
        await _transactions.Received(1).AddAsync(Arg.Any<FinancialTransaction>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_type_throws()
    {
        var cmd = new CreateTransactionCommand(new CreateTransactionRequest(
            "Naməlum", "X", 10m, new DateOnly(2026, 7, 23), "Nağd"));

        await Assert.ThrowsAsync<ERP.Domain.Exceptions.DomainException>(
            () => Handler.Handle(cmd, default));
    }
}
