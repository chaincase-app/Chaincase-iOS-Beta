using System;
using Chaincase.ViewModels;
using ReactiveUI;
using ReactiveUI.XamForms;
using System.Reactive.Disposables;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Linq;

namespace Chaincase.Views
{
	public partial class VerifyMnemonicPage : ReactiveContentPage<VerifyMnemonicViewModel>
	{

		private int failedAttempts = 0;

		public VerifyMnemonicPage()
		{
			InitializeComponent();
			this.WhenActivated(d =>
			{
				InstructionLabel.Text = $"Select word {ViewModel.SeedWords.IndexOf(ViewModel.WordToVerify)+1}";

				var buttons = new Button[] { ButtonA, ButtonB, ButtonC, ButtonD, ButtonE };
				string[] shuffledWords = new string[ViewModel.SeedWords.Count()];
				ViewModel.SeedWords.CopyTo(shuffledWords);
				Shuffle(shuffledWords);
				string[] buttonText = new string[buttons.Length];
				Array.Copy(shuffledWords, buttonText, buttons.Length);
				buttonText[0] = ViewModel.WordToVerify;
				Shuffle(buttonText);
				for (int i = 0; i < 5; i++)
				{
					buttons[i].Text = buttonText[i];
					buttons[i].Clicked += async (sender, args) =>
					{
						Button clicked = (Button)sender;
						if(clicked.Text == ViewModel.WordToVerify)
                        {
							ViewModel.VerifiedCommand.Execute();
                        }
                        else if (failedAttempts++ > 1 || await DisplayAlert("Wrong Word", "Are you sure you have your words?", "See Words", "Try Again"))
                        {
							ViewModel.FailedCommand.Execute();
                        }
					};
				}
			});
		}

        // Just a game of "have u written this down?" not CSPRNG
		private static Random rng = new Random();

		public static void Shuffle<T>(IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}
}
