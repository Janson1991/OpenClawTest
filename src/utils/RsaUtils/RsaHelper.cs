using System;
using System.Security.Cryptography;
using System.Text;

namespace RsaUtils;

/// <summary>
/// RSA 加密 / 解密 / 签名 / 验签工具类。
/// 默认使用 2048 位密钥，OAEP-SHA256 填充（加密）和 PSS-SHA256（签名）。
/// </summary>
public static class RsaHelper
{
    // -------------------------------------------------------------------------
    //  密钥生成
    // -------------------------------------------------------------------------

    /// <summary>
    /// 生成 RSA 密钥对，返回 (publicKeyPem, privateKeyPem)。
    /// </summary>
    /// <param name="keySize">密钥长度（位），默认 2048。</param>
    public static (string PublicKey, string PrivateKey) GenerateKeyPair(int keySize = 2048)
    {
        using var rsa = RSA.Create(keySize);
        var pub  = rsa.ExportRSAPublicKeyPem();
        var priv = rsa.ExportRSAPrivateKeyPem();
        return (pub, priv);
    }

    // -------------------------------------------------------------------------
    //  加密 / 解密（字符串）
    // -------------------------------------------------------------------------

    /// <summary>
    /// 用公钥加密 UTF-8 字符串，返回 Base64 密文。
    /// </summary>
    public static string Encrypt(string plainText, string publicKeyPem)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        ArgumentNullException.ThrowIfNull(publicKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var data       = Encoding.UTF8.GetBytes(plainText);
        var cipherData = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(cipherData);
    }

    /// <summary>
    /// 用私钥解密 Base64 密文，返回原始 UTF-8 字符串。
    /// </summary>
    public static string Decrypt(string cipherBase64, string privateKeyPem)
    {
        ArgumentNullException.ThrowIfNull(cipherBase64);
        ArgumentNullException.ThrowIfNull(privateKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var cipherData = Convert.FromBase64String(cipherBase64);
        var plainData  = rsa.Decrypt(cipherData, RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(plainData);
    }

    // -------------------------------------------------------------------------
    //  加密 / 解密（字节）
    // -------------------------------------------------------------------------

    /// <summary>
    /// 用公钥加密原始字节，返回密文字节。
    /// </summary>
    public static byte[] EncryptBytes(byte[] data, string publicKeyPem)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(publicKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    /// <summary>
    /// 用私钥解密字节数组，返回明文字节。
    /// </summary>
    public static byte[] DecryptBytes(byte[] cipherData, string privateKeyPem)
    {
        ArgumentNullException.ThrowIfNull(cipherData);
        ArgumentNullException.ThrowIfNull(privateKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        return rsa.Decrypt(cipherData, RSAEncryptionPadding.OaepSHA256);
    }

    // -------------------------------------------------------------------------
    //  数字签名 / 验签
    // -------------------------------------------------------------------------

    /// <summary>
    /// 用私钥对数据进行 SHA-256 + PSS 签名，返回 Base64 签名。
    /// </summary>
    public static string Sign(string data, string privateKeyPem)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(privateKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var bytes  = Encoding.UTF8.GetBytes(data);
        var sig    = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Convert.ToBase64String(sig);
    }

    /// <summary>
    /// 用公钥验证签名（SHA-256 + PSS），返回 true 表示签名有效。
    /// </summary>
    public static bool Verify(string data, string signatureBase64, string publicKeyPem)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signatureBase64);
        ArgumentNullException.ThrowIfNull(publicKeyPem);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var bytes = Encoding.UTF8.GetBytes(data);
        var sig   = Convert.FromBase64String(signatureBase64);
        return rsa.VerifyData(bytes, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }
}
