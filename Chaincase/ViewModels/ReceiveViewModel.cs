using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;
using WalletWasabi.KeyManagement;
using Xamarin.Forms;

namespace Chaincase.ViewModels
{
	public class ReceiveViewModel : ViewModelBase
	{
		private ObservableCollection<AddressViewModel> _addresses;
		private AddressViewModel _selectedAddress;
		private string _label;
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
		public string Label
		{
			get => _label;
			set => this.RaiseAndSetIfChanged(ref _label, value);
		}

		public ICommand GenerateCommand { get; }

		public ReceiveViewModel(IScreen hostScreen) : base(hostScreen)
		{
			_addresses = new ObservableCollection<AddressViewModel>();

			InitializeAddresses();

			GenerateCommand = new Command(() =>
			{
				Label = Label.Trim(',', ' ').Trim();
				// Require label in next iteration

				Device.BeginInvokeOnMainThread(() =>
				{
					var label = Label;
					HdPubKey newKey = Global.WalletService.GetReceiveKey(label, Addresses.Select(x => x.Model).Take(7)); // Never touch the first 7 keys.

					AddressViewModel found = Addresses.FirstOrDefault(x => x.Model == newKey);
					if (found != default)
					{
						Addresses.Remove(found);
					}

					var newAddress = new AddressViewModel(_hostScreen, newKey);

					Addresses.Insert(0, newAddress);

					SelectedAddress = newAddress;

					Label = "";
				});
			});
		}

		private void InitializeAddresses()
		{
			_addresses?.Clear();

			foreach (HdPubKey key in Global.WalletService.KeyManager.GetKeys(x =>
																		x.HasLabel
																		&& !x.IsInternal
																		&& x.KeyState == KeyState.Clean)
																	.Reverse())
			{
				_addresses.Add(new AddressViewModel(_hostScreen, key));
			}
		}
	}
}
