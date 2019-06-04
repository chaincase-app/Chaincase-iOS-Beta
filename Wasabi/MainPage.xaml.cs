using NBitcoin;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Wasabi
{
	public partial class MainPage : ContentPage
	{
		string _fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "notes.txt");
		BlockCypherClient bcc = new BlockCypherClient(Network.Main);

		public MainPage()
		{
			InitializeComponent();

			if (File.Exists(_fileName))
			{
				editor.Text = File.ReadAllText(_fileName);
				bcc = new BlockCypherClient(Network.Main);
			}
		}

		async void OnSaveButtonClickedAsync(object sender, EventArgs e)
		{
			string generalInfo = await bcc.GetGeneralInformationAsync(CancellationToken.None);
			editor.Text = generalInfo;
			File.WriteAllText(_fileName, generalInfo);
		}

		void OnDeleteButtonClicked(object sender, EventArgs e)
		{
			if (File.Exists(_fileName))
			{
				File.Delete(_fileName);
			}
			editor.Text = string.Empty;
		}
	}
}
