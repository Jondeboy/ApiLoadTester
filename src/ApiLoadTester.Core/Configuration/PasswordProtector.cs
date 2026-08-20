using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace ApiLoadTester.Core.Configuration;

/// <summary>
/// Optional opt-in encryption of a certificate password at rest, using Windows DPAPI bound to the
/// current user. The resulting blob only decrypts on the same Windows account and machine, which is
/// a deliberate feature here (a copied scenario file silently fails to auto-fill the password rather
/// than leaking it) - this is never used to make passwords portable.
/// </summary>
public static class PasswordProtector
{
    private static readonly byte[] Entropy = "ApiLoadTester.PfxPassword.v1"u8.ToArray();

    public static string Protect(SecureString password)
    {
        var plainBytes = ToBytes(password);
        try
        {
            var protectedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    public static SecureString Unprotect(string protectedBase64)
    {
        var protectedBytes = Convert.FromBase64String(protectedBase64);
        var plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        try
        {
            var chars = Encoding.UTF8.GetString(plainBytes);
            var secure = new SecureString();
            foreach (var c in chars)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return secure;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    private static byte[] ToBytes(SecureString password)
    {
        var ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(password);
        try
        {
            var unicodeChars = new char[password.Length];
            System.Runtime.InteropServices.Marshal.Copy(ptr, unicodeChars, 0, password.Length);
            return Encoding.UTF8.GetBytes(unicodeChars);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }
}
