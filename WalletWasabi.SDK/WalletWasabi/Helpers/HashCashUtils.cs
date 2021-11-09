using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using NBitcoin;

namespace WalletWasabi.Helpers
{
	public static class HashCashUtils
	{
		public static string GenerateChallenge(string subject, DateTimeOffset expiry, int difficulty)
		{
			return
				$"H:{difficulty}:{expiry.ToUnixTimeSeconds()}:{subject}:SHA-256:{Convert.ToBase64String(Encoding.UTF8.GetBytes(GenerateString(5)))}";
		}

		public static string ComputeFromChallenge(string challenge)
		{
			var parts = challenge.Split(":");
			string version = parts[0];
			int difficulty = int.Parse(parts[1]);
			var expiry = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(parts[2]));
			var subject = parts[3];
			var algo = parts[4].ToLowerInvariant().Replace("-", "");
			var hashAlgo = algo == "sha256" ? (HashAlgorithm) new SHA256CryptoServiceProvider() : new SHA1CryptoServiceProvider();
			var nonce = parts[5];

			string stamp = null;
			int solution = int.MinValue;
			string solutionPrefix = "";

			while (!AcceptableHeader(stamp, difficulty, hashAlgo))
			{
				if (expiry <= DateTimeOffset.UtcNow)
				{
					throw new Exception("Hashcash challenge expired");
				}
				if (solution == int.MaxValue)
				{
					solutionPrefix += GenerateString(1);
					solution = int.MinValue;
				}
				else
				{
					solution++;
				}

				stamp = $"{challenge}:{solutionPrefix}{solution}";
			}

			return stamp;
		}

		private  static bool AcceptableHeader(string header, int bits, HashAlgorithm hashAlg)
		{
			if (header is null)
			{
				return false;
			}

			var hash = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(header));
			return GetStampHashDenomination(hash) == bits;
		}

		public static bool Verify(string stamp)
		{
			var parts = stamp.Split(":");
			var algo = parts[4].ToLowerInvariant().Replace("-", "");
			var hashAlgo = algo == "sha256"
				? (HashAlgorithm)new SHA256CryptoServiceProvider()
				: new SHA1CryptoServiceProvider();

			var difficulty = int.Parse(parts[1]);
			return difficulty <= GetStampHashDenomination(hashAlgo.ComputeHash(Encoding.UTF8.GetBytes(stamp)));
		}

		private static int GetStampHashDenomination(byte[] stampHash)
		{
			var continuousBits = new BitArray(stampHash);
			var denomination = 0;
			for (var bitIndex = 0; bitIndex < continuousBits.Length; bitIndex++)
			{
				var bit = continuousBits[bitIndex];

				if (bit)
				{
					break;
				}

				denomination++;
			}

			return denomination;
		}

		private static string GenerateString(int length)
		{
			var r = new Random(RandomUtils.GetInt32());

			var letters = new char[length];

			for (var i = 0; i < length; i++)
			{
				letters[i] = (char) (r.Next('A', 'Z' + 1));
			}
			return new string(letters);
		}
	}
}