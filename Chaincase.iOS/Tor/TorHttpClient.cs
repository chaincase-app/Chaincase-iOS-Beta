using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using TorFramework;
/*
namespace Chaincase.iOS.Tor
{
    public class TorHttpClient : ITorHttpClient, IDisposable
    {
		private static DateTimeOffset? TorDoesntWorkSinceBacking = null;

		public static DateTimeOffset? TorDoesntWorkSince
		{
			get => TorDoesntWorkSinceBacking;
			private set
			{
				if (value != TorDoesntWorkSinceBacking)
				{
					TorDoesntWorkSinceBacking = value;
					if (value is null)
					{
						LatestTorException = null;
					}
				}
			}
		}

		public static Exception LatestTorException { get; private set; } = null;

		public Uri DestinationUri => DestinationUriAction();
		public Func<Uri> DestinationUriAction { get; private set; }
		public EndPoint TorSocks5EndPoint { get; private set; }
		public bool IsTorUsed => TorSocks5EndPoint != null;

		public TORController TorController { get; private set; }


		public TorHttpClient(Uri baseUri, EndPoint torSocks5EndPoint, bool isolateStream = false)
		{
			// TODO baseUri = Guard.NotNull(nameof(baseUri), baseUri);
			Create(torSocks5EndPoint, isolateStream, () => baseUri);
		}

		public TorHttpClient(Func<Uri> baseUriAction, EndPoint torSocks5EndPoint, bool isolateStream = false)
		{
			Create(torSocks5EndPoint, isolateStream, baseUriAction);
		}

		private void Create(EndPoint torSocks5EndPoint, bool isolateStream, Func<Uri> baseUriAction)
		{
			//DestinationUriAction = Guard.NotNull(nameof(baseUriAction), baseUriAction);
            TorSocks5EndPoint = DestinationUri.IsLoopback ? null : torSocks5EndPoint;
			TorController = null;
		}

		/// <remarks>
		/// Throws OperationCancelledException if <paramref name="cancel"/> is set.
		/// </remarks>
		public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel = default)
		{
			Guard.NotNull(nameof(request), request);

			// https://tools.ietf.org/html/rfc7230#section-2.7.1
			// A sender MUST NOT generate an "http" URI with an empty host identifier.
			var host = Guard.NotNullOrEmptyOrWhitespace($"{nameof(request)}.{nameof(request.RequestUri)}.{nameof(request.RequestUri.DnsSafeHost)}", request.RequestUri.DnsSafeHost, trim: true);

			// https://tools.ietf.org/html/rfc7230#section-2.6
			// Intermediaries that process HTTP messages (i.e., all intermediaries
			// other than those acting as tunnels) MUST send their own HTTP - version
			// in forwarded messages.
			request.Version = new Version("1.1"); // TODO HttpProtocol.HTTP11.Version;

			if (TorController != null && !TorController.Connected)
			{
				TorController?.Dispose();
				TorController = null;
			}

			if (TorController is null || !TorController.Connected)
			{
				TorController = new TORController(TorSocks5EndPoint);

				var (success, error) = TorController.AuthenticateWithDataAsync(Cookie).Result;
				await TorController.ConnectAsync().ConfigureAwait(false);
				await TorController.HandshakeAsync(IsolateStream).ConfigureAwait(false);
				await TorController.ConnectToDestinationAsync(host, request.RequestUri.Port).ConfigureAwait(false);

				Stream stream = TorController.TcpClient.GetStream();
				if (request.RequestUri.Scheme == "https")
				{
					SslStream sslStream;
					// On Linux and OSX ignore certificate, because of a .NET Core bug
					// This is a security vulnerability, has to be fixed as soon as the bug get fixed
					// Details:
					// https://github.com/dotnet/corefx/issues/21761
					// https://github.com/nopara73/DotNetTor/issues/4
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						sslStream = new SslStream(
							stream,
							leaveInnerStreamOpen: true);
					}
					else
					{
						sslStream = new SslStream(
							stream,
							leaveInnerStreamOpen: true,
							userCertificateValidationCallback: (a, b, c, d) => true);
					}

					await sslStream
						.AuthenticateAsClientAsync(
							host,
							new X509CertificateCollection(),
							SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
							checkCertificateRevocation: true).ConfigureAwait(false);
					stream = sslStream;
				}

				TorController.Stream = stream;
			}
			cancel.ThrowIfCancellationRequested();

			// https://tools.ietf.org/html/rfc7230#section-3.3.2
			// A user agent SHOULD send a Content - Length in a request message when
			// no Transfer-Encoding is sent and the request method defines a meaning
			// for an enclosed payload body.For example, a Content - Length header
			// field is normally sent in a POST request even when the value is 0
			// (indicating an empty payload body).A user agent SHOULD NOT send a
			// Content - Length header field when the request message does not contain
			// a payload body and the method semantics do not anticipate such a
			// body.
			if (request.Method == HttpMethod.Post)
			{
				if (request.Headers.TransferEncoding.Count == 0)
				{
					if (request.Content is null)
					{
						request.Content = new ByteArrayContent(Array.Empty<byte>()); // dummy empty content
						request.Content.Headers.ContentLength = 0;
					}
					else
					{
						if (request.Content.Headers.ContentLength is null)
						{
							request.Content.Headers.ContentLength = (await request.Content.ReadAsStringAsync().ConfigureAwait(false)).Length;
						}
					}
				}
			}

			var requestString = await request.ToHttpStringAsync().ConfigureAwait(false);

			var bytes = Encoding.UTF8.GetBytes(requestString);

			await TorController.Stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
			await TorController.Stream.FlushAsync().ConfigureAwait(false);
			using var httpResponseMessage = new HttpResponseMessage();
			return await HttpResponseMessageExtensions.CreateNewAsync(TorController.Stream, request.Method).ConfigureAwait(false);
		}

		#region IDisposable Support

		private volatile bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					TorController?.Dispose();
				}

				_disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        public System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativeUri, HttpContent content = null, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        #endregion IDisposable Support
    }


	public static class Guard
	{
		public static T NotNull<T>(string parameterName, T value)
		{
			AssertCorrectParameterName(parameterName);

			if (value == null)
			{
				throw new ArgumentNullException(parameterName, "Parameter cannot be null.");
			}

			return value;
		}

		private static void AssertCorrectParameterName(string parameterName)
		{
			if (parameterName is null)
			{
				throw new ArgumentNullException(nameof(parameterName), "Parameter cannot be null.");
			}

			if (parameterName.Length == 0)
			{
				throw new ArgumentException("Parameter cannot be empty.", nameof(parameterName));
			}

			if (parameterName.Trim().Length == 0)
			{
				throw new ArgumentException("Parameter cannot be whitespace.", nameof(parameterName));
			}
		}

		public static T Same<T>(string parameterName, T expected, T actual)
		{
			AssertCorrectParameterName(parameterName);
			NotNull(nameof(expected), expected);

			if (!expected.Equals(actual))
			{
				throw new ArgumentException($"Parameter must be {expected}. Actual: {actual}.", parameterName);
			}

			return actual;
		}

		public static IEnumerable<T> NotNullOrEmpty<T>(string parameterName, IEnumerable<T> value)
		{
			NotNull(parameterName, value);

			if (!value.Any())
			{
				throw new ArgumentException("Parameter cannot be empty.", parameterName);
			}

			return value;
		}

		public static T[] NotNullOrEmpty<T>(string parameterName, T[] value)
		{
			NotNull(parameterName, value);

			if (!value.Any())
			{
				throw new ArgumentException("Parameter cannot be empty.", parameterName);
			}

			return value;
		}

		public static IDictionary<TKey, TValue> NotNullOrEmpty<TKey, TValue>(string parameterName, IDictionary<TKey, TValue> value)
		{
			NotNull(parameterName, value);
			if (!value.Any())
			{
				throw new ArgumentException("Parameter cannot be empty.", parameterName);
			}
			return value;
		}

		public static Dictionary<TKey, TValue> NotNullOrEmpty<TKey, TValue>(string parameterName, Dictionary<TKey, TValue> value)
		{
			NotNull(parameterName, value);
			if (!value.Any())
			{
				throw new ArgumentException("Parameter cannot be empty.", parameterName);
			}
			return value;
		}

		public static string NotNullOrEmptyOrWhitespace(string parameterName, string value, bool trim = false)
		{
			NotNullOrEmpty(parameterName, value);

			string trimmedValue = value.Trim();
			if (trimmedValue.Length == 0)
			{
				throw new ArgumentException("Parameter cannot be whitespace.", parameterName);
			}

			if (trim)
			{
				return trimmedValue;
			}
			else
			{
				return value;
			}
		}

		public static T MinimumAndNotNull<T>(string parameterName, T value, T smallest) where T : IComparable
		{
			NotNull(parameterName, value);

			if (value.CompareTo(smallest) < 0)
			{
				throw new ArgumentOutOfRangeException(parameterName, value, $"Parameter cannot be less than {smallest}.");
			}

			return value;
		}

		public static T MaximumAndNotNull<T>(string parameterName, T value, T greatest) where T : IComparable
		{
			NotNull(parameterName, value);

			if (value.CompareTo(greatest) > 0)
			{
				throw new ArgumentOutOfRangeException(parameterName, value, $"Parameter cannot be greater than {greatest}.");
			}

			return value;
		}

		public static T InRangeAndNotNull<T>(string parameterName, T value, T smallest, T greatest) where T : IComparable
		{
			NotNull(parameterName, value);

			if (value.CompareTo(smallest) < 0)
			{
				throw new ArgumentOutOfRangeException(parameterName, value, $"Parameter cannot be less than {smallest}.");
			}

			if (value.CompareTo(greatest) > 0)
			{
				throw new ArgumentOutOfRangeException(parameterName, value, $"Parameter cannot be greater than {greatest}.");
			}

			return value;
		}

		/// <summary>
		/// Corrects the string:
		/// If the string is null, it'll be empty.
		/// Trims the string.
		/// </summary>
		public static string Correct(string str)
		{
			return string.IsNullOrWhiteSpace(str)
				? string.Empty
				: str.Trim();
		}
	}
}
*/