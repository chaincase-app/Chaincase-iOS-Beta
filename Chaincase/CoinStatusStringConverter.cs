using System;
using System.Globalization;
using Chaincase.Models;
using ReactiveUI;
using WalletWasabi.Exceptions;

namespace Chaincase
{
    
    public class CoinStatusStringConverter : IBindingTypeConverter
    {
        public bool TryConvert(object from, Type toType, object conversionHint, out object result)
        {
            if (from is SmartCoinStatus status)
            {
                result = status switch
                {
                    SmartCoinStatus.Confirmed => "",
                    SmartCoinStatus.Unconfirmed => "",
                    SmartCoinStatus.MixingOnWaitingList => " queued  ",
                    SmartCoinStatus.MixingBanned => " banned  ",
                    SmartCoinStatus.MixingInputRegistration => " registered  ",
                    SmartCoinStatus.MixingConnectionConfirmation => " connection confirmed  ",
                    SmartCoinStatus.MixingOutputRegistration => " output registered  ",
                    SmartCoinStatus.MixingSigning => " signed  ",
                    SmartCoinStatus.SpentAccordingToBackend => " spent  ",
                    SmartCoinStatus.MixingWaitingForConfirmation => " waiting for confirmation  ",
                    _ => ""
                };
                return true;
            }

            result = null;
            return false;
        }

        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            if (fromType == typeof(bool) && toType == typeof(SmartCoinStatus))
            {
                return 100;
            }

            return 0;
        }


    }
}
