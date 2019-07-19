using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WalletWasabi.KeyManagement;
using Wasabi.Controllers;
using Wasabi.Navigation;
using Xamarin.Forms;

namespace Wasabi.ViewModels
{
	public class ReceiveViewModel : ViewModelBase
	{
		private ObservableCollection<AddressViewModel> _addresses;
		private AddressViewModel _selectedAddress;
		private string _label;
		public ObservableCollection<AddressViewModel> Addresses
		{
			get => _addresses;
			set
			{
				_addresses = value;
				RaisePropertyChanged(() => _addresses);
			}
		}
		public AddressViewModel SelectedAddress
		{
			get => _selectedAddress;
			set
			{
				_selectedAddress = value;
				RaisePropertyChanged(() => _selectedAddress);
			}
		}
		public string Label
		{
			get => _label;
			set
			{
				_label = value;
				RaisePropertyChanged(() => _label);
			}
		}

		public ICommand BackCommand { get; }

		public ICommand GenerateCommand { get; }

		public ReceiveViewModel(INavigationService navigationService) : base(navigationService)
		{
			WalletController.LoadWalletAsync();
			_addresses = new ObservableCollection<AddressViewModel>();

			InitializeAddresses();

			BackCommand = new Command(() => navigationService.GoBack());

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

					var newAddress = new AddressViewModel(_navigationService, newKey);

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
				_addresses.Add(new AddressViewModel(_navigationService, key));
			}
		}
	}
}
