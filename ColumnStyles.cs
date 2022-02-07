using Terminal.Gui;

namespace AppCompare;

public static class ColumnStyles {

	public static readonly TableView.ColumnStyle FileInfoLengthNumber = new () {
		Alignment = TextAlignment.Right,
		RepresentationGetter = (object o) => {
			(FileInfo? file, long length) = ((FileInfo?, long)) o;
			if ((length == 0) || (file is not null && file.Attributes.HasFlag (FileAttributes.ReparsePoint)))
				return "";
			return length.ToString ("N0");
		},
	};

	public static readonly TableView.ColumnStyle LongNumber = new () {
		Alignment = TextAlignment.Right,
		RepresentationGetter = (object o) => {
			var value = (long) o;
			return value == 0 ? "" : value.ToString ("N0");
		},
	};

	public static readonly TableView.ColumnStyle Percentage = new () {
		Alignment = TextAlignment.Right,
		RepresentationGetter = (object o) => {
			var value = (double) o;
			return Double.IsNaN (value) ? "" : value.ToString ("P1");
		},
	};
}
