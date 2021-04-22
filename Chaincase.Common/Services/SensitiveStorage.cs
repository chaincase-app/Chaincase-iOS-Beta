using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using NBitcoin;
using Cryptor = Chaincase.Common.Services.AesThenHmac;

namespace Chaincase.Common.Services
{
    public class SensitiveStorage
    {
        private readonly IHsmStorage _hsm;
        private readonly Network _network;
        private const string I_KEY_LOC = "i_key";

        public SensitiveStorage(IHsmStorage hsm, Network network)
        {
            RNG = new RNGCryptoServiceProvider();
            _hsm = hsm;
            _network = network;
        }

        public async Task SetSeedWords(string password, string seedWords)
        {
            var iKey = await GetOrDefaultIntermediateKey(password);
            var encSeedWords = Cryptor.Encrypt(seedWords, iKey);
            await _hsm.SetAsync($"{_network}-cSeedWords", encSeedWords);
        }

        public async Task<string> GetSeedWords(string password)
        {
            var iKey = await GetOrDefaultIntermediateKey(password);

            var encSeedWords = await _hsm.GetAsync($"{_network}-cSeedWords");
            var seedWords = Cryptor.Decrypt(encSeedWords, iKey);
            return seedWords;
        }

        // Use an intermediate key. This way main password can be changed
        // out for a global pin in multi-wallet. Store it with biometrics
        // for access without a static password.
        public async Task<byte[]> GetOrDefaultIntermediateKey(string password)
        {
            byte[] iKey;
            try
            {
                string encIKeyString = await _hsm.GetAsync(I_KEY_LOC);
                byte[] encIKey = Convert.FromBase64String(encIKeyString);
                iKey = Cryptor.DecryptWithPassword(encIKey, password);
                return iKey;
            }
            // there must not be an intermediate key yet
            catch (Exception)
            {
                // default one at cryptographically-secure pseudo-random
                iKey = Cryptor.NewKey();

                // store it encrypted under the password
                var encIKey = Cryptor.EncryptWithPassword(iKey, password);
                var encIKeyString = Convert.ToBase64String(encIKey);
                await _hsm.SetAsync(I_KEY_LOC, encIKeyString);
                return iKey;
            }
        }
    }
}
