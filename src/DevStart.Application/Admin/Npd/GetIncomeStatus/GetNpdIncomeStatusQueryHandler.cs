using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Payments.Npd;
using DevStart.SharedKernel;
using Microsoft.Extensions.Options;

namespace DevStart.Application.Admin.Npd.GetIncomeStatus
{
    internal sealed class GetNpdIncomeStatusQueryHandler(
        INpdIncomeService incomeService,
        IOptions<NpdOptions> npdOptions,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetNpdIncomeStatusQuery, NpdIncomeStatusResponse>
    {
        public async Task<Result<NpdIncomeStatusResponse>> Handle(
            GetNpdIncomeStatusQuery query,
            CancellationToken cancellationToken)
        {
            NpdOptions options = npdOptions.Value;
            int year = query.Year ?? incomeService.ResolveIncomeYear(dateTimeProvider.UtcNow);
            decimal income = await incomeService.GetYearToDateIncomeAsync(year, null, cancellationToken);

            return new NpdIncomeStatusResponse
            {
                Year = year,
                IncomeToDate = income,
                Limit = options.AnnualIncomeLimit,
                WarningAmount = options.WarningAmount,
                Remaining = Math.Max(0m, options.AnnualIncomeLimit - income),
                WarningReached = income >= options.WarningAmount,
                LimitReached = income >= options.AnnualIncomeLimit,
            };
        }
    }
}
