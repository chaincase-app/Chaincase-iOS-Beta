using System;
using Chaincase.Common.Models;
using ReactiveUI;
using WalletWasabi.Exceptions;
using WalletWasabi.Helpers;

namespace Chaincase.Converters
{
	public class FeeTargetTimeConverter : IBindingTypeConverter
	{
		public bool TryConvert(object from, Type toType, object conversionHint, out object result)
		{
			if (from is int feeTarget)
			{
				if (feeTarget >= Constants.TwentyMinutesConfirmationTarget && feeTarget <= 6) // minutes
				{
					result = $"{feeTarget}0 minutes";
					return true;
				}
				else if (feeTarget >= 7 && feeTarget <= Constants.OneDayConfirmationTarget) // hours
				{
					var hours = feeTarget / 6; // 6 blocks per hour
					result = $"{hours} {IfPlural(hours, "hour", "hours")}";
					return true;
				}
				else if (feeTarget >= Constants.OneDayConfirmationTarget + 1 && feeTarget < Constants.SevenDaysConfirmationTarget) // days
				{
					var days = feeTarget / Constants.OneDayConfirmationTarget;
					result = $"{days} {IfPlural(days, "day", "days")}";
					return true;
				}
				else if (feeTarget == Constants.SevenDaysConfirmationTarget)
				{
					result = "one week";
					return true;
				}
				else
				{
					result = "Invalid";
					return false;
				}
			}
			else
			{
				throw new TypeArgumentException(from, typeof(SmartCoinStatus), nameof(from));
			}
		}

        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            if (fromType == typeof(int) && toType == typeof(string))
            {
                return 100;
            }

            return 0;
        }

		private static string IfPlural(int val, string singular, string plural)
		{
			return val == 1 ? singular : plural;
		}
	}
}
