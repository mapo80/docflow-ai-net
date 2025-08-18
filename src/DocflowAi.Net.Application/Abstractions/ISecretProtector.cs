namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Provides encryption for sensitive secrets before persisting.
/// </summary>
public interface ISecretProtector
{
    /// <summary>
    /// Encrypts the provided plain text string.
    /// </summary>
    /// <param name="plainText">Secret value in plain text.</param>
    /// <returns>Encrypted representation of the secret.</returns>
    string Protect(string plainText);
    /// <summary>
    /// Decrypts the provided encrypted text.
    /// </summary>
    /// <param name="cipherText">Encrypted value.</param>
    /// <returns>Plain text secret.</returns>
    string Unprotect(string cipherText);
}
