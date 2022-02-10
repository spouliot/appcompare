using CliWrap;
using CliWrap.Buffered;

namespace AppCompare.Analyzers;

class CompareFiles {

	public static string Compare (FileInfo fileA, FileInfo fileB)
	{
		List<string> arguments = new (3) {
			"-b",
			fileA.FullName,
			fileB.FullName,
		};

		Task<BufferedCommandResult>? result = Task.Run (async () => {
			return await Cli.Wrap ("cmp")
				.WithArguments (arguments, escape: true)
				.WithValidation (CommandResultValidation.None)
				.ExecuteBufferedAsync ();
		});

		if (result.Result.ExitCode == 0)
			return "identical";
		
		var output = result.Result.StandardOutput;
		var pos = output.LastIndexOf (" differ: ");
		return output [(pos + 1)..].Trim ();
	}
}
