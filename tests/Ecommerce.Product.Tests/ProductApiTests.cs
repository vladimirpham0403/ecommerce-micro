using System.Net;
using System.Net.Http.Json;
using Ecommerce.BuildingBlocks.Http;
using Ecommerce.Product.Domain;
using Ecommerce.Product.Dtos;

namespace Ecommerce.Product.Tests;

[Collection(ProductApiCollection.Name)]
public class ProductApiTests(ProductApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_then_get_returns_product_with_slug()
    {
        var (brandId, categoryId) = await SeedBrandAndCategoryAsync();
        var req = NewProductRequest(brandId, categoryId, name: "Áo Thun Đỏ");

        var created = await CreateProductAsync(req);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(req.Sku, created.Sku);
        Assert.Equal("ao-thun-do", created.Slug);
        Assert.Equal(req.Price, created.Price);
        Assert.Equal(brandId, created.BrandId);
        Assert.Equal(categoryId, created.CategoryId);
        
        var getResp = await _client.GetAsync($"/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await ReadDataAsync<ProductResponse>(getResp);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Slug, fetched.Slug);
    }

    [Fact]
    public async Task Get_unknown_id_returns_404_with_error_code()
    {
        var resp = await _client.GetAsync($"/v1/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Equal("PRODUCT_NOT_FOUND", body.Error!.Code);
    }

    [Fact]
    public async Task Create_with_duplicate_sku_returns_409()
    {
        var (brandId, categoryId) = await SeedBrandAndCategoryAsync();
        var req = NewProductRequest(brandId, categoryId);
        await CreateProductAsync(req);

        // Cùng SKU -> Conflict.
        var dupResp = await _client.PostAsJsonAsync("/v1/products", req);
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);
    }

    [Fact]
    public async Task Create_with_unknown_brand_returns_400()
    {
        var (_, categoryId) = await SeedBrandAndCategoryAsync();
        var req = NewProductRequest(brandId: Guid.NewGuid(), categoryId: categoryId);

        var resp = await _client.PostAsJsonAsync("/v1/products", req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Update_changes_fields()
    {
        var (brandId, categoryId) = await SeedBrandAndCategoryAsync();
        var created = await CreateProductAsync(NewProductRequest(brandId, categoryId));

        var update = new UpdateProductRequest(
            Name: "Tên Mới",
            Description: "updated",
            BrandId: brandId,
            CategoryId: categoryId,
            Price: 199_000m,
            Currency: "VND",
            Status: ProductStatus.Inactive,
            Attributes: null);

        var resp = await _client.PutAsJsonAsync($"/v1/products/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var updated = await ReadDataAsync<ProductResponse>(resp);
        Assert.Equal("Tên Mới", updated.Name);
        Assert.Equal("ten-moi", updated.Slug);
        Assert.Equal(199_000m, updated.Price);
        Assert.Equal(ProductStatus.Inactive, updated.Status);
    }

    [Fact]
    public async Task Delete_soft_deletes_then_get_returns_404()
    {
        var (brandId, categoryId) = await SeedBrandAndCategoryAsync();
        var created = await CreateProductAsync(NewProductRequest(brandId, categoryId));

        var delResp = await _client.DeleteAsync($"/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
        
        var getResp = await _client.GetAsync($"/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task List_filters_by_brand_and_reports_total()
    {
        var (brandId, categoryId) = await SeedBrandAndCategoryAsync();
        await CreateProductAsync(NewProductRequest(brandId, categoryId));
        await CreateProductAsync(NewProductRequest(brandId, categoryId));

        var resp = await _client.GetAsync($"/v1/products?brandId={brandId}&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var page = await ReadDataAsync<PagedResult<ProductListItem>>(resp);
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, i => Assert.False(string.IsNullOrEmpty(i.Slug)));
    }

    [Fact]
    public async Task Ready_healthcheck_is_healthy()
    {
        var resp = await _client.GetAsync("/ready");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
    
    /*================ HELPER FUNCTIONS ================*/
    private static CreateProductRequest NewProductRequest(Guid brandId, Guid categoryId, string name = "Sản phẩm test") =>
        new(
            Sku: $"SKU-{Guid.NewGuid():N}"[..12],
            Name: name,
            Description: "desc",
            BrandId: brandId,
            CategoryId: categoryId,
            Price: 99_000m,
            Currency: "VND",
            Status: ProductStatus.Active,
            Attributes: null,
            Variants: null,
            Images: null);

    private async Task<ProductResponse> CreateProductAsync(CreateProductRequest req)
    {
        var resp = await _client.PostAsJsonAsync("/v1/products", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return await ReadDataAsync<ProductResponse>(resp);
    }

    private async Task<(Guid brandId, Guid categoryId)> SeedBrandAndCategoryAsync()
    {
        var brandResp = await _client.PostAsJsonAsync("/v1/brands",
            new CreateBrandRequest(Name: $"Brand {Guid.NewGuid():N}", LogoUrl: null, Description: null));
        var brand = await ReadDataAsync<BrandResponse>(brandResp);

        var catResp = await _client.PostAsJsonAsync("/v1/categories",
            new CreateCategoryRequest(Name: $"Cat {Guid.NewGuid():N}", Description: null, ParentId: null));
        var cat = await ReadDataAsync<CategoryResponse>(catResp);

        return (brand.Id, cat.Id);
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.NotNull(body.Data);
        return body.Data!;
    }
}
