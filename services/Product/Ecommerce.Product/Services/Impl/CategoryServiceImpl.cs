using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.Product.Common;
using Ecommerce.Product.Data;
using Ecommerce.Product.Domain;
using Ecommerce.Product.Dtos;
using Ecommerce.Product.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Product.Services.Impl;

public class CategoryServiceImpl(ProductDbContext db) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
        return categories.Select(c => c.ToResponse()).ToList();
    }

    public async Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var category = await db.Categories
                           .AsNoTracking()
                           .FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw AppException.NotFound(ErrorCodes.CategoryNotFound, $"Category with id: {id} not found!");
        return category.ToResponse();
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        await EnsureParentExistsAsync(request.ParentId, ct);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = Slug.GenerateSlug(request.Name),
            Description = request.Description,
            ParentId = request.ParentId,
            IsActive = request.IsActive,
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(category.Id, ct);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound(ErrorCodes.CategoryNotFound, $"Category with id: {id} not found!");

        if (request.ParentId == id)
        {
            throw AppException.BadRequest(ErrorCodes.ValidationError, "Category cannot be its own parent");
        }

        await EnsureParentExistsAsync(request.ParentId, ct);

        category.Slug = Slug.GenerateSlug(request.Name);
        category.Name = request.Name;
        category.Description = request.Description;
        category.ParentId = request.ParentId;
        category.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound(ErrorCodes.CategoryNotFound, $"Category with id: {id} not found!");

        if (await db.Products.AnyAsync(p => p.CategoryId == id, ct))
        {
            throw AppException.Conflict(ErrorCodes.ValidationError, $"Category {id} still has products");
        }

        if (await db.Categories.AnyAsync(c => c.ParentId == id, ct))
        {
            throw AppException.Conflict(ErrorCodes.ValidationError, $"Category {id} still has child categories");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureParentExistsAsync(Guid? parentId, CancellationToken ct)
    {
        if (parentId is { } pid && !await db.Categories.AnyAsync(c => c.Id == pid, ct))
        {
            throw AppException.BadRequest(ErrorCodes.ValidationError, $"Parent category {pid} not found");
        }
    }
}
