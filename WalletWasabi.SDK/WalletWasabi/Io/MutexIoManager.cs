using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WalletWasabi.Crypto;

namespace WalletWasabi.Io
{
	public class MutexIoManager : IoManager
	{
		public MutexIoManager(string filePath) : base(filePath)
		{
			Mutex = new AsyncLock();
		}

		public AsyncLock Mutex { get; }
	}
}
