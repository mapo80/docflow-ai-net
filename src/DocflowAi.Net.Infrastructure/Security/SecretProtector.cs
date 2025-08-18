using System.Security.Cryptography;
using System.Text;
using DocflowAi.Net.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DocflowAi.Net.Infrastructure.Security;

/// <summary>
/// AES based implementation of <see cref="ISecretProtector"/>.
/// </summary>
public class SecretProtector : ISecretProtector
{
    private readonly byte[] _key;

    public SecretProtector(IConfiguration configuration)
    {
        var key = configuration["EncryptionKey"] ?? "docflow-default-key-docflow-default-key"; // 32+ chars
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }

    public string Protect(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var input = Encoding.UTF8.GetBytes(plainText);
        var cipher = encryptor.TransformFinalBlock(input, 0, input.Length);
        var result = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(result);
    }

    public string Unprotect(string cipherText)
    {
        var data = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _key;
        var iv = new byte[aes.BlockSize / 8];
        Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var cipher = new byte[data.Length - iv.Length];
        Buffer.BlockCopy(data, iv.Length, cipher, 0, cipher.Length);
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
