using CliWrap;
using CliWrap.Buffered;

namespace AppCompare.Analyzers;

class Identifier {

	public static string Identify (IEnumerable<string> arguments)
	{
		Task<BufferedCommandResult>? result = Task.Run (async () => {
			return await Cli.Wrap ("file")
				.WithArguments (arguments, escape: true)
				.ExecuteBufferedAsync ();
		});
		return result.Result.StandardOutput;
	}

	public static string Identify (FileInfo file)
	{
		return Identify (new [] { "-b", file.FullName }).Trim ();
	}

	public static IEnumerable<string> Identify (IList<FileInfo> files)
	{
		List<string> arguments = new (files.Count + 1);
		arguments.Add ("-b");
		foreach (var file in files)
			arguments.Add (file.FullName);

		return Identify (arguments).Split ('\n', StringSplitOptions.TrimEntries);
	}
}
