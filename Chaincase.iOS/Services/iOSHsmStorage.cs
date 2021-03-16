using System;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using Foundation;
using LocalAuthentication;
using Security;
using WalletWasabi.Logging;

namespace Chaincase.iOS.Services
{
    // based on Xamarin.Essentials SecureStorage
    // Simulator requires Keychain and a keychain access group for the application's bundle identifier.
    // When deploying to an iOS device this entitlement is not required and should be removed.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "iOS is iOS")]
    public class iOSHsmStorage : IHsmStorage
    {
        // has fallback: a device without a passcode is considered "unlocked"
        public static SecAccessible DefaultAccessible { get; set; } =
           SecAccessible.WhenUnlockedThisDeviceOnly;

        // default to this
        public static SecAccessControl BiometricAccessControl { get; set; } =
            new SecAccessControl(DefaultAccessible,
                SecAccessControlCreateFlags.BiometryCurrentSet
            );

        // fall back to this when biomitry disabled
        public static SecAccessControl PasscodeAccessControl { get; set; } =
            new SecAccessControl(DefaultAccessible,
                SecAccessControlCreateFlags.DevicePasscode
            );

        public Task<string> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var kc = new KeyChain(DefaultAccessible);
            var value = kc.ValueForKey(key);

            return Task.FromResult(value);
        }

        // The always default choose the most secure user is comfy with
        public Task SetAsync(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var context = new LAContext();
            KeyChain kc;
            if (context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out var _))
            {
                kc = new KeyChain(BiometricAccessControl);
            }
            else
            {
                // all devices should have passcodes set
                kc = new KeyChain(PasscodeAccessControl);
            }

            kc.SetValueForKey(value, key);

            return Task.CompletedTask;
        }

        public bool Remove(string key)
        {
            var kc = new KeyChain(DefaultAccessible);
            return kc.Remove(key);
        }
    }


    class KeyChain
    {
        SecAccessible accessible;
        SecAccessControl control;

        internal KeyChain(SecAccessible accessible)
        {
            this.accessible = accessible;
        }

        internal KeyChain(SecAccessControl control)
        {
            this.accessible = control.Accessible;
            this.control = control;
        }

        SecRecord ExistingRecordForKey(string key)
        {
            return new SecRecord(SecKind.GenericPassword)
            {
                Account = key
            };
        }

        internal string ValueForKey(string key)
        {
            using (var record = ExistingRecordForKey(key))
            using (var match = SecKeyChain.QueryAsRecord(record, out var resultCode))
            {
                if (resultCode == SecStatusCode.Success)
                    return NSString.FromData(match.ValueData, NSStringEncoding.UTF8);
                else
                    return null;
            }
        }

        internal void SetValueForKey(string value, string key)
        {
            using (var record = ExistingRecordForKey(key))
            {
                if (string.IsNullOrEmpty(value))
                {
                    if (!string.IsNullOrEmpty(ValueForKey(key)))
                        RemoveRecord(record);

                    return;
                }

                // if the key already exists, remove it
                if (!string.IsNullOrEmpty(ValueForKey(key)))
                    RemoveRecord(record);
            }

            using (var newRecord = CreateRecordForNewKeyValue(key, value))
            {
                var result = SecKeyChain.Add(newRecord);

                switch (result)
                {
                    case SecStatusCode.DuplicateItem:
                        {
                            Logger.LogDebug("Duplicate item found.Attempting to remove and add again.");

                            // try to remove and add again
                            if (Remove(key))
                            {
                                result = SecKeyChain.Add(newRecord);
                                if (result != SecStatusCode.Success)
                                    throw new Exception($"Error adding record: {result}");
                            }
                            else
                            {
                                Logger.LogDebug("Unable to remove key.");
                            }
                        }
                        break;
                    case SecStatusCode.Success:
                        return;
                    default:
                        throw new Exception($"Error adding record: {result}");
                }
            }
        }

        internal bool Remove(string key)
        {
            using (var record = ExistingRecordForKey(key))
            using (var match = SecKeyChain.QueryAsRecord(record, out var resultCode))
            {
                if (resultCode == SecStatusCode.Success)
                {
                    RemoveRecord(record);
                    return true;
                }
            }
            return false;
        }

        SecRecord CreateRecordForNewKeyValue(string key, string value)
        {
            return new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Label = key,
                AccessControl = control,
                ValueData = NSData.FromString(value, NSStringEncoding.UTF8),
            };
        }

        bool RemoveRecord(SecRecord record)
        {
            var result = SecKeyChain.Remove(record);
            if (result != SecStatusCode.Success && result != SecStatusCode.ItemNotFound)
                throw new Exception($"Error removing record: {result}");

            return true;
        }
    }
}
