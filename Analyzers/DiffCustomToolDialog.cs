using System.Text;
using CliWrap;
using NStack;
using SimpleGist;
using Terminal.Gui;

namespace AppCompare.Analyzers;

class DiffCustomToolDialog : Dialog {

	enum Output {
		Open,
		Gist,
	}

	static readonly Label file1label = new ("File A:") {
		X = 1,
		Y = 1,
	};
	static readonly TextField tf1 = new () {
		X = Pos.Right (file1label) + 1,
		Y = file1label.Y,
		Width = Dim.Fill () - 1,
		ReadOnly = true,
	};
	static readonly Label file2label = new ("File B:") {
		X = 1,
		Y = Pos.Bottom (file1label),
	};
	static readonly TextField tf2 = new () {
		X = Pos.Right (file2label) + 1,
		Y = file2label.Y,
		Width = Dim.Fill () - 1,
		ReadOnly = true,
	};
	static readonly Label t = new () {
		X = 1,
		Y = Pos.Bottom (file2label) + 1,
		Text = "Tool Executable: ",
	};
	static readonly TextField tool = new () {
		X = Pos.Right (t) + 1,
		Y = t.Y,
		Width = Dim.Fill () - 1,
	};
	static readonly Label a = new () {
		X = 1,
		Y = Pos.Bottom (t) + 1,
		Text = "Arguments: ",
	};
	static readonly TextField arguments = new () {
		X = Pos.Right (t) + 1,
		Y = a.Y,
		Width = Dim.Fill () - 1,
	};
	static readonly CheckBox sort = new () {
		X = 1,
		Y = Pos.Bottom (a) + 1,
		Text = "Sort Results",
		Checked = true,
	};
	static readonly Button open = new ("Open...", true) {
		Enabled = false,
	};
	static readonly Button gist = new ("Gist...") {
		Enabled = false,
	};
	static readonly Button cancel = new ("Cancel");

	public DiffCustomToolDialog (FileInfo fa, FileInfo fb)
		: base ("Diff Custom Tool Output")
	{
		Height = 14;
		tf1.Text = fa.Name;
		tf2.Text = fb.Name;
		tool.SetFocus ();
		tool.TextChanged += ToolChanged;
		Add (file1label, tf1, file2label, tf2, t, tool, a, arguments, sort);

		open.Clicked += async () => {
			await DiffAsync (fa, fb, Output.Open);
			Application.RequestStop ();
		};
		AddButton (open);

		gist.Clicked += async () => {
			await DiffAsync (fa, fb, Output.Gist);
			Application.RequestStop ();
		};
		AddButton (gist);

		cancel.Clicked += () => Application.RequestStop ();
		AddButton (cancel);
	}

	static void ToolChanged (ustring text)
	{
		open.Enabled = gist.Enabled = text is not null && text.Length > 0;
	}

	static async Task<CommandResult> DiffAsync (FileInfo file1, FileInfo file2, Output output)
	{
		var temp = Path.Combine (Path.GetTempPath (), "appcompare", "diffcustomtooloutput");
		var toolname = tool.Text.ToString ()!;
		var args = arguments.Text.ToString ();

		List<string> args1 = new ();
		if (args is not null && args.Length > 0) {
			foreach (var s in args.Split (' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
				args1.Add (s);
			}
		}
		List<string> args2 = new (args1);
		args1.Add (file1.FullName);
		args2.Add (file2.FullName);

		var ta = Path.Combine (temp, file1.Name) + ".a.text";
		var t1 = Cli.Wrap (toolname)
			.WithArguments (args1, escape: true)
			.WithStandardOutputPipe (PipeTarget.ToFile (ta))
			.WithValidation (CommandResultValidation.None)
			.ExecuteAsync ();

		var tb = Path.Combine (temp, file2.Name) + ".b.text";
		var t2 = Cli.Wrap (toolname)
			.WithArguments (args2, escape: true)
			.WithStandardOutputPipe (PipeTarget.ToFile (tb))
			.WithValidation (CommandResultValidation.None)
			.ExecuteAsync ();

		if (sort.Checked) {
			var sa = Path.ChangeExtension (ta, ".sorted");
			var sb = Path.ChangeExtension (tb, ".sorted");
			await t1;
			t1 = Cli.Wrap ("sort")
				.WithArguments (new [] { ta })
				.WithStandardOutputPipe (PipeTarget.ToFile (sa))
				.WithValidation (CommandResultValidation.None)
				.ExecuteAsync ();
			await t2;
			t2 = Cli.Wrap ("sort")
				.WithArguments (new [] { tb })
				.WithStandardOutputPipe (PipeTarget.ToFile (sb))
				.WithValidation (CommandResultValidation.None)
				.ExecuteAsync ();
			ta = sa;
			tb = sb;
		}
		await t1;
		await t2;

		var diff_u = Path.GetTempFileName () + ".diff";
		await Cli.Wrap ("diff")
			.WithArguments (new [] { "-u", ta, tb }, escape: true)
			.WithStandardOutputPipe (PipeTarget.ToFile (diff_u))
			.WithValidation (CommandResultValidation.None)
			.ExecuteAsync ();

		if (output == Output.Gist) {
			// provide a meaningful description of the gist
			StringBuilder sb = new ();
			sb.Append ("diff -u `");
			sb.Append (toolname).Append (' ').Append (args).Append (' ').Append (file1.Name);
			if (sort.Checked)
				sb.Append (" | sort");
			sb.Append ("` `");
			sb.Append (toolname).Append (' ').Append (args).Append (' ').Append (file2.Name);
			if (sort.Checked)
				sb.Append (" | sort");
			sb.Append ('`');
			GistRequest request = new () {
				Description = sb.ToString (),
				Public = false,
			};
			request.AddFile ("analysis.diff", File.ReadAllText (diff_u));
			var gist = await GistClient.CreateAsync (request);
			// we'll open the gist, of the unified diff, in a browser
			diff_u = gist.Url;
		}

		return await Cli.Wrap ("open")
			.WithArguments (diff_u)
			.WithValidation (CommandResultValidation.None)
			.ExecuteAsync ();
	}
}
