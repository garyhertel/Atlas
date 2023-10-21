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

	public static List<DateTimeFormat> DateFormats = new()
	{
		new DateTimeFormat(2 * 60, TimeSpan.FromSeconds(1), "H:mm:ss"),
		new DateTimeFormat(24 * 60 * 60, TimeSpan.FromMinutes(1), "H:mm"),
		new DateTimeFormat(3 * 24 * 60 * 60, TimeSpan.FromHours(6), "M/d H:mm"),
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
