using System.Data;
using System.Text;
using AppCompare.Analyzers;
using Terminal.Gui;

namespace AppCompare;

class ProgramUI {

	static readonly MenuBar menu = new (new MenuBarItem [] {
		new ("_File", new MenuItem? [] {
			new ("Open App _A...", "", FileOpenApp1, null, null, Key.F1),
			new ("Open App _B...", "", FileOpenApp2, null, null, Key.F2),
			null,
			new ("_Open File Mappings...", "", FileOpenMappings, null, null, Key.CtrlMask | Key.O),
			new ("_Save File Mappings...", "", FileSaveMappings, AreMappingsPresent, null, Key.CtrlMask | Key.S),
			null,
			new ("_Export Markdown...", "", FileExport, null, null, Key.CtrlMask | Key.E),
			null,
			new ("_Quit", "", FileQuit, null, null, Key.CtrlMask | Key.Q),
		}),
		new ("_Edit", new MenuItem? [] {
			new ("_Pair files...", "", EditPairFiles, AnyFilesPresent, null, Key.CtrlMask | Key.P),
			new ("_Unpair files", "", EditUnpairFiles, IsPaired, null, Key.CtrlMask | Key.U),
		}),
		new ("_View", new MenuItem? [] {
			new ("_Gist App Compare Report...", "", ViewGist, null, null, Key.CtrlMask | Key.G),
			null,
			new ("_Refresh", "", ViewRefresh, null, null, Key.F5),
		}),
		new ("_Analyze", new MenuItem? [] {
			new ("Diff Custom _Tool Output...", "", AnalyzeDiffCustomTool, BothFilesPresent, null, Key.ShiftMask | Key.T),
			null,
			new ("_Compare Selected File", "", AnalyzeCompareSelected, BothFilesPresent, null, Key.ShiftMask | Key.C),
			new ("_Identify All Files", "", AnalyzeIdentifyAll, null, null, Key.ShiftMask | Key.I),
			new ("Identify _Selected File", "", AnalyzeIdentifySelected, AnyFilesPresent, null, Key.ShiftMask | Key.J),
		}),
		new ("_Help", new MenuItem [] {
			new ("About...", "", HelpAbout),
		}),
	});
	static readonly Label app1label = new ("App A:") {
		X = 1,
		Y = 2,
		Width = 6,
		Height = 1,
	};
	static readonly TextField tf1 = new () {
		X = Pos.Right (app1label) + 1,
		Y = 2,
		Width = Dim.Fill () - 1,
		Height = 1,
		ReadOnly = true,
	};
	static readonly Label app2label = new ("App B:") {
		X = 1,
		Y = 3,
		Width = 6,
		Height = 1,
	};
	static readonly TextField tf2 = new () {
		X = Pos.Right (app2label) + 1,
		Y = 3,
		Width = Dim.Fill () - 1,
		Height = 1,
		Text = app2_path ?? "",
		ReadOnly = true,
	};
	static readonly CompareTableView tv = new () {
		Y = Pos.Bottom (tf2) + 1,
		Height = Dim.Fill () - 1,
		Width = Dim.Fill (),
	};
	static readonly StatusBar statusBar = new (new StatusItem [] {
		new (Key.F1, "~F1~ Open A", FileOpenApp1),
		new (Key.F2, "~F2~ Open B", FileOpenApp2),
		new (Key.F5, "~F5~ Refresh", ViewRefresh),
		new (Key.CtrlMask | Key.E, "~Ctrl+E~ Export", FileExport),
		new (Key.CtrlMask | Key.G, "~Ctrl+G~ Gist", ViewGist),
		new (Key.CtrlMask | Key.Q, "~Ctrl+Q~ Quit", FileQuit),
	});

	static string? app1_path;
	static string? app2_path;
	static Dictionary<string, string> mappings = new ();

	public static int Start (string [] args, Dictionary<string, string> fileMappings)
	{
		mappings = fileMappings;

		tf1.Text = app1_path = args.Length > 0 ? Path.GetFullPath (args [0]) : Environment.CurrentDirectory;
		tf1.MouseClick += (e) => {
			if (e.MouseEvent.Flags == MouseFlags.Button1Clicked) {
				FileOpenApp1 ();
				e.Handled = true;
			}
		};

		tf2.Text = app2_path = args.Length > 1 ? Path.GetFullPath (args [1]) : Environment.CurrentDirectory;
		tf2.MouseClick += (e) => {
			if (e.MouseEvent.Flags == MouseFlags.Button1Clicked) {
				FileOpenApp2 ();
				e.Handled = true;
			}
		};

		Application.Top.ColorScheme = Colors.Base;
		Application.Top.Add (menu, app1label, tf1, app2label, tf2, tv, statusBar);

		if ((app1_path is not null) && (app2_path is not null))
			ViewRefresh ();

		Application.Run ();
		return 0;
	}

