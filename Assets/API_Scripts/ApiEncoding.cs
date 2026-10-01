using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class ApiEncoding
{
    public static byte[] _tag;
    public static byte[] _iv;

    public static RSA LoadRsaPublicKey(string pem)
    {
        PemReader reader = new PemReader(new StringReader(pem));
        var keyObject = reader.ReadObject();

        var rsaParams = DotNetUtilities.ToRSAParameters((Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)keyObject);

        RSA rsa = RSA.Create();
        rsa.ImportParameters(rsaParams);
        return rsa;
    }

    public static AsymmetricKeyParameter ReadPublicKey(string pem)
    {
        using var sr = new StringReader(pem);
        var pr = new PemReader(sr);
        var obj = pr.ReadObject();

        if (obj is AsymmetricKeyParameter akp && akp.IsPrivate == false)
            return akp;

        if (obj is Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp)
            return kp.Public;

        throw new ArgumentException("Public key PEM không hợp lệ.");
    }

    //public static byte[] AesGcmEncrypt(byte[] plaintext, byte[] key, byte[] iv, out byte[] tag)
    //{
    //    byte[] ciphertext = new byte[plaintext.Length];
    //    tag = new byte[16];
    //    using var aes = new AesGcm(key);
    //    aes.ApiEncoding(iv, plaintext, ciphertext, tag);
    //    return ciphertext;
    //}

    public static byte[] AesGcmEncrypt(byte[] plaintext, byte[] key, byte[] iv, out byte[] tag)
    {
        // Validate
        if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (iv == null) throw new ArgumentNullException(nameof(iv));
        if (iv.Length != 12) throw new ArgumentException("IV (nonce) must be 12 bytes for AES-GCM");
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            throw new ArgumentException("Key must be 16, 24, or 32 bytes");

        const int TAG_SIZE_BYTES = 16;

        // AES-GCM (BouncyCastle)
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(key),
            TAG_SIZE_BYTES * 8, // tag size in bits
            iv,
            null                // AAD = null (giống AesGcm .NET)
        );

        cipher.Init(true, parameters);

        // Output của BC = ciphertext || tag
        byte[] output = new byte[cipher.GetOutputSize(plaintext.Length)];
        int len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
        len += cipher.DoFinal(output, len);

        int ciphertextLen = len - TAG_SIZE_BYTES;

        // Tách ciphertext và tag
        byte[] ciphertext = new byte[ciphertextLen];
        tag = new byte[TAG_SIZE_BYTES];

        Buffer.BlockCopy(output, 0, ciphertext, 0, ciphertextLen);
        Buffer.BlockCopy(output, ciphertextLen, tag, 0, TAG_SIZE_BYTES);

        return ciphertext;
    }

    public static string DecryptResponseAesGcm(string combinedBase64, byte[] aesKey)
    {
        byte[] fullBytes = Convert.FromBase64String(combinedBase64);

        // Định nghĩa kích thước chuẩn của GCM
        int ivLength = 12;
        int tagLength = 16;
        int cipherLength = fullBytes.Length - ivLength - tagLength;

        if (cipherLength < 0)
        {
            throw new ArgumentException("Dữ liệu không hợp lệ: Quá ngắn để chứa IV và Tag.");
        }

        // Cắt mảng byte bằng Span để tối ưu bộ nhớ
        ReadOnlySpan<byte> fullSpan = fullBytes.AsSpan();
        ReadOnlySpan<byte> nonce = fullSpan.Slice(0, ivLength); // 12 bytes đầu
        ReadOnlySpan<byte> tag = fullSpan.Slice(fullBytes.Length - tagLength, tagLength); // 16 bytes cuối
        ReadOnlySpan<byte> ciphertext = fullSpan.Slice(ivLength, cipherLength); // Phần giữa

        // // Buffer để chứa kết quả giải mã
        // byte[] plaintextBytes = new byte[cipherLength];

   
        var decryptedBytes = AesGcmDecrypt(ciphertext.ToArray(), aesKey, nonce.ToArray(), tag.ToArray());

        return Encoding.UTF8.GetString(decryptedBytes);
    }


    public static byte[] AesGcmDecrypt(byte[] ciphertext, byte[] key, byte[] iv, byte[] tag)
    {
        // Validate
        if (ciphertext == null) throw new ArgumentNullException(nameof(ciphertext));
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (iv == null) throw new ArgumentNullException(nameof(iv));
        if (tag == null) throw new ArgumentNullException(nameof(tag));
        if (iv.Length != 12) throw new ArgumentException("IV (nonce) must be 12 bytes for AES-GCM");
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            throw new ArgumentException("Key must be 16, 24, or 32 bytes");
        if (tag.Length != 16) throw new ArgumentException("Tag must be 16 bytes");

        const int TAG_SIZE_BYTES = 16;

        // AES-GCM (BouncyCastle)
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(key),
            TAG_SIZE_BYTES * 8, // tag size in bits
            iv,
            null                // AAD = null
        );

        cipher.Init(false, parameters);

        // Combine ciphertext + tag for BouncyCastle
        byte[] input = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, input, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, input, ciphertext.Length, tag.Length);

        byte[] plaintext = new byte[cipher.GetOutputSize(input.Length)];
        int len = cipher.ProcessBytes(input, 0, input.Length, plaintext, 0);
        len += cipher.DoFinal(plaintext, len);

        // Resize to actual plaintext length
        System.Array.Resize(ref plaintext, len);
        return plaintext;
    }



    public static byte[] RsaEncryptAesKey(AsymmetricKeyParameter publicKey, byte[] aesKey, out string alg)
    {
        if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));
        if (aesKey == null) throw new ArgumentNullException(nameof(aesKey));
        if (publicKey.IsPrivate)
            throw new ArgumentException("Public key is required for encryption");

        alg = "RSA-OAEP-SHA256";

        // OAEP with SHA-256 for hash + MGF1
        IAsymmetricBlockCipher cipher =
            new OaepEncoding(
                new RsaEngine(),
                new Sha256Digest(),
                new Sha256Digest(),
                null);

        cipher.Init(true, publicKey);

        return cipher.ProcessBlock(aesKey, 0, aesKey.Length);
    }

    [Serializable]
    public class SecureRequest
    {
        public string alg;
        public string key;
        public string iv;
        public string data;
        public string tag;
    }

    public static byte[] CreateAesKey()
    {
        byte[] aesKey = new byte[32];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(aesKey);
        }

        return aesKey;
    }

    public static byte[] CreateIv()
    {
        byte[] iv = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        return iv;
    }


    public static string BuildSecureRequest(AsymmetricKeyParameter publicKey, string payloadJson, byte[] aesKey, byte[] iv)
    {
        byte[] payload = Encoding.UTF8.GetBytes(
            payloadJson
        );
        byte[] cipherText = AesGcmEncrypt(payload, aesKey, iv, out byte[] tag);
        byte[] encryptedKey = RsaEncryptAesKey(publicKey, aesKey, out string alg);

        _tag = tag;
        _iv = iv;
        return JsonUtility.ToJson(new SecureRequest
        {
            alg = alg,
            key = Convert.ToBase64String(encryptedKey),
            iv = Convert.ToBase64String(iv),
            data = Convert.ToBase64String(cipherText),
            tag = Convert.ToBase64String(tag)
        });
    }


}
