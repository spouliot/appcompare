
using Spectre.Console;

namespace AppCompare;

static class Mappings {

	public static Dictionary<string, string> ReadFromFile (string mappingFile, out int result)
	{
		Dictionary<string, string> dict = new ();
		result = 1;
		var lineno = 0;
		try {
			foreach (var line in File.ReadLines (mappingFile)) {
				lineno++;
				if (line [0] == '#')
					continue;
				var eq = line.IndexOf ('=');
				if (eq == -1) {
					AnsiConsole.MarkupLine ($"[red]Error line {lineno}:[/] Missing `=` in a non-comment (`#`) line mapping.");
					return dict;
				}
				var file_b = line [(eq + 1)..];
				if (dict.ContainsKey (file_b)) {
					AnsiConsole.MarkupLine ($"[red]Error line {lineno}:[/] Duplicate mapping for `{file_b}`.");
					return dict;
				}
				dict [file_b] = line [..eq];
			}
		} catch (Exception e) {
			AnsiConsole.WriteException (e);
		}
		result = 0;
		return dict;
	}
}
