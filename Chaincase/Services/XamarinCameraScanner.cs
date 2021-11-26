using System.Threading.Tasks;
using Chaincase.Common.Contracts;
using ZXing.Mobile;

namespace Chaincase.Services
{
	public class XamarinCameraScanner :ICameraScanner
	{
		private readonly MobileBarcodeScanner _innerScanner;

		public XamarinCameraScanner()
		{
			_innerScanner =  new ZXing.Mobile.MobileBarcodeScanner();	
		}
		public async Task<string> Scan()
		{
			var result =  await _innerScanner.Scan();
			return result?.Text;
		}
	}
}
