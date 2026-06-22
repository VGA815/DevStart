using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.PromoCodes;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.PromoCodes.GetPromoCodes
{
    internal sealed class GetPromoCodesQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetPromoCodesQuery, List<PromoCodeResponse>>
    {
        public async Task<Result<List<PromoCodeResponse>>> Handle(
            GetPromoCodesQuery query,
            CancellationToken cancellationToken)
        {
            IQueryable<PromoCode> promoCodes = context.PromoCodes.AsNoTracking();

            if (query.ActiveOnly == true)
            {
                promoCodes = promoCodes.Where(p => p.IsActive);
            }

            int pageSize = query.PageSize is > 0 and <= 200 ? query.PageSize : 50;
            int pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;

            List<PromoCodeResponse> items = await promoCodes
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PromoCodeResponse
                {
                    Id = p.Id,
                    Code = p.Code,
                    DiscountType = p.DiscountType,
                    DiscountValue = p.DiscountValue,
                    FreePeriodDays = p.FreePeriodDays,
                    Plan = p.Plan,
                    MaxRedemptions = p.MaxRedemptions,
                    RedeemedCount = p.RedeemedCount,
                    ValidFrom = p.ValidFrom,
                    ValidUntil = p.ValidUntil,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
