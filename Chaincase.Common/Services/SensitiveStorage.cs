using System;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using NBitcoin;

namespace Chaincase.Common.Services
{
    public class SensitiveStorage
    {
        private readonly IHsmStorage _hsm;
        private readonly Network _network;
        private readonly UiConfig _uiConfig;
        private const string I_KEY_LOC = "i_key";
        public string EncSeedWordsLoc => $"{_network}-encSeedWords";

        public SensitiveStorage(IHsmStorage hsm, Config config, UiConfig uiConfig)
        {
            _hsm = hsm;
            _network = config.Network;
            _uiConfig = uiConfig;
        }

        public async Task SetSeedWords(string password, string seedWords)
        {
            var iKey = await GetOrGenerateIntermediateKey(password);
            var encSeedWords = AesThenHmac.Encrypt(seedWords, iKey);
            await _hsm.SetAsync(EncSeedWordsLoc, encSeedWords);
        }

        public async Task<string> GetSeedWords(string password)
        {
            var iKey = await GetIntermediateKey(password);
            var encSeedWords = await _hsm.GetAsync(EncSeedWordsLoc);
            var seedWords = AesThenHmac.Decrypt(encSeedWords, iKey);
            return seedWords;
        }

        // Use an intermediate key. This way main password can be changed
        // out for a global pin in multi-wallet. Store it with biometrics
        // for access without a static password.
        public async Task<byte[]> GetOrGenerateIntermediateKey(string password)
        {
            byte[] iKey = await GetIntermediateKey(password);
            if (iKey is null)
                // default one at cryptographically-secure pseudo-random
                iKey = AesThenHmac.NewKey();

            // store it encrypted under the password
            byte[] encIKey = AesThenHmac.EncryptWithPassword(iKey, password);
            string encIKeyString = Convert.ToBase64String(encIKey);
            await _hsm.SetAsync(I_KEY_LOC, encIKeyString);
            _uiConfig.HasIntermediateKey = true;
            _uiConfig.ToFile();
            return iKey;

        }

        public async Task<byte[]> GetIntermediateKey(string password)
        {
            if (_uiConfig.HasIntermediateKey)
            {
                // throws if it fails
                string encIKeyString = await _hsm.GetAsync(I_KEY_LOC);
                if (encIKeyString is null)
                {
	                _uiConfig.HasIntermediateKey = false;
	                return null;
                }
                byte[] encIKey = Convert.FromBase64String(encIKeyString);
                byte[] iKey = AesThenHmac.DecryptWithPassword(encIKey, password);
                return iKey;
            }
            return null;
        }
    }
}
