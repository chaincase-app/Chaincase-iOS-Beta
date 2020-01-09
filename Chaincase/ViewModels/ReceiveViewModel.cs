using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.Keys;
using Xamarin.Forms;

namespace Chaincase.ViewModels
{
	public class ReceiveViewModel : ViewModelBase
	{
		private ObservableCollection<AddressViewModel> _addresses;
		private AddressViewModel _selectedAddress;
		private string _memo;
		public ObservableCollection<AddressViewModel> Addresses
		{
			get => _addresses;
			set => this.RaiseAndSetIfChanged(ref _addresses, value);
		}
		public AddressViewModel SelectedAddress
		{
			get => _selectedAddress;
			set => this.RaiseAndSetIfChanged(ref _selectedAddress, value);
		}
		public string Memo
		{
			get => _memo;
			set => this.RaiseAndSetIfChanged(ref _memo, value);
		}

		public ICommand GenerateCommand { get; }

		public ReceiveViewModel(IScreen hostScreen) : base(hostScreen)
		{
			_addresses = new ObservableCollection<AddressViewModel>();

			InitializeAddresses();

			GenerateCommand = new Command(() =>
			{

                var memo = new SmartLabel();
				// Require label in next iteration

				Device.BeginInvokeOnMainThread(() =>
				{
                    var newKey = Global.WalletService.KeyManager.GetNextReceiveKey(Memo, out bool minGapLimitIncreased);
                    if (minGapLimitIncreased)
                    {
                        int minGapLimit = Global.WalletService.KeyManager.MinGapLimit.Value;
                        int prevMinGapLimit = minGapLimit - 1;
                    }

                    var newAddress = new AddressViewModel(_hostScreen, newKey);
                    Addresses.Insert(0, newAddress);
                    SelectedAddress = newAddress;
                    Memo = null;
				});
			});
		}

		private void InitializeAddresses()
		{
			_addresses?.Clear();

			foreach (HdPubKey key in Global.WalletService.KeyManager.GetKeys(x =>
																		!x.Label.IsEmpty
                                                                        && !x.IsInternal
																		&& x.KeyState == KeyState.Clean)
																	.Reverse())
			{
				_addresses.Add(new AddressViewModel(_hostScreen, key));
			}
		}
	}
}
