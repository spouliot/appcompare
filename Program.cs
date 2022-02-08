using Spectre.Console;
using Terminal.Gui;

namespace AppCompare;

class Program {

	/// <summary>
	/// Compare two application bundles / directories.
	/// </summary>
	/// <param name="args">Paths to the first and second applications bundle/directory. If no other option is give then the text user interface will be shown.</param>
	/// <param name="outputMarkdown">Filename for the markdown output (optional).</param>
	/// <param name="gist">Gist the output.</param>
	/// <returns>0 for success, 1 for invalid/incorrect arguments, 2 for unexpected failure.</returns>
	static int Main (string [] args, string? outputMarkdown, bool gist)
	{
		try {
			// if we start the TUI then the paths are optional
			if (outputMarkdown is null && !gist) {
				try {
					Application.Init ();
					return ProgramUI.Start (args);
				}
				finally {
					Application.Shutdown ();
				}
			}

			// without the TUI the paths are required
			if (args.Length != 2) {
				AnsiConsole.MarkupLine ("[red]Error:[/] Paths for both application bundles/directories are required. Use `apidiff --help` for details.");
				return 1;
			}
			if (!CheckDirectory (args [0], out var app1)) {
				AnsiConsole.MarkupLine ($"[red]Error:[/] Cannot find bundle or directory at `{app1}`.");
				return 1;
			}
			if (!CheckDirectory (args [1], out var app2)) {
				AnsiConsole.MarkupLine ($"[red]Error:[/] Cannot find bundle or directory at `{app2}`.");
				return 1;
			}

			var table = Comparer.GetTable (app1, app2);
			string markdown = Comparer.ExportMarkdown (table);

			if (outputMarkdown is not null) {
				File.WriteAllText (outputMarkdown, markdown);
			}

			if (gist) {
				var url = Comparer.Gist (table, openUrl: false);
				Console.Out.WriteLine (url);
			}

			return 0;
		} catch (Exception e) {
			AnsiConsole.Markup ("[bold red]FATAL:[/] ");
			AnsiConsole.WriteException (e);
			return 2;
		}
	}

	static bool CheckDirectory (string path, out string fullPath)
	{
		fullPath = Path.GetFullPath (path);
		return Directory.Exists (fullPath);
	}
}