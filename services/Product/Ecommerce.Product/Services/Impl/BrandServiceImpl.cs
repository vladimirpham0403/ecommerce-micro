using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.Product.Common;
using Ecommerce.Product.Data;
using Ecommerce.Product.Domain;
using Ecommerce.Product.Dtos;
using Ecommerce.Product.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Product.Services.Impl;

public class BrandServiceImpl(ProductDbContext db) : IBrandService
{
    public async Task<IReadOnlyList<BrandResponse>> ListAsync(CancellationToken ct)
    {
        var brands = await db.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync(ct);
        return brands.Select(b => b.ToResponse()).ToList();
    }

    public async Task<BrandResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var brand = await db.Brands
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Id == id, ct)
                    ?? throw AppException.NotFound(ErrorCodes.BrandNotFound, $"Brand with id: {id} not found!");
        return brand.ToResponse();
    }

    public async Task<BrandResponse> CreateAsync(CreateBrandRequest request, CancellationToken ct)
    {
        var brandSlug = Slug.GenerateSlug(request.Name);
        var brand = new Brand
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            Description = request.Description,
            Slug = brandSlug,
            LogoUrl = request.LogoUrl
        };

        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(brand.Id, ct);
    }

    public async Task<BrandResponse> UpdateAsync(Guid id, UpdateBrandRequest request, CancellationToken ct)
    {
        var brand = await db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw AppException.NotFound(ErrorCodes.BrandNotFound, $"Brand with id: {id} not found!");

        brand.Slug = Slug.GenerateSlug(request.Name);
        brand.Name = request.Name;
        brand.Description = request.Description;
        brand.LogoUrl = request.LogoUrl;
        brand.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var brand = await db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw AppException.NotFound(ErrorCodes.BrandNotFound, $"Brand with id: {id} not found!");

        if (await db.Products.AnyAsync(p => p.BrandId == id, ct))
        {
            throw AppException.Conflict(ErrorCodes.ValidationError, $"Brand {id} still has products");
        }

        db.Brands.Remove(brand);
        await db.SaveChangesAsync(ct);
    }
}
