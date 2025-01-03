﻿using AICode.Contracts;
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

            await context.SaveChangesAsync(ct);
            await cache.RemoveAsync($"products-{id}", ct);

            return Results.NoContent();
        });
        
        app.MapGet("expenses", async (
            ApplicationDbContext context,
            CancellationToken ct,
            int page = 1,
            int pageSize = 10) =>
        {
            var products = await context.Expenses
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Results.Ok(products);
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
    }
}