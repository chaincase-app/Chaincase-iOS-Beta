using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using NBitcoin;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.TransactionProcessing;
using WalletWasabi.CoinJoin.Client.Clients.Queuing;
using WalletWasabi.Logging;
using WalletWasabi.Wallets;

namespace Chaincase.Common.Services
{
    public static class WalletExtensions
    {
        public static async Task TryUnlock(this Wallet wallet, string password)
        {
            await Task.Run(() => wallet.KeyManager.GetMasterExtKey(password ?? ""));
        }
    }

    public class ChaincaseWalletManager : WalletManager
    {
        private readonly INotificationManager _notificationManager;

        public Wallet CurrentWallet { get; set; }
        public IEnumerable<SmartCoin> SleepingCoins;

        public ChaincaseWalletManager(Network network, WalletDirectories walletDirectories, INotificationManager notificationManager)
            : base(network, walletDirectories)
        {
            _notificationManager = notificationManager;

            OnDequeue += WalletManager_OnDequeue;
            WalletRelevantTransactionProcessed += WalletManager_WalletRelevantTransactionProcessed;
        }

        private void WalletManager_OnDequeue(object? sender, DequeueResult e)
        {
            try
            {
                foreach (var success in e.Successful.Where(x => x.Value.Any()))
                {
                    DequeueReason reason = success.Key;
                    if (reason == DequeueReason.ApplicationExit)
                    {
                        SleepingCoins = success.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex);
            }
        }

        private void WalletManager_WalletRelevantTransactionProcessed(object sender, ProcessedResult e)
        {
            try
            {
                // If there are no news, then don't bother.
                if (!e.IsNews || (sender as Wallet).State != WalletState.Started)
                {
                    return;
                }

                // ToDo
                // Double spent.
                // Anonymity set gained?
                // Received dust

                bool isSpent = e.NewlySpentCoins.Any();
                bool isReceived = e.NewlyReceivedCoins.Any();
                bool isConfirmedReceive = e.NewlyConfirmedReceivedCoins.Any();
                bool isConfirmedSpent = e.NewlyConfirmedReceivedCoins.Any();
                Money miningFee = e.Transaction.Transaction.GetFee(e.SpentCoins.Select(x => x.GetCoin()).ToArray());
                if (isReceived || isSpent)
                {
                    Money receivedSum = e.NewlyReceivedCoins.Sum(x => x.Amount);
                    Money spentSum = e.NewlySpentCoins.Sum(x => x.Amount);
                    Money incoming = receivedSum - spentSum;
                    Money receiveSpentDiff = incoming.Abs();
                    string amountString = receiveSpentDiff.ToString(false, true);

                    if (e.Transaction.Transaction.IsCoinBase)
                    {
                        _notificationManager.NotifyAndLog($"{amountString} BTC", "Mined", NotificationType.Success, e);
                    }
                    else if (isSpent && receiveSpentDiff == miningFee)
                    {
                        _notificationManager.NotifyAndLog($"Mining Fee: {amountString} BTC", "Self Spend", NotificationType.Information, e);
                    }
                    else if (isSpent && receiveSpentDiff.Almost(Money.Zero, Money.Coins(0.01m)) && e.IsLikelyOwnCoinJoin)
                    {
                        _notificationManager.NotifyAndLog($"CoinJoin Completed!", "", NotificationType.Success, e);
                    }
                    else if (incoming > Money.Zero)
                    {
                        if (e.Transaction.IsRBF && e.Transaction.IsReplacement)
                        {
                            _notificationManager.NotifyAndLog($"{amountString} BTC", "Received Replaceable Replacement Transaction", NotificationType.Information, e);
                        }
                        else if (e.Transaction.IsRBF)
                        {
                            _notificationManager.NotifyAndLog($"{amountString} BTC", "Received Replaceable Transaction", NotificationType.Success, e);
                        }
                        else if (e.Transaction.IsReplacement)
                        {
                            _notificationManager.NotifyAndLog($"{amountString} BTC", "Received Replacement Transaction", NotificationType.Information, e);
                        }
                        else
                        {
                            _notificationManager.NotifyAndLog($"{amountString} BTC", "Received", NotificationType.Success, e);
                        }
                    }
                    else if (incoming < Money.Zero)
                    {
                        _notificationManager.NotifyAndLog($"{amountString} BTC", "Sent", NotificationType.Information, e);
                    }
                }
                else if (isConfirmedReceive || isConfirmedSpent)
                {
                    Money receivedSum = e.ReceivedCoins.Sum(x => x.Amount);
                    Money spentSum = e.SpentCoins.Sum(x => x.Amount);
                    Money incoming = receivedSum - spentSum;
                    Money receiveSpentDiff = incoming.Abs();
                    string amountString = receiveSpentDiff.ToString(false, true);

                    if (isConfirmedSpent && receiveSpentDiff == miningFee)
                    {
                        _notificationManager.NotifyAndLog($"Mining Fee: {amountString} BTC", "Self Spend Confirmed", NotificationType.Information, e);
                    }
                    else if (isConfirmedSpent && e.IsLikelyOwnCoinJoin)
                    {
                        _notificationManager.NotifyAndLog($"CoinJoin Confirmed!", "", NotificationType.Information, e);
                    }
                    else if (incoming > Money.Zero)
                    {
                        _notificationManager.NotifyAndLog($"{amountString} BTC", "Receive Confirmed", NotificationType.Information, e);
                    }
                    else if (incoming < Money.Zero)
                    {
                        _notificationManager.NotifyAndLog($"{amountString} BTC", "Send Confirmed", NotificationType.Information, e);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex);
            }
        }

        public void SetDefaultWallet()
        {
            CurrentWallet = GetWalletByName(Network.ToString());
        }

        public bool HasDefaultWalletFile()
        {
            // this is kinda codesmell biz logic but it doesn't make sense for a full VM here
            var walletName = Network.ToString();
            (string walletFullPath, _) = WalletDirectories.GetWalletFilePaths(walletName);
            return File.Exists(walletFullPath);
        }
    }
}
