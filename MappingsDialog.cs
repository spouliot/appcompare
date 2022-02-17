using Terminal.Gui;

namespace AppCompare;

public class MappingsDialog : Dialog {

	readonly Button map;
	readonly Button cancel;
	readonly TextField tf;
	readonly ComboBox cf;

	public MappingsDialog (string? fileA, string? fileB, List<string> list) : base ("Map Files")
	{
		Label file1label = new ("File A:") {
			X = 1,
			Y = 1,
		};
		Label file2label = new ("File B:") {
			X = 1,
			Y = Pos.Bottom (file1label) + 1,
		};

		cf = new () {
			X = Pos.Right (file1label) + 1,
			Y = fileA is null ? file1label.Y : file2label.Y,
			Width = Dim.Fill () - 1,
			Height = list.Count + 1,
			ReadOnly = true,
			Text = "",
		};
		cf.SetSource (list);
		cf.SelectedItemChanged += SelectionChanged;

		tf = new () {
			X = Pos.Right (file2label) + 1,
			Y = fileB is null ? file1label.Y : file2label.Y,
			Width = Dim.Fill () - 1,
			ReadOnly = true,
			Text = fileA ?? fileB,
		};

		Height = 12;
		Add (file1label, fileA is null ? cf : tf, file2label, fileB is null ? cf : tf);

		map = new ("Map", true);
		map.Clicked += () => {
			FileA = (fileA is null ? cf.Text : tf.Text).ToString ();
			FileB = (fileB is null ? cf.Text : tf.Text).ToString ();
			Application.RequestStop ();
		};

		cancel = new ("Cancel");
		cancel.Clicked += () => {
			FileA = null;
			FileB = null;
			Application.RequestStop ();
		};

		AddButton (map);
		AddButton (cancel);
	}

	void SelectionChanged (ListViewItemEventArgs obj)
	{
		map.Enabled = !string.IsNullOrEmpty (cf.Text.ToString ()) && !string.IsNullOrEmpty (tf.Text.ToString ());
	}

	public string? FileA { get; private set; }
	public string? FileB { get; private set; }
}
