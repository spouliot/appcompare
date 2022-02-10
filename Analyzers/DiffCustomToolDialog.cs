using System.Text;
using CliWrap;
using NStack;
using SimpleGist;
using Terminal.Gui;

namespace AppCompare.Analyzers;

public class DiffCustomToolDialog : DiffBaseDialog {

	public DiffCustomToolDialog (FileInfo fa, FileInfo fb)
		: base ("Diff Custom Tool Output", fa, fb)
	{
		Height = 14;
	}

	TextField? tool;
	TextField? arguments;
	CheckBox? sort;

	protected override void CustomUI ()
	{
		Label t = new () {
			X = 1,
			Y = Top,
			Text = "Tool Executable: ",
		};
		tool = new () {
			X = Pos.Right (t) + 1,
			Y = t.Y,
			Width = Dim.Fill () - 1,
		};
		tool.SetFocus ();
		tool.TextChanged += ToolChanged;
		Label a = new () {
			X = 1,
			Y = Pos.Bottom (t) + 1,
			Text = "Arguments: ",
		};
		arguments = new () {
			X = Pos.Right (t) + 1,
			Y = a.Y,
			Width = Dim.Fill () - 1,
		};
		sort = new () {
			X = 1,
			Y = Pos.Bottom (a) + 1,
			Text = "Sort Results",
			Checked = true,
		};
		Add (t, tool, a, arguments, sort);

		open.Clicked += async () => {
			await DiffAsync (Output.Open);
			Application.RequestStop ();
		};

		gist.Clicked += async () => {
			await DiffAsync (Output.Gist);
			Application.RequestStop ();
		};

		ToolChanged (tool.Text);
	}

	void ToolChanged (ustring text)
	{
		open.Enabled = gist.Enabled = text is not null && text.Length > 0;
	}

	public string ToolName => tool is null ? "" : tool.Text.ToString ()!;
	public string Arguments => arguments is null ? "" : arguments.Text.ToString ()!;
	public bool Sorted => sort is not null && sort.Checked;

	async Task<CommandResult> DiffAsync (Output output)
	{
		var temp = Path.Combine (Path.GetTempPath (), "appcompare", "diffcustomtooloutput");
		Directory.CreateDirectory (temp);

		List<string> args1 = new ();
		if (Arguments.Length > 0) {
			foreach (var s in Arguments.Split (' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
				args1.Add (s);
			}
		}
		List<string> args2 = new (args1);
		args1.Add (FileInfoA.FullName);
		args2.Add (FileInfoB.FullName);

		var ta = Path.Combine (temp, FileInfoA.Name) + ".a.text";
		var t1 = Cli.Wrap (ToolName)
			.WithArguments (args1, escape: true)
			.WithStandardOutputPipe (PipeTarget.ToFile (ta))
			.WithValidation (CommandResultValidation.None)
			.ExecuteAsync ();

		var tb = Path.Combine (temp, FileInfoB.Name) + ".b.text";
		var t2 = Cli.Wrap (ToolName)
			.WithArguments (args2, escape: true)
			.WithStandardOutputPipe (PipeTarget.ToFile (tb))
			.WithValidation (CommandResultValidation.None)
			.ExecuteAsync ();

		if (Sorted) {
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

		var diff_u = Path.Combine (temp, FileInfoA.Name) + ".a-vs-b.diff";
		await Cli.Wrap ("diff")
			.WithArguments (new [] { "-u", ta, tb }, escape: true)
			.WithStandardOutputPipe (PipeTarget.ToFile (diff_u))
			.WithValidation (CommandResultValidation.None)
			.ExecuteAsync ();

		if (output == Output.Gist) {
			// provide a meaningful description of the gist
			StringBuilder sb = new ();
			sb.Append ("diff -u <(");
			sb.Append (ToolName).Append (' ').Append (Arguments).Append (' ').Append (FileInfoA.Name);
			if (Sorted)
				sb.Append (" | sort");
			sb.Append (") <(");
			sb.Append (ToolName).Append (' ').Append (Arguments).Append (' ').Append (FileInfoB.Name);
			if (Sorted)
				sb.Append (" | sort");
			sb.Append (')');
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
