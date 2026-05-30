using System;
using System.Text;
using Xunit;
using RsaUtils;

namespace RsaUtils.Tests;

public class RsaHelperTests
{
    // 复用同一个密钥对，避免每个测试都重新生成
    private static readonly (string Pub, string Priv) KeyPair2048 =
        RsaHelper.GenerateKeyPair(2048);

    // -------------------------------------------------------------------------
    //  密钥生成
    // -------------------------------------------------------------------------

    [Fact]
    public void GenerateKeyPair_DefaultSize_ReturnsPemStrings()
    {
        var (pub, priv) = RsaHelper.GenerateKeyPair();
        Assert.Contains("PUBLIC KEY", pub);
        Assert.Contains("PRIVATE KEY", priv);
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    public void GenerateKeyPair_VariousSizes_Succeed(int keySize)
    {
        var (pub, priv) = RsaHelper.GenerateKeyPair(keySize);
        Assert.NotEmpty(pub);
        Assert.NotEmpty(priv);
    }

    // -------------------------------------------------------------------------
    //  加密 / 解密（字符串）
    // -------------------------------------------------------------------------

    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalText()
    {
        const string plain = "Hello, RSA! 你好世界 🔐";
        var cipher = RsaHelper.Encrypt(plain, KeyPair2048.Pub);
        var decrypted = RsaHelper.Decrypt(cipher, KeyPair2048.Priv);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void Encrypt_SamePlainText_ProducesDifferentCipherEachTime()
    {
        // OAEP 包含随机盐，相同明文每次密文不同
        const string plain = "test";
        var c1 = RsaHelper.Encrypt(plain, KeyPair2048.Pub);
        var c2 = RsaHelper.Encrypt(plain, KeyPair2048.Pub);
        Assert.NotEqual(c1, c2);
    }

    [Fact]
    public void Decrypt_WrongPrivateKey_ThrowsCryptographicException()
    {
        const string plain = "secret";
        var cipher = RsaHelper.Encrypt(plain, KeyPair2048.Pub);

        var (_, wrongPriv) = RsaHelper.GenerateKeyPair();
        Assert.ThrowsAny<Exception>(() => RsaHelper.Decrypt(cipher, wrongPriv));
    }

    [Fact]
    public void Encrypt_NullPlainText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RsaHelper.Encrypt(null!, KeyPair2048.Pub));
    }

    [Fact]
    public void Decrypt_NullCipher_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RsaHelper.Decrypt(null!, KeyPair2048.Priv));
    }

    // -------------------------------------------------------------------------
    //  加密 / 解密（字节）
    // -------------------------------------------------------------------------

    [Fact]
    public void EncryptDecryptBytes_RoundTrip_ReturnsOriginalBytes()
    {
        var original = Encoding.UTF8.GetBytes("Binary payload 二进制");
        var cipher   = RsaHelper.EncryptBytes(original, KeyPair2048.Pub);
        var restored = RsaHelper.DecryptBytes(cipher, KeyPair2048.Priv);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void EncryptBytes_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RsaHelper.EncryptBytes(null!, KeyPair2048.Pub));
    }

    // -------------------------------------------------------------------------
    //  签名 / 验签
    // -------------------------------------------------------------------------

    [Fact]
    public void SignVerify_ValidSignature_ReturnsTrue()
    {
        const string message = "Sign me 签名消息";
        var sig = RsaHelper.Sign(message, KeyPair2048.Priv);
        Assert.True(RsaHelper.Verify(message, sig, KeyPair2048.Pub));
    }

    [Fact]
    public void Verify_TamperedMessage_ReturnsFalse()
    {
        const string message  = "Original message";
        const string tampered = "Tampered message";
        var sig = RsaHelper.Sign(message, KeyPair2048.Priv);
        Assert.False(RsaHelper.Verify(tampered, sig, KeyPair2048.Pub));
    }

    [Fact]
    public void Verify_TamperedSignature_ReturnsFalse()
    {
        const string message = "Legit message";
        var sig  = RsaHelper.Sign(message, KeyPair2048.Priv);
        var sigBytes = Convert.FromBase64String(sig);
        sigBytes[0] ^= 0xFF; // 翻转第一个字节
        var badSig = Convert.ToBase64String(sigBytes);
        Assert.False(RsaHelper.Verify(message, badSig, KeyPair2048.Pub));
    }

    [Fact]
    public void Sign_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RsaHelper.Sign(null!, KeyPair2048.Priv));
    }

    [Fact]
    public void Verify_NullSignature_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RsaHelper.Verify("msg", null!, KeyPair2048.Pub));
    }
}
