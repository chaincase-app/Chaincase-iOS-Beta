using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Chaincase.Common.Services
{
    // https://stackoverflow.com/questions/60889345/using-the-aesgcm-class
    public class AesGcmService : IDisposable
    {
        private readonly AesGcm _aes;
        private const int ITERATIONS = 1000; // "A modest number" @ RFC 2898

        // <param name="key">a 256 bit key</param>
        public AesGcmService(byte[] key)
		{
            _aes = new AesGcm(key);
		}

        // <param name="salt">a UNIQUE value, e.g. a random 64 bits long</param>
        public AesGcmService(string password, byte[] salt)
        {
            // Derive key
            // AES key size is 16 bytes
            byte[] key = new Rfc2898DeriveBytes(password, salt, ITERATIONS).GetBytes(16);

            // Initialize AES implementation
            _aes = new AesGcm(key);
        }

        public string Encrypt(string plain)
        {
            // Get bytes of plaintext string
            byte[] plainBytes = Encoding.UTF8.GetBytes(plain);

            // Get parameter sizes
            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;
            int cipherSize = plainBytes.Length;

            // Write everything into a big array for easier encoding
            int encryptedDataLength = 4 + nonceSize + 4 + tagSize + cipherSize;
            // stackalloc will fail if we run out of memory so `new` is safer.
            Span<byte> encryptedData = encryptedDataLength < 1024 ? stackalloc byte[encryptedDataLength] : new byte[encryptedDataLength].AsSpan();

            // Copy parameters
            BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(0, 4), nonceSize);
            BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4), tagSize);
            var nonce = encryptedData.Slice(4, nonceSize);
            var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
            var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

            // Generate secure nonce
            RandomNumberGenerator.Fill(nonce);

            // Encrypt
            _aes.Encrypt(nonce, plainBytes.AsSpan(), cipherBytes, tag);

            // Encode for transmission
            return Convert.ToBase64String(encryptedData);
        }

        public string Decrypt(string cipher)
		{
            // Decode
            Span<byte> encryptedData = Convert.FromBase64String(cipher).AsSpan();

            // Extract parameter sizes
            int nonceSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(0, 4));
            int tagSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4));
            int cipherSize = encryptedData.Length - 4 - nonceSize - 4 - tagSize;

            // Extract parameters
            var nonce = encryptedData.Slice(4, nonceSize);
            var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
            var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

            // Decrypt
            Span<byte> plainBytes = cipherSize < 1024 ? stackalloc byte[cipherSize] : new byte[cipherSize];
            _aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            // Convert plain bytes back into string
            return Encoding.UTF8.GetString(plainBytes);
		}

        public void Dispose()
        {
            _aes.Dispose();
        }
    }
}
