using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Auth.Dtos;
using Ecommerce.BuildingBlocks.Http;

namespace Ecommerce.Auth.Tests;

[Collection(AuthApiCollection.Name)]
public class UserAdminTests(AuthApiFactory factory)
{
    private const string AdminEmail = "admin@ecom.local";
    private const string AdminPassword = "Admin@123456";
    private const string CustomerEmail = "customer@ecom.local";
    private const string CustomerPassword = "Customer@123456";

    [Fact]
    public async Task Admin_can_list_users()
    {
        var client = await SignedInClientAsync(AdminEmail, AdminPassword);

        var response = await client.GetAsync("/v1/auth/users?search=admin@ecom.local");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserSummaryResponse>>>();
        var admin = Assert.Single(body!.Data!.Items);
        Assert.Equal(AdminEmail, admin.Email);
        Assert.Contains("Admin", admin.Roles);
    }

    [Fact]
    public async Task Admin_can_promote_a_user_and_the_new_admin_can_manage_users()
    {
        var email = NewEmail();
        var admin = await SignedInClientAsync(AdminEmail, AdminPassword);
        var userId = await RegisterAsync(email);

        var promote = await admin.PutAsJsonAsync(
            $"/v1/auth/users/{userId}/roles", new SetUserRolesRequest(["Admin", "Customer"]));

        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);

        var promoted = await promote.Content.ReadFromJsonAsync<ApiResponse<UserSummaryResponse>>();
        Assert.Equal(["Admin", "Customer"], promoted!.Data!.Roles);

        var newAdmin = await SignedInClientAsync(email, "Passw0rd!");
        Assert.Equal(HttpStatusCode.OK, (await newAdmin.GetAsync("/v1/auth/users")).StatusCode);
    }

    [Fact]
    public async Task Customer_token_cannot_manage_users()
    {
        var client = await SignedInClientAsync(CustomerEmail, CustomerPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/v1/auth/users")).StatusCode);
    }

    [Fact]
    public async Task Admin_token_without_users_manage_scope_is_rejected()
    {
        var client = await SignedInClientAsync(AdminEmail, AdminPassword, OidcFlowHelper.DefaultScope);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/v1/auth/users")).StatusCode);
    }

    [Fact]
    public async Task Request_without_token_is_rejected()
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await factory.CreateClient().GetAsync("/v1/auth/users")).StatusCode);
    }

    [Fact]
    public async Task Unknown_role_is_rejected()
    {
        var admin = await SignedInClientAsync(AdminEmail, AdminPassword);
        var userId = await RegisterAsync(NewEmail());

        var response = await admin.PutAsJsonAsync(
            $"/v1/auth/users/{userId}/roles", new SetUserRolesRequest(["Superuser"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("AUTH_ROLE_NOT_FOUND", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Setting_roles_of_an_unknown_user_returns_404()
    {
        var admin = await SignedInClientAsync(AdminEmail, AdminPassword);

        var response = await admin.PutAsJsonAsync(
            $"/v1/auth/users/{Guid.CreateVersion7()}/roles", new SetUserRolesRequest(["Customer"]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("AUTH_USER_NOT_FOUND", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Admin_cannot_remove_the_admin_role_from_themselves()
    {
        var admin = await SignedInClientAsync(AdminEmail, AdminPassword);
        var adminId = await FindUserIdAsync(admin, AdminEmail);

        var response = await admin.PutAsJsonAsync(
            $"/v1/auth/users/{adminId}/roles", new SetUserRolesRequest(["Customer"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("AUTH_CANNOT_DEMOTE_SELF", await ReadErrorCodeAsync(response));

        Assert.Contains("Admin", (await FindUserAsync(admin, AdminEmail)).Roles);
    }

    private async Task<HttpClient> SignedInClientAsync(
        string email, string password, string scope = OidcFlowHelper.AdminScope)
    {
        var client = factory.CreateClient();
        var tokens = await client.SignInAsync(email, password, scope);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    private async Task<Guid> RegisterAsync(string email)
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, "Passw0rd!", "Người dùng thử"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        return body!.Data!.Id;
    }

    private static async Task<Guid> FindUserIdAsync(HttpClient admin, string email) =>
        (await FindUserAsync(admin, email)).Id;

    private static async Task<UserSummaryResponse> FindUserAsync(HttpClient admin, string email)
    {
        var response = await admin.GetAsync($"/v1/auth/users?search={Uri.EscapeDataString(email)}");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserSummaryResponse>>>();
        return body!.Data!.Items.Single(u => u.Email == email);
    }

    private static string NewEmail() => $"user-{Guid.CreateVersion7():N}@ecom.test";

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return body?.Error?.Code;
    }
}
