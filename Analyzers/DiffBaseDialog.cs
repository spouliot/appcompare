using Terminal.Gui;

namespace AppCompare.Analyzers;

public abstract class DiffBaseDialog : Dialog {

	public enum Output {
		Open,
		Gist,
	}

	protected readonly Button open;
	protected readonly Button gist;
	protected readonly Button cancel;

	public DiffBaseDialog (string name, FileInfo fa, FileInfo fb) : base (name)
	{
		FileInfoA = fa;
		FileInfoB = fb;

		Label file1label = new ("File A:") {
			X = 1,
			Y = 1,
		};
		TextField tf1 = new () {
			X = Pos.Right (file1label) + 1,
			Y = file1label.Y,
			Width = Dim.Fill () - 1,
			ReadOnly = true,
			Text = fa.Name,
		};
		Label file2label = new ("File B:") {
			X = 1,
			Y = Pos.Bottom (file1label),
		};
		TextField tf2 = new () {
			X = Pos.Right (file2label) + 1,
			Y = file2label.Y,
			Width = Dim.Fill () - 1,
			ReadOnly = true,
			Text = fb.Name,
		};
		Add (file1label, tf1, file2label, tf2);
		Top = Pos.Bottom (file2label) + 1;

		open = new ("Open...", true);
		gist = new ("Gist...");
		cancel = new ("Cancel");
		cancel.Clicked += () => Application.RequestStop ();

		// this preserve the tab order of the UI elements
		CustomUI ();

		AddButton (open);
		AddButton (gist);
		AddButton (cancel);
	}

	protected abstract void CustomUI ();

	protected FileInfo FileInfoA { get; private set; }
	protected FileInfo FileInfoB { get; private set; }

	protected Pos Top { get; private set; }
}
