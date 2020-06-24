
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Foundation;
using Security;

using Xamarin.Forms;

[assembly: Dependency(typeof(Chaincase.iOS.HsmStorage))]
namespace Chaincase.iOS
{
    // based on Xamarin.Essentials SecureStorage
    public class HsmStorage : IHsmStorage
    {
        public static SecAccessible DefaultAccessible { get; set; } =
           SecAccessible.WhenUnlockedThisDeviceOnly;

         public static Task SetAsync(string key, string value, SecAccessible accessible)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var kc = new KeyChain(accessible);
            kc.SetValueForKey(value, key);

            return Task.CompletedTask;
        }

        static Task<string> PlatformGetAsync(string key)
        {
            var kc = new KeyChain(DefaultAccessible);
            var value = kc.ValueForKey(key);

            return Task.FromResult(value);
        }

        static Task PlatformSetAsync(string key, string data) =>
            SetAsync(key, data, DefaultAccessible);

        static bool PlatformRemove(string key)
        {
            var kc = new KeyChain(DefaultAccessible);

            return kc.Remove(key);
        }
    }

    //    public static HsmStorage()
    //    {
    //        var access = new SecAccessControl(SecAccessible.WhenUnlockedThisDeviceOnly,
    //                                          SecAccessControlCreateFlags.ApplicationPassword);

    //        var kc = new KeyChain(SecAccessible.WhenUnlockedThisDeviceOnly);
    //        kc.SetValueForKey(value, key, Alias);

    //        var query = new Sec
    //        //let query: [String: Any] = [kSecClass as String: kSecClassInternetPassword,
    //        //                kSecAttrAccount as String: account,
    //        //                kSecAttrServer as String: server,
    //        //                kSecAttrAccessControl as String: access as Any,
    //        //                kSecUseAuthenticationContext as String: context,
    //        //                kSecValueData as String: password]
    //            //(nil, // Use the default allocator.
    //            //                             SecAccessible.WhenUnlockedThisDeviceOnly,
    //            //                             .userPresence,
    //            //                             nil) // Ignore any error.
    //    }

    //}

    class KeyChain
    {
        //SecAccessControl accessControl;

        //internal KeyChain(SecAccessible accessible, SecAccessControlCreateFlags flags) =>
        //    this.accessControl = new SecAccessControl(accessible, flags);

        SecAccessible accessible;

        internal KeyChain(SecAccessible accessible) =>
            this.accessible = accessible;

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
                            Debug.WriteLine("Duplicate item found. Attempting to remove and add again.");

                            // try to remove and add again
                            if (Remove(key))
                            {
                                result = SecKeyChain.Add(newRecord);
                                if (result != SecStatusCode.Success)
                                    throw new Exception($"Error adding record: {result}");
                            }
                            else
                            {
                                Debug.WriteLine("Unable to remove key.");
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
                Accessible = accessible,
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
