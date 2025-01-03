using AICode.Contracts;
using AICode.Database;
using AICode.Entities;
using AICode.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace AICode.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("expenses", async (
            CreateExpenseRequest request,
            IValidator<CreateExpenseRequest> validator,
            ApplicationDbContext context,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            var expense = request.ToExpense();
            context.Add(expense);

            await context.SaveChangesAsync(ct);

            return Results.Ok(expense);
        });

        app.MapPut("expenses/{id}", async (
            int id,
            UpdateExpenseRequest request,
            IValidator<UpdateExpenseRequest> validator,
            ApplicationDbContext context,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            var product = await context.Expenses.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product is null)
            {
                return Results.NotFound();
            }

            // Update product properties here
            product.Name = request.Name;
            product.Amount = request.Amount;
            product.CategoryId = request.CategoryId;
            product.Date = request.Date;
            product.Description = request.Description;
            product.IsDeleted = request.IsDeleted;

            await context.SaveChangesAsync(ct);
            await cache.RemoveAsync($"products-{id}", ct);

            return Results.NoContent();
        });

        app.MapGet("expenses", async (
            ApplicationDbContext context,
            CancellationToken ct,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? searchText = null) =>
        {
            var query = context.Expenses.AsNoTracking();

            query = query.Where(expense => !expense.IsDeleted);

            // Apply date range filter if startDate and endDate are provided
            if (startDate.HasValue)
            {
                query = query.Where(expense => expense.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(expense => expense.Date <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchPattern = $"%{searchText}%"; // Add wildcards for fuzzy search
                query = query.Where(expense =>
                    EF.Functions.Like(expense.Name.ToLower(), searchPattern.ToLower()) ||
                    EF.Functions.Like(expense.Amount.ToString(), searchPattern) ||
                    (expense.Category != null && EF.Functions.Like(expense.Category.Name.ToLower(), searchPattern.ToLower())) ||
                    (expense.Description != null && EF.Functions.Like(expense.Description.ToLower(), searchPattern.ToLower())));
            }

            // Pagination
            var expenses = await query
                .Select(expense => new ExpenseModel
                {
                    Id = expense.Id,
                    Name = expense.Name,
                    Amount = expense.Amount,
                    CategoryId = expense.CategoryId,
                    CategoryName = expense.Category.Name,
                    Date = expense.Date,
                    Description = expense.Description,
                    CreatedAt = expense.CreatedAt,
                    UpdatedAt = expense.UpdatedAt
                })
                .ToListAsync(ct);

            return Results.Ok(expenses);
        });

        app.MapGet("expenses/{id}", async (
            int id,
            ApplicationDbContext context,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            var product = await cache.GetAsync($"products-{id}",
                async token =>
                {
                    var product = await context.Expenses
                        .Select(expense => new ExpenseModel
                        {
                            Id = expense.Id,
                            Name = expense.Name,
                            Amount = expense.Amount,
                            CategoryId = expense.CategoryId,
                            CategoryName = expense.Category.Name,
                            Date = expense.Date,
                            Description = expense.Description,
                            CreatedAt = expense.CreatedAt,
                            UpdatedAt = expense.UpdatedAt
                        })
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == id, token);

                    return product;
                },
                CacheOptions.DefaultExpiration,
                ct);

            return product is null ? Results.NotFound() : Results.Ok(product);
        });


        /*app.MapDelete("expenses/{id}", async (
            int id,
            ApplicationDbContext context,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (product is null)
            {
                return Results.NotFound();
            }

            context.Remove(product);

            await context.SaveChangesAsync(ct);

            await cache.RemoveAsync($"products-{id}", ct);

            return Results.NoContent();
        });*/

        app.MapGet("getSummary", async (
            ApplicationDbContext context,
            IDistributedCache cache,
            CancellationToken ct,
            int? year = null,
            int? month = null) =>
        {
            var product = await cache.GetAsync($"get-summary-for-year-{year}-and-month-{month}",
                async token =>
                {
                    var response = new GetSummaryResponseDto();

                    response.GroupedByCategory = await context.Expenses
                    .Where(e => !e.IsDeleted) // Exclude deleted expenses
                    .GroupBy(e => e.Category.Name)
                    .Select(group => new CategoryGroupDto
                    {
                        Category = group.Key,
                        TotalAmount = group.Sum(e => e.Amount),
                        ExpenseCount = group.Count()
                    })
                    .ToListAsync();

                    var query = context.Expenses
                        .Where(e => !e.IsDeleted); // Exclude deleted expenses

                    // Apply year filter if provided
                    if (year.HasValue)
                    {
                        query = query.Where(e => e.Date.Year == year.Value);
                    }

                    // Apply month filter if provided
                    if (month.HasValue)
                    {
                        query = query.Where(e => e.Date.Month == month.Value);
                    }

                    // Grouping logic based on input parameters

                    if (year.HasValue && month.HasValue)
                    {
                        // Group by specific dates for the given year and month
                        response.GroupedByDate = await query
                            .GroupBy(e => e.Date.Date)
                            .Select(group => new DateGroupDto
                            {
                                Date = group.Key,
                                TotalAmount = group.Sum(e => e.Amount),
                                ExpenseCount = group.Count()
                            })
                            .ToListAsync();
                    }
                    else if (year.HasValue)
                    {
                        // Group by months for the given year
                        response.GroupedByMonth = Enumerable.Range(1, 12) // Generate all months (1 to 12)
                         .GroupJoin(
                             query.GroupBy(e => e.Date.Month)
                                  .Select(group => new
                                  {
                                      Month = group.Key,
                                      TotalAmount = group.Sum(e => e.Amount),
                                      ExpenseCount = group.Count()
                                  })
                                  .ToList(), // Ensure this is evaluated in-memory
                             month => month, // Outer key selector: month number
                             group => group.Month, // Inner key selector: grouped month
                             (month, group) => new MonthGroupDto
                             {
                                 Month = month,
                                 TotalAmount = group.FirstOrDefault()?.TotalAmount ?? 0, // Default to 0 if no data
                                 ExpenseCount = group.FirstOrDefault()?.ExpenseCount ?? 0 // Default to 0 if no data
                             }
                         )
                         .ToList(); // Use ToList() for IEnumerable
                    }
                    else
                    {
                        // Default: Group by years
                        response.GroupedByYear = await query
                            .GroupBy(e => e.Date.Year)
                            .Select(group => new YearGroupDto
                            {
                                Year = group.Key,
                                TotalAmount = group.Sum(e => e.Amount),
                                ExpenseCount = group.Count()
                            })
                            .ToListAsync();
                    }

                    return response;
                },
                CacheOptions.DefaultExpiration,
                ct);

            return product is null ? Results.NotFound() : Results.Ok(product);
        });
    }
}