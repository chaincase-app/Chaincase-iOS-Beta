using System.Windows.Input;
using Xamarin.Forms;
using Chaincase.Navigation;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive;
using System;
using System.Reactive.Linq;
using Splat;

namespace Chaincase.ViewModels
{
	public class MnemonicViewModel : ViewModelBase
	{
		private string _mnemonicString;
		public string MnemonicString
		{
			get => _mnemonicString;
			set => this.RaiseAndSetIfChanged(ref _mnemonicString, value);
		}

		public ReactiveCommand<Unit, Unit> AcceptCommand;

		public MnemonicViewModel(string mnemonicString)
            : base(Locator.Current.GetService<IViewStackService>())
		{
			MnemonicString = mnemonicString;
			AcceptCommand = ReactiveCommand.CreateFromObservable(() =>
			{
				ViewStackService.PushPage(new VerifyMnemonicViewModel(MnemonicString)).Subscribe();
				return Observable.Return(Unit.Default);
			});
		}
	}
}
