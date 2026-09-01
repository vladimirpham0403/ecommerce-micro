using System.Net;
using System.Net.Http.Json;
using Ecommerce.BuildingBlocks.Http;
using Ecommerce.Product.Domain;
using Ecommerce.Product.Dtos;

namespace Ecommerce.Product.Tests;

/**
 * Ma trận phân quyền của Phase 1: đọc thì mở, ghi thì phải vừa đúng role vừa đủ scope.
 */
[Collection(ProductApiCollection.Name)]
public class ProductAuthorizationTests(ProductApiFactory factory)
{
    [Fact]
    public async Task Listing_products_is_public()
    {
        var resp = await factory.CreateClient().GetAsync("/v1/products");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Create_without_token_returns_401()
    {
        var resp = await factory.CreateClient().PostAsJsonAsync("/v1/products", NewProductRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("AUTH_UNAUTHENTICATED", await ReadErrorCodeAsync(resp));
    }

    [Fact]
    public async Task Create_with_expired_token_returns_401_token_expired()
    {
        var client = factory.CreateClientWithToken(TestTokens.Expired());

        var resp = await client.PostAsJsonAsync("/v1/products", NewProductRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("AUTH_TOKEN_EXPIRED", await ReadErrorCodeAsync(resp));
    }

    [Fact]
    public async Task Create_with_customer_role_returns_403()
    {
        var client = factory.CreateClientWithToken(TestTokens.Customer());

        var resp = await client.PostAsJsonAsync("/v1/products", NewProductRequest());

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal("AUTH_FORBIDDEN", await ReadErrorCodeAsync(resp));
    }

    [Fact]
    public async Task Create_with_admin_but_missing_write_scope_returns_403()
    {
        var client = factory.CreateClientWithToken(TestTokens.AdminWithoutWriteScope());

        var resp = await client.PostAsJsonAsync("/v1/products", NewProductRequest());

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        Assert.Equal("AUTH_FORBIDDEN", await ReadErrorCodeAsync(resp));
    }

    [Fact]
    public async Task Delete_without_token_returns_401()
    {
        var resp = await factory.CreateClient().DeleteAsync($"/v1/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Brands_and_categories_write_are_also_protected()
    {
        var client = factory.CreateClient();

        var brandResp = await client.PostAsJsonAsync("/v1/brands",
            new CreateBrandRequest(Name: "Nope", LogoUrl: null, Description: null));
        var categoryResp = await client.PostAsJsonAsync("/v1/categories",
            new CreateCategoryRequest(Name: "Nope", Description: null, ParentId: null));

        Assert.Equal(HttpStatusCode.Unauthorized, brandResp.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, categoryResp.StatusCode);
    }

    /*================ HELPER FUNCTIONS ================*/
    private static CreateProductRequest NewProductRequest() =>
        new(
            Sku: $"SKU-{Guid.NewGuid():N}"[..12],
            Name: "Sản phẩm test",
            Description: "desc",
            BrandId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            Price: 99_000m,
            Currency: "VND",
            Status: ProductStatus.Active,
            Attributes: null,
            Variants: null,
            Images: null);

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        return body.Error?.Code;
    }
}