	static string? FileOpen (string? currentDirectory)
	{
		using OpenDialog d = new ("Open Application / Directory", "", null, OpenDialog.OpenMode.Directory);
		d.DirectoryPath = currentDirectory ?? Environment.CurrentDirectory;
		Application.Run (d);
		return d.Canceled ? null : Path.GetFullPath (d.FilePath.ToString ()!);
	}

	static void FileOpenApp1 ()
	{
		var selected = FileOpen (app1_path);
		if ((selected is not null) && (selected != app1_path)) {
			tf1!.Text = app1_path = selected;
			ViewRefresh ();
		}
	}

	static void FileOpenApp2 ()
	{
		var selected = FileOpen (app2_path);
		if ((selected is not null) && (selected != app2_path)) {
			tf2!.Text = app2_path = selected;
			ViewRefresh ();
		}
	}

	static void FileOpenMappings ()
	{
		using OpenDialog d = new ("Open File Mappings", "", null, OpenDialog.OpenMode.File);
		d.DirectoryPath = Environment.CurrentDirectory;
		Application.Run (d);
		if (!d.Canceled) {
			mappings = Mappings.ReadFromFile (d.FilePath.ToString ()!, out var result);
			if (result == 0)
				ViewRefresh ();
		}
	}

	static bool AreMappingsPresent ()
	{
		return mappings.Count > 0;
	}

	static void FileSaveMappings ()
	{
		using SaveDialog d = new ("Save File Mappings", "", new () { ".map" });
		d.FilePath = Path.Combine (Environment.CurrentDirectory, "mappings.map");
		Application.Run (d);
		if (!d.Canceled) {
			using StreamWriter writer = new (d.FilePath.ToString ()!);
			foreach (var kvp in mappings)
				writer.WriteLine ($"{kvp.Value}={kvp.Key}");
		}
	}

	static void FileExport ()
	{
		using SaveDialog d = new ("Export Table", "", new () { ".md" });
		Application.Run (d);
		if (!d.Canceled && (d.FilePath is not null)) {
			File.WriteAllText (d.FilePath.ToString ()!, Comparer.ExportMarkdown (new List<DataTable> { tv.Table }));
		}
	}

	static void FileQuit ()
	{
		Application.Top.Running = false;
	}

	static void EditPairFiles ()
	{
		(FileInfo? fa, FileInfo? fb) = CurrentSelection;
		if ((fa is null) && (fb is null))
			return;

		List<string> files = new ();
		if (fa is null) {
			// don't include matched files (paired or not)
			foreach (DataRow row in tv.Table.Rows) {
				(FileInfo? file1, long _) = ((FileInfo?, long)) row [1];
				if (file1 is null)
					continue;
				(FileInfo? file2, long _) = ((FileInfo?, long)) row [2];
				if (file2 is not null)
					continue;
				files.Add (file1.Name);
			}
		}
		if (fb is null) {
			foreach (DataRow row in tv.Table.Rows) {
				(FileInfo? file2, long _) = ((FileInfo?, long)) row [2];
				if (file2 is null)
					continue;
				(FileInfo? file1, long _) = ((FileInfo?, long)) row [1];
				if (file1 is not null)
					continue;
				files.Add (file2.Name);
			}
		}

		MappingsDialog d = new (fa?.Name, fb?.Name, files);
		Application.Run (d);
		if ((d.FileA is not null) && (d.FileB is not null)) {
			mappings.TryAdd (d.FileB, d.FileA);
			ViewRefresh ();
		}
	}

	static bool IsPaired ()
	{
		var current = tv.Table.Rows [tv.SelectedRow];
		return (current [0] as string)!.Contains (" -> ");
	}

	static void EditUnpairFiles ()
	{
		if (!IsPaired ())
			return;
		(FileInfo? _, FileInfo? fb) = CurrentSelection;
		mappings.Remove (fb!.Name);
		ViewRefresh ();
	}

