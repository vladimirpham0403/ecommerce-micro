using System.Security.Cryptography.X509Certificates;

namespace Ecommerce.Auth.Common;

public sealed class SigningCertificateInventory
{
    private readonly List<X509Certificate2> _certificates = [];
    public IReadOnlyList<X509Certificate2> Certificates => _certificates;
    public void Add(X509Certificate2 certificate) => _certificates.Add(certificate);

    public DateTimeOffset? SigningValidUntil => _certificates.Count == 0
        ? null
        : _certificates.Max(c => new DateTimeOffset(c.NotAfter.ToUniversalTime(), TimeSpan.Zero));
}
