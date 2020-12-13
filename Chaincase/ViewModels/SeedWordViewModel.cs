using Chaincase.Common.Models;
using Chaincase.Navigation;
using NBitcoin;
using ReactiveUI;
using Splat;
using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.CoinJoin.Client.Rounds;
using WalletWasabi.CoinJoin.Common.Models;
using WalletWasabi.Models;

namespace Chaincase.ViewModels
{
	public class SeedWordViewModel : ViewModelBase, IDisposable
	{

        public CompositeDisposable Disposables { get; set; }

        public string Word;
        public int Position;

        public SeedWordViewModel(string word, int i)
            : base(Locator.Current.GetService<IViewStackService>())
		{
            Word = $"{i}. {word}";
            Position = i;

            Disposables = new CompositeDisposable();
        }

        public CompositeDisposable GetDisposables() => Disposables;

        #region IDisposable Support

        private volatile bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Disposables?.Dispose();
                }

                Disposables = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}
