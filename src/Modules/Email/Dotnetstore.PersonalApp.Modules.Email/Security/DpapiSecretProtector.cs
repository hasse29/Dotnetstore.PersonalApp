using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Dotnetstore.PersonalApp.Modules.Email.Security;

/// <summary>
/// Windows DPAPI for the current user: the stored value can only be decrypted by the same Windows account on the same
/// machine. The app ships as a Windows installer, so no cross-platform key store is needed.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Dotnetstore.PersonalApp.Email.v1");

    public string Protect(string plainText) =>
        Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), Entropy, DataProtectionScope.CurrentUser));

    public string Unprotect(string protectedText) =>
        Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protectedText), Entropy, DataProtectionScope.CurrentUser));
}
