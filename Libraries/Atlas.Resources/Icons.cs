using Atlas.Core;
using System.Reflection;

namespace Atlas.Resources;

public static class Icons
{
	public const string Logo = "Logo3.ico";

	public const string Pin = "placeholder.png";

	//public const string Add = "add.png";
	//public const string Delete = "cancel.png"; // red too bright

	//public const string Back = "left-arrow-blue.png";
	//public const string Forward = "right-arrow-blue.png";

	//public const string Search = "search.png";
	public const string Search16 = "search_right_light_16.png";
	public const string ClearSearch = "clear_search.png";

	public const string Info1 = "info_24_759eeb.png";
	
	public const string Info20 = "info_20_759eeb.png";

	// public const string Info2 = "info_24_c8c2f9.png";

	public const string BlankDocument = "blank-document.png";
	public const string Save = "save-file-option.png";
	public const string OpenFolder = "OpenFolder.png";

	public const string Browser = "internet.png";

	public const string Unlock = "unlock.png";
	public const string Password = "password.png";
	public const string Enter = "enter.png";

	public const string PadNote = "padnote.png";
	public const string Paste = "paste_16.png";
	public const string Eraser = "eraser.png";

	//public const string Refresh = "refresh.png";
	public const string Stats = "stats.png";

	public const string List1 = "list1.png";
	public const string List2 = "list2.png";
	public const string DeleteList = "delete_list.png";

	//public const string Link = "link.png";
	public const string Bookmark = "bookmark.png";
	//public const string Import = "import.png";

	public const string Screenshot = "screenshot.png";

	public static class Svg
	{
		public const string Add = "add.svg";
		public const string Delete = "cancel.svg";

		public const string Back = "leftarrow.svg";
		public const string Forward = "rightarrow.svg";

		public const string Search = "search.svg";

		public const string PadNote = "padnote.svg";

		public const string Refresh = "refresh.svg";

		public const string Link = "link.svg";
		public const string Import = "import.svg";

		public const string Screenshot = "screenshot.svg";
	}

	public class Streams : NamedItemCollection<Streams, Stream>
	{
		public static Stream Logo => Get(Icons.Logo, "Logo");

		public static Stream Pin => Get(Icons.Pin);
		//public static Stream Add => Get(Icons.Add);
		public static Stream Add => GetSvg(Svg.Add);
		//public static Stream Delete => Get(Icons.Delete);
		public static Stream Delete => GetSvg(Svg.Delete);

		//public static Stream Back => Get(Icons.Back);
		public static Stream Back => GetSvg(Svg.Back);
		//public static Stream Forward => Get(Icons.Forward);
		public static Stream Forward => GetSvg(Svg.Forward);

		public static Stream ClearSearch => Get(Icons.ClearSearch);
		//public static Stream Search => Get(Icons.Search);
		public static Stream Search => GetSvg(Svg.Search);
		public static Stream Search16 => Get(Icons.Search16);

		public static Stream Info => Get(Icons.Info1);
		public static Stream Info20 => Get(Icons.Info20);

		public static Stream BlankDocument => Get(Icons.BlankDocument);
		public static Stream Save => Get(Icons.Save);
		public static Stream OpenFolder => Get(Icons.OpenFolder);

		public static Stream Browser => Get(Icons.Browser);

		public static Stream Unlock => Get(Icons.Unlock);
		public static Stream Password => Get(Icons.Password);
		public static Stream Enter => Get(Icons.Enter);

		public static Stream PadNote => Get(Icons.PadNote);
		public static Stream PadNoteSvg => GetSvg(Svg.PadNote);
		public static Stream Paste => Get(Icons.Paste);
		public static Stream Eraser => Get(Icons.Eraser);

		//public static Stream Refresh => Get(Icons.Refresh);
		public static Stream Refresh => GetSvg(Svg.Refresh);
		public static Stream Stats => Get(Icons.Stats);

		public static Stream List1 => Get(Icons.List1);
		public static Stream List2 => Get(Icons.List2);
		public static Stream DeleteList => Get(Icons.DeleteList);

		//public static Stream Link => Get(Icons.Link);
		public static Stream Link => GetSvg(Svg.Link);
		public static Stream Bookmark => Get(Icons.Bookmark);
		//public static Stream Import => Get(Icons.Import);
		public static Stream Import => GetSvg(Svg.Import);
		//public static Stream Screenshot => Get(Icons.Screenshot);
		public static Stream Screenshot => GetSvg(Svg.Screenshot);

		public static Stream Get(string resourceName, string resourceType = "png")
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			return assembly.GetManifestResourceStream("Atlas.Resources.Icons." + resourceType + "." + resourceName)!;
		}

		public static Stream GetSvg(string resourceName)
		{
			Stream stream = Get(resourceName, "svg");
			return stream;
		}
	}
}