	static void ViewGist ()
	{
		Comparer.Gist (new List<DataTable> { tv.Table });
	}

	static void ViewRefresh ()
	{
		if ((app1_path is not null) && (app2_path is not null)) {
			tv.Table = Comparer.GetAppCompareTable (app1_path, app2_path, mappings);
			if (tv.Table.ExtendedProperties ["Exception"] is Exception ex) {
				MessageBox.ErrorQuery (60, 13, "Error", ex.ToString (), "_Ok");
			}
			tv.Refresh ();
		}
	}

	static (FileInfo?, FileInfo?) CurrentSelection {
		get {
			var row = tv.Table.Rows [tv.SelectedRow];
			(FileInfo? file1, long _) = ((FileInfo?, long)) row [1];
			(FileInfo? file2, long _) = ((FileInfo?, long)) row [2];
			return (file1, file2);
		}
	}

	static bool BothFilesPresent ()
	{
		(FileInfo? fa, FileInfo? fb) = CurrentSelection;
		return fa is not null && fb is not null;
	}

	static bool AnyFilesPresent ()
	{
		(FileInfo? fa, FileInfo? fb) = CurrentSelection;
		return fa is not null || fb is not null;
	}

	static void AnalyzeCompareSelected ()
	{
		(FileInfo? fa, FileInfo? fb) = CurrentSelection;
		if (fa is not null && fb is not null) {
			tv.Table.Rows [tv.SelectedRow] [5] = CompareFiles.Compare (fa, fb);
			tv.SetNeedsDisplay ();
		}
	}

	public static void AnalyzeIdentifyAll ()
	{
		List<FileInfo> files = new (tv.Table.Rows.Count - 8);
		foreach (DataRow row in tv.Table.Rows) {
			(FileInfo? file1, long _) = ((FileInfo?, long)) row [1];
			if (file1 is null) {
				(FileInfo? file2, long _) = ((FileInfo?, long)) row [2];
				if (file2 is not null)
					files.Add (file2);
			} else {
				files.Add (file1);
			}
		}
		int n = 0;
		foreach (var filetype in Identifier.Identify (files)) {
			tv.Table.Rows [n++] [5] = filetype;
		}
		tv.SetNeedsDisplay ();
	}

	public static void AnalyzeIdentifySelected ()
	{
		string filetype = "";
		(FileInfo? fa, FileInfo? fb) = CurrentSelection;
		if (fa is not null) {
			filetype = Identifier.Identify (fa);
		} else if (fb is not null) {
			filetype = Identifier.Identify (fb);
		}
		if (filetype.Length > 0) {
			tv.Table.Rows [tv.SelectedRow] [5] = filetype;
			tv.SetNeedsDisplay ();
		}
	}

	static void AnalyzeDiffCustomTool ()
	{
		(FileInfo? fa, FileInfo? fb) = CurrentSelection;
		if (fa is not null && fb is not null) {
			DiffCustomToolDialog dialog = new (fa, fb);
			Application.Run (dialog);
		}
	}

	static void HelpAbout ()
	{
		StringBuilder aboutMessage = new ();
		aboutMessage.AppendLine ("An app bundle/directory comparison tool");
		aboutMessage.AppendLine (@"  __ _ _ __  _ __   ___ ___  _ __ ___  _ __   __ _ _ __ ___ ");
		aboutMessage.AppendLine (@" / _` | '_ \| '_ \ / __/ _ \| '_ ` _ \| '_ \ / _` | '__/ _ \");
		aboutMessage.AppendLine (@"| (_| | |_) | |_) | (_| (_) | | | | | | |_) | (_| | | |  __/");
		aboutMessage.AppendLine (@" \__,_| .__/| .__/ \___\___/|_| |_| |_| .__/ \__,_|_|  \___|");
		aboutMessage.AppendLine (@"      | |   | |                       | |                   ");
		aboutMessage.AppendLine (@"      |_|   |_|                       |_|                   ");
		aboutMessage.AppendLine ($"Version: {typeof (Program).Assembly.GetName ().Version}");
		aboutMessage.AppendLine ("Copyright 2022 Sebastien Pouliot");
		aboutMessage.AppendLine ("");
		MessageBox.Query (62, 13, "About AppCompare", aboutMessage.ToString (), "_Ok");
	}
}
