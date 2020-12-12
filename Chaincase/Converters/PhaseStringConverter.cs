using System;
using WalletWasabi.CoinJoin.Common.Models;
using ReactiveUI;
using Chaincase.Common.Models;

namespace Chaincase.Converters
{
	public class PhaseStringConverter : IBindingTypeConverter
	{
		public bool TryConvert(object from, Type toType, object conversionHint, out object result)
		{
			if (from is RoundPhaseState phase)
			{
				result = phase.Phase switch
				{
					RoundPhase.InputRegistration => "Registration",
					RoundPhase.ConnectionConfirmation => "Connection Confirmation",
					RoundPhase.OutputRegistration => "Output Registration",
					RoundPhase.Signing => "Signing",
					_ => ""
				};
				return true;
			}

			result = null;
			return false;
		}

		public int GetAffinityForObjects(Type fromType, Type toType)
		{
			if (fromType == typeof(RoundPhaseState) && toType == typeof(string))
			{
				return 100;
			}

			return 0;
		}
	}
}
