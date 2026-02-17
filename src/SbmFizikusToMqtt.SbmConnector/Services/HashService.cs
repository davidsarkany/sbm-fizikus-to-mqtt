using System.Security.Cryptography;
using System.Text;

namespace SbmFizikusToMqtt.SbmConnector.Services;

internal static class HashService
{
    public static string Sha256Hash(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}