using Ecommerce.Product.Common;

namespace Ecommerce.Product.Tests;

/** Unit test thuần cho bộ sinh slug — không cần DB. */
public class SlugTests
{
    [Theory]
    [InlineData("Áo Thun Đỏ", "ao-thun-do")] // bỏ dấu tiếng Việt, đ -> d
    [InlineData("  Hello  World ", "hello-world")] // trim + gộp khoảng trắng
    [InlineData("A---B", "a-b")] // gộp nhiều gạch nối
    [InlineData("iPhone 15 Pro (Max)!", "iphone-15-pro-max")] // bỏ ký tự đặc biệt
    public void GenerateSlug_normalizes_input(string input, string expected)
    {
        Assert.Equal(expected, Slug.GenerateSlug(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GenerateSlug_returns_empty_for_blank(string? input)
    {
        Assert.Equal(string.Empty, Slug.GenerateSlug(input!));
    }
}
