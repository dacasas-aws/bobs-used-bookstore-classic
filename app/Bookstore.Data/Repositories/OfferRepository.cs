using Amazon.Auth.AccessControlPolicy;
using Bookstore.Domain;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Bookstore.Data.Repositories
{
    public class OfferRepository : IOfferRepository
    {
        private readonly ApplicationDbContext dbContext;

        public OfferRepository(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<OfferStatistics> GetStatisticsAsync()
        {
            var startOfMonth = DateTime.UtcNow.StartOfMonth();

            // EF6 used GroupBy(x => 1) with filtered Count aggregates. That pattern is fragile to translate in
            // EF Core, so the statistics are computed with three server-side COUNT queries instead.
            var offersTotal = await dbContext.Offer.CountAsync();

            // EF6's GroupBy over an empty table yielded no group, so SingleOrDefaultAsync returned null.
            if (offersTotal == 0)
            {
                return null;
            }

            var pendingOffers = await dbContext.Offer.CountAsync(y => y.OfferStatus == OfferStatus.PendingApproval);
            var offersThisMonth = await dbContext.Offer.CountAsync(y => y.CreatedOn >= startOfMonth);

            return new OfferStatistics
            {
                PendingOffers = pendingOffers,
                OffersThisMonth = offersThisMonth,
                OffersTotal = offersTotal
            };
        }

        async Task IOfferRepository.AddAsync(Offer offer)
        {
            await Task.Run(() => dbContext.Offer.Add(offer));
        }

        Task<Offer> IOfferRepository.GetAsync(int id)
        {
            return dbContext.Offer.Include(x => x.Customer).SingleOrDefaultAsync(x => x.Id == id);
        }

        async Task<IPaginatedList<Offer>> IOfferRepository.ListAsync(OfferFilters filters, int pageIndex, int pageSize)
        {
            var query = dbContext.Offer.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filters.Author))
            {
                query = query.Where(x => x.Author.Contains(filters.Author));
            }

            if (!string.IsNullOrWhiteSpace(filters.BookName))
            {
                query = query.Where(x => x.BookName.Contains(filters.BookName));
            }

            if (filters.ConditionId.HasValue)
            {
                query = query.Where(x => x.ConditionId == filters.ConditionId);
            }

            if (filters.GenreId.HasValue)
            {
                query = query.Where(x => x.GenreId == filters.GenreId);
            }

            if (filters.OfferStatus.HasValue)
            {
                query = query.Where(x => x.OfferStatus == filters.OfferStatus);
            }

            query = query.Include(x => x.Customer)
                .Include(x => x.Condition)
                .Include(x => x.Genre);
         
                

            var result = new PaginatedList<Offer>(query, pageIndex, pageSize);

            await result.PopulateAsync();

            return result;
        }

        async Task<IEnumerable<Offer>> IOfferRepository.ListAsync(string sub)
        {
            return await dbContext.Offer
                .Include(x => x.BookType)
                .Include(x => x.Genre)
                .Include(x => x.Condition)
                .Include(x => x.Publisher)
                .Where(x => x.Customer.Sub == sub)
                .ToListAsync();
        }

        async Task IOfferRepository.SaveChangesAsync()
        {
            await dbContext.SaveChangesAsync();
        }
    }
}