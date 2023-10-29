namespace Atlas.UI.Avalonia.Charts;

public class DateTimeFormat
{
	public TimeSpan Maximum { get; set; }
	public string TextFormat { get; set; }

	public DateTimeFormat(TimeSpan maximum, string textFormat)
	{
		Maximum = maximum;
		TextFormat = textFormat;
	}

	public static List<DateTimeFormat> DateFormats = new()
	{
		new(TimeSpan.FromMinutes(3), "H:mm:ss"),
		new(TimeSpan.FromDays(1), "H:mm"),
		new(TimeSpan.FromDays(3), "M/d H:mm"),
		new(TimeSpan.FromDays(6 * 30), "M/d"),
		new(TimeSpan.FromDays(1000.0 * 12 * 30), "yyyy-M-d"),
	};

	public static DateTimeFormat? GetDateTimeFormat(TimeSpan duration)
	{
		return DateFormats.FirstOrDefault(format => duration < format.Maximum);
	}
}
