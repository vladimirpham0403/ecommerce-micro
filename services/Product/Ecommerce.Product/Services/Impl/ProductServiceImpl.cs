using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.BuildingBlocks.Http;
using Ecommerce.Product.Common;
using Ecommerce.Product.Data;
using Ecommerce.Product.Domain;
using Ecommerce.Product.Dtos;
using Ecommerce.Product.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Product.Services.Impl;

public class ProductServiceImpl(ProductDbContext db) : IProductService
{
    public async Task<PagedResult<ProductListItem>> ListAsync(ProductListQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var q = db.Products.AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        if (query.CategoryId is { } cat) q = q.Where(p => p.CategoryId == cat);
        if (query.BrandId is { } brand) q = q.Where(p => p.BrandId == brand);
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => EF.Functions.ILike(p.Name, $"%{query.Search}%"));

        var desc = !query.Order.Equals("asc", StringComparison.OrdinalIgnoreCase);
        q = (query.Sort.ToLowerInvariant(), desc) switch
        {
            ("price", true) => q.OrderByDescending(p => p.Price),
            ("price", false) => q.OrderBy(p => p.Price),
            ("name", true) => q.OrderByDescending(p => p.Name),
            ("name", false) => q.OrderBy(p => p.Name),
            (_, true) => q.OrderByDescending(p => p.CreatedAt),
            (_, false) => q.OrderBy(p => p.CreatedAt)
        };

        var total = await q.LongCountAsync(ct);
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<ProductListItem>(items.Select(p => p.ToListItem()).ToList(), page, size, total);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var product = await db.Products
                          .AsNoTracking()
                          .Include(p => p.Brand)
                          .Include(p => p.Category)
                          .Include(p => p.Variants)
                          .Include(p => p.Images)
                          .FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw AppException.NotFound(ErrorCodes.ProductNotFound, $"Product with id: {id} not found!");
        return product.ToResponse();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        await CheckBrandAndCategoryAsync(request.BrandId, request.CategoryId, ct);

        if (await db.Products.AnyAsync(p => p.Sku == request.Sku, ct))
            throw AppException.Conflict(ErrorCodes.ValidationError, $"Sku '{request.Sku}' already exists");

        var slug = await GenerateUniqueSlugAsync(request.Name, excludeId: null, ct);

        var product = new Domain.Product
        {
            Id = Guid.CreateVersion7(),
            Sku = request.Sku,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            Price = request.Price,
            Currency = request.Currency,
            Status = request.Status,
            Attributes = request.Attributes
        };

        if (request.Variants is { Count: > 0 } variants)
        {
            foreach (var v in variants)
            {
                product.Variants.Add(new ProductVariant
                {
                    Id = Guid.CreateVersion7(),
                    Sku = v.Sku,
                    Name = v.Name,
                    Price = v.Price,
                    Attributes = v.Attributes,
                    IsActive = v.IsActive
                });
            }
        }

        if (request.Images is { Count: > 0 } images)
        {
            foreach (var i in images)
            {
                product.Images.Add(new ProductImage
                {
                    Id = Guid.CreateVersion7(),
                    Url = i.Url,
                    AltText = i.AltText,
                    SortOrder = i.SortOrder,
                    IsPrimary = i.IsPrimary
                });
            }
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(product.Id, ct);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw AppException.NotFound(ErrorCodes.ProductNotFound, $"Product with id: {id} not found!");

        await CheckBrandAndCategoryAsync(request.BrandId, request.CategoryId, ct);

        product.Slug = await GenerateUniqueSlugAsync(request.Name, excludeId: id, ct);
        product.Name = request.Name;
        product.Description = request.Description;
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.Currency = request.Currency;
        product.Status = request.Status;
        product.Attributes = request.Attributes;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw AppException.NotFound(ErrorCodes.ProductNotFound, $"Product with id: {id} not found!");

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
    }

    private async Task CheckBrandAndCategoryAsync(Guid brandId, Guid categoryId, CancellationToken ct)
    {
        if (!await db.Brands.AnyAsync(b => b.Id == brandId, ct))
            throw AppException.BadRequest(ErrorCodes.ValidationError, $"Brand {brandId} not found");
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            throw AppException.BadRequest(ErrorCodes.ValidationError, $"Category {categoryId} not found");
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var baseSlug = Slug.GenerateSlug(name);
        var finalSlug = baseSlug;
        var cnt = 1;
        while (await db.Products.AnyAsync(p => p.Id != excludeId && p.Slug == finalSlug, ct))
        {
            finalSlug = $"{baseSlug}-{cnt}";
            cnt++;
        }

        return finalSlug;
    }
}
