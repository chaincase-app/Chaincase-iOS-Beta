using System;
using ReactiveUI;
using System.Text;

namespace Chaincase.Converters
{
	public class TimeSpanStringConverter : IBindingTypeConverter
	{
		public bool TryConvert(object from, Type toType, object conversionHint, out object result)
		{
			if (from is TimeSpan ts)
			{
				var builder = new StringBuilder();
				if (ts.Days != 0)
				{
					if (ts.Days == 1)
					{
						builder.Append($"{ts.Days} day ");
					}
					else
					{
						builder.Append($"{ts.Days} days ");
					}
				}
				if (ts.Hours != 0)
				{
					if (ts.Hours == 1)
					{
						builder.Append($"{ts.Hours} hour ");
					}
					else
					{
						builder.Append($"{ts.Hours} hours ");
					}
				}
				if (ts.Minutes != 0)
				{
					if (ts.Minutes == 1)
					{
						builder.Append($"{ts.Minutes} minute ");
					}
					else
					{
						builder.Append($"{ts.Minutes} minutes ");
					}
				}
				if (ts.Seconds != 0)
				{
					if (ts.Seconds == 1)
					{
						builder.Append($"{ts.Seconds} second");
					}
					else
					{
						builder.Append($"{ts.Seconds} seconds");
					}
				}
				result = builder.ToString();
				return true;
			}

			result = null;
			return false;
		}

		public int GetAffinityForObjects(Type fromType, Type toType)
		{
			if (fromType == typeof(TimeSpan) && toType == typeof(string))
			{
				return 100;
			}

			return 0;
		}
	}
}
