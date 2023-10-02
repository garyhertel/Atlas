namespace Atlas.Extensions;

public static class NumberExtensions
{
	public static string FormattedDecimal(this double d)
	{
		return d.ToString("#,0.#");
	}

	public static string FormattedShortDecimal(this double d)
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
}
