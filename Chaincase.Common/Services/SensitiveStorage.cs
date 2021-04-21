using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;

namespace Chaincase.Common.Services
{
    public class SensitiveStorage
    {
        private readonly IHsmStorage _hsm;
        private readonly Config _config;
        private RNGCryptoServiceProvider RNG;
        private const int I_KEY_LENGTH = 16; // bytes
        private const string I_KEY_LOC = "i_key";
        private const string I_KEY_SALT_LOC = "i_key_salt";


        public SensitiveStorage(IHsmStorage hsm, Config config)
        {
            RNG = new RNGCryptoServiceProvider();
            _hsm = hsm;
            _config = config;
        }

        public async Task SetSeedWords(string password, string seedWords)
        {
            var encryptor = new AesGcmService(await GetOrDefaultIntermediateKey(password));

            var cSeedWords = encryptor.Encrypt(seedWords);
            await _hsm.SetAsync($"{_config.Network}-seedWords", cSeedWords);
        }

        // Use an intermediate key. That way the main password could be changed
        // out for a pin with high iterations in multi-wallet or with biometrics
        // without storing the password.
        public async Task<byte[]> GetOrDefaultIntermediateKey(string password)
        {
            byte[] iKey = new byte[I_KEY_LENGTH];
            try
            {
                string iKeySaltString = await _hsm.GetAsync(I_KEY_SALT_LOC);
                byte[] iKeySalt = Convert.FromBase64String(iKeySaltString);
                var decryptor = new AesGcmService(password, iKeySalt);
                string encIKeyString = await _hsm.GetAsync(I_KEY_LOC);
                var iKeyString = decryptor.Decrypt(encIKeyString);
                iKey = Encoding.UTF8.GetBytes(iKeyString);
                return iKey;
            }
            // there must not be an intermediate key yet
            catch (Exception)
            {
                // pick one at cryptographically-secure pseudo-random
                RNG.GetBytes(iKey);
                var iKeyString = Convert.ToBase64String(iKey);

                // since we're password protecting it, come up with a unique salt
                var iKeySalt = new byte[8];
                RNG.GetBytes(iKeySalt);

                // store it encrypted under the password
                var encryptor = new AesGcmService(password, iKeySalt);
                var encIKeyString = encryptor.Encrypt(iKeyString);

                var iKeySaltString = Convert.ToBase64String(iKeySalt);
                await _hsm.SetAsync(I_KEY_SALT_LOC, iKeySaltString);
                await _hsm.SetAsync(I_KEY_LOC, encIKeyString);
                return iKey;
            }
        }
    }
}
