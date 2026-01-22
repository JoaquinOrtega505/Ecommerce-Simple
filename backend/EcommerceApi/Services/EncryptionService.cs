using System.Security.Cryptography;
using System.Text;

namespace EcommerceApi.Services;

public class EncryptionService
{
    private readonly string _encryptionKey;

    public EncryptionService(IConfiguration configuration)
    {
        _encryptionKey = configuration["EncryptionSettings:Key"]
            ?? throw new InvalidOperationException("Encryption key no configurada");

        // Validar que la clave tenga exactamente 32 caracteres (256 bits)
        if (_encryptionKey.Length != 32)
        {
            throw new InvalidOperationException("La clave de encriptación debe tener exactamente 32 caracteres");
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
                aes.GenerateIV();

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // Escribir el IV al inicio del stream
                    msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al encriptar datos", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);

                // Leer el IV del inicio del buffer
                byte[] iv = new byte[aes.IV.Length];
                Array.Copy(buffer, 0, iv, 0, iv.Length);
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al desencriptar datos", ex);
        }
    }
}
