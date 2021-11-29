using System.Threading.Tasks;
using BlazorBarcodeScanner.ZXing.JS;
using Chaincase.Common.Contracts;

namespace Chaincase.SSB
{
	public class WebCameraScanner : ICameraScanner
	{
		private ZXingBlazorCameraScanner _reader;
		private TaskCompletionSource<string> tcs;

		public void SetRef(ZXingBlazorCameraScanner barcodeReader)
		{
			_reader = barcodeReader;
		}

		public Task<string> Scan()
		{
			tcs = new TaskCompletionSource<string>();
			_reader.ShowNow();
			return tcs.Task;
		}

		public void OnBarcodeReceived(BarcodeReceivedEventArgs barcodeReceivedEventArgs)
		{
			tcs?.SetResult(barcodeReceivedEventArgs.BarcodeText);
		}
	}
}
