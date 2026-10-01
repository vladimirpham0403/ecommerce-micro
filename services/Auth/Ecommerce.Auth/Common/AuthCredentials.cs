using System.Security.Cryptography.X509Certificates;
using OpenIddict.Server;

namespace Ecommerce.Auth.Common;

public static class AuthCredentials
{
    public static OpenIddictServerBuilder AddEcommerceCredentials(
        this OpenIddictServerBuilder builder,
        IConfiguration configuration,
        IHostEnvironment environment,
        SigningCertificateInventory inventory)
    {
        if (configuration.GetValue("Auth:Certificates:Ephemeral", false))
        {
            return builder.AddEphemeralEncryptionKey().AddEphemeralSigningKey();
        }

        var path = configuration["Auth:Certificates:Path"];
        if (!string.IsNullOrWhiteSpace(path))
        {
            var password = configuration["Auth:Certificates:Password"];

            if (!Directory.Exists(path))
            {
                throw new InvalidOperationException($"Không tìm thấy thư mục chứng thư '{path}'. Chạy scripts/gen-auth-certs.sh trước.");
            }

            var signing = LoadAll(path, "signing*.pfx", password);
            var encryption = LoadAll(path, "encryption*.pfx", password);

            if (signing.Count == 0 || encryption.Count == 0)
            {
                throw new InvalidOperationException($"Thư mục '{path}' phải có ít nhất một signing*.pfx và một encryption*.pfx.");
            }

            foreach (var certificate in signing)
            {
                builder.AddSigningCertificate(certificate);
                inventory.Add(certificate);
            }

            foreach (var certificate in encryption)
            {
                builder.AddEncryptionCertificate(certificate);
            }

            return builder;
        }

        if (environment.IsDevelopment())
        {
            return builder.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        }

        throw new InvalidOperationException("Ngoài Development bắt buộc phải cấu hình 'Auth:Certificates:Path'.");
    }

    private static List<X509Certificate2> LoadAll(string path, string pattern, string? password) =>
    [
        .. Directory.EnumerateFiles(path, pattern)
            .Order(StringComparer.Ordinal)
            .Select(file => X509CertificateLoader.LoadPkcs12FromFile(file, password))
    ];
}
