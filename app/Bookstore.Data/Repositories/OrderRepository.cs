using Bookstore.Domain;
using Bookstore.Domain.Books;
using Bookstore.Domain.Orders;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Bookstore.Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext dbContext;

        public OrderRepository(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        async Task IOrderRepository.AddAsync(Order order)
        {
            await Task.Run(() => dbContext.Order.Add(order));
        }

        async Task<Order> IOrderRepository.GetAsync(int id)
        {
            return await dbContext.Order
                .Include(x => x.Customer)
                .Include(x => x.Address)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                        .ThenInclude(b => b.BookType)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                        .ThenInclude(b => b.Condition)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                        .ThenInclude(b => b.Genre)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                        .ThenInclude(b => b.Publisher)
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        async Task<Order> IOrderRepository.GetAsync(int id, string sub)
        {
            return await dbContext.Order.SingleOrDefaultAsync(x => x.Id == id && x.Customer.Sub == sub);
        }

        async Task<IEnumerable<Book>> IOrderRepository.ListBestSellingBooksAsync(int count)
        {
            // EF Core cannot reliably translate GroupBy(...).Select(g => g.FirstOrDefault().Book), so the
            // top-selling BookIds are ranked on the server first and the books are then loaded by Id.
            var topBookIds = await dbContext.OrderItem
                .GroupBy(x => x.BookId)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Take(count)
                .ToListAsync();

            var books = await dbContext.Book
                .Where(x => topBookIds.Contains(x.Id))
                .ToListAsync();

            // Preserve the best-selling order.
            return topBookIds
                .Join(books, id => id, book => book.Id, (id, book) => book)
                .ToList();
        }

        async Task<OrderStatistics> IOrderRepository.GetStatisticsAsync()
        {
            var startOfMonth = DateTime.UtcNow.StartOfMonth();
            var now = DateTime.UtcNow;

            // EF6 used GroupBy(x => 1) with filtered Count aggregates. That pattern is fragile to translate in
            // EF Core, so the statistics are computed with server-side COUNT queries instead.
            var ordersTotal = await dbContext.Order.CountAsync();

            // EF6's GroupBy over an empty table yielded no group, so SingleOrDefaultAsync returned null.
            if (ordersTotal == 0)
            {
                return null;
            }

            var pendingOrders = await dbContext.Order.CountAsync(y => y.OrderStatus == OrderStatus.Pending);
            var pastDueOrders = await dbContext.Order.CountAsync(y => y.OrderStatus == OrderStatus.Ordered && y.DeliveryDate < now);
            var ordersThisMonth = await dbContext.Order.CountAsync(y => y.CreatedOn >= startOfMonth);

            return new OrderStatistics
            {
                PendingOrders = pendingOrders,
                PastDueOrders = pastDueOrders,
                OrdersThisMonth = ordersThisMonth,
                OrdersTotal = ordersTotal
            };
        }

        async Task<IPaginatedList<Order>> IOrderRepository.ListAsync(OrderFilters filters, int pageIndex, int pageSize)
        {
            var query = dbContext.Order.AsQueryable();

            if (filters.OrderStatusFilter.HasValue)
            {
                query = query.Where(x => x.OrderStatus == filters.OrderStatusFilter);
            }

            if (filters.OrderDateFromFilter.HasValue)
            {
                query = query.Where(x => x.CreatedOn >= filters.OrderDateFromFilter);
            }

            if (filters.OrderDateToFilter.HasValue)
            {
                var filterData = filters.OrderDateToFilter.Value.OneSecondToMidnight();
                query = query.Where(x => x.CreatedOn < filterData );
            }

            query = query
                .Include(x => x.Customer)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book);

            var result = new PaginatedList<Order>(query, pageIndex, pageSize);

            await result.PopulateAsync();

            return result;
        }

        async Task<IEnumerable<Order>> IOrderRepository.ListAsync(string sub)
        {
            return await dbContext.Order
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                .Where(x => x.Customer.Sub == sub)
                .ToListAsync();
        }

        async Task IOrderRepository.SaveChangesAsync()
        {
            await dbContext.SaveChangesAsync();
        }
    }
}