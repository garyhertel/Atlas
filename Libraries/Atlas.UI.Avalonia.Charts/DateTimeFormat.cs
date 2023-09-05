using Atlas.Extensions;

namespace Atlas.UI.Avalonia.Charts;

public class DateTimeFormat
{
	public double Maximum { get; set; }
	public TimeSpan StepSize { get; set; }
	public string TextFormat { get; set; }

	public DateTimeFormat(double maximum, TimeSpan stepSize, string textFormat)
	{
		Maximum = maximum;
		StepSize = stepSize;
		TextFormat = textFormat;
	}

	// todo: centralize and add units
	public static string ValueFormatter(double d)
	{
		double ad = Math.Abs(d);
		string prefix = "{0:#,0.#} ";
		if (ad >= 1E12)
		{
			return string.Format(prefix + "T", d / 1E12);
		}
		else if (ad >= 1E9)
		{
			return string.Format(prefix + "G", d / 1E9);
		}
		else if (ad >= 1E6)
		{
			return string.Format(prefix + "M", d / 1E6);
		}
		else if (ad >= 1E3)
		{
			return string.Format(prefix + "K", d / 1E3);
		}
		else
		{
			return d.Formatted()!;
		}
	}

	public static List<DateTimeFormat> DateFormats = new()
	{
		new DateTimeFormat(2 * 60, TimeSpan.FromSeconds(1), "H:mm:ss"),
		new DateTimeFormat(24 * 60 * 60, TimeSpan.FromMinutes(1), "H:mm"),
		new DateTimeFormat(3 * 24 * 60 * 60, TimeSpan.FromMinutes(1), "M/d H:mm"),
		new DateTimeFormat(6 * 30 * 24 * 60 * 60, TimeSpan.FromDays(1), "M/d"),
		new DateTimeFormat(1000.0 * 12 * 30 * 24 * 60 * 60, TimeSpan.FromDays(1), "yyyy-M-d"),
	};

	public static DateTimeFormat? GetDateTimeFormat(TimeSpan duration)
	{
		return GetDateTimeFormat(duration.TotalSeconds);
	}

	public static DateTimeFormat? GetDateTimeFormat(double durationSeconds)
	{
		return DateFormats.FirstOrDefault(format => durationSeconds < format.Maximum);
	}
}
