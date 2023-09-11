using System.Data;
using System.Text;
using CliWrap;
using SimpleGist;

namespace AppCompare;

class Comparer {

	const string filter_all_files = "*.*";
	const string filter_obj_files = "*.o";

	public static DataTable GetAppCompareTable (string app1path, string app2path, Dictionary<string, string> mappings)
	{
		DataTable dt = new ("App bundle compare");
		DataColumn files = new ("Files", typeof (string));
		dt.Columns.Add (files);
		dt.PrimaryKey = new [] { files };

		dt.Columns.Add (new DataColumn ("Size A", typeof ((FileInfo, long))));
		dt.Columns.Add (new DataColumn ("Size B", typeof ((FileInfo, long))));
		dt.Columns.Add (new DataColumn ("diff", typeof (long)));
		dt.Columns.Add (new DataColumn ("%", typeof (double)));
		dt.Columns.Add (new DataColumn (" ", typeof (string)));

		try {
			Populate (dt, app1path, app2path, mappings, filter_all_files);
		} catch (Exception ex) {
			dt.ExtendedProperties.Add ("Exception", ex);
		}

		dt.DefaultView.Sort = "Files ASC";
		dt = dt.DefaultView.ToTable ();
		dt.ExtendedProperties.Add ("AppA", app1path);
		dt.ExtendedProperties.Add ("AppB", app2path);

		long size_a = 0;
		long size_b = 0;
		long managed_a = 0;
		long managed_b = 0;
		long native_a = 0;
		long native_b = 0;
		long aotdata_a = 0;
		long aotdata_b = 0;
		long others_a = 0;
		long others_b = 0;

		foreach (DataRow row in dt.Rows) {
			(FileInfo? f1file, long f1length) = ((FileInfo?, long)) row [1];
			size_a += f1length;
			(FileInfo? f2file, long f2length) = ((FileInfo?, long)) row [2];
			size_b += f2length;
			FileInfo f = f1file ?? f2file!;
			switch (f.Extension) {
			case ".dll":
			case ".exe":
				managed_a += f1length;
				managed_b += f2length;
				break;
			case "":
				switch (f.Name) {
				case "CodeResources":
				case "PkgInfo":
					others_a += f1length;
					others_b += f2length;
					break;
				default:
					native_a += f1length;
					native_b += f2length;
					break;
				}
				break;
			case ".arm64":
			case ".armv7":
			case ".armv7s":
			case ".x86_64":
				if (f.Name.Contains (".aotdata.")) {
					aotdata_a += f1length;
					aotdata_b += f2length;
				} else {
					others_a += f1length;
					others_b += f2length;
				}
				break;
			default:
				others_a += f1length;
				others_b += f2length;
				break;
			}
		}

		AddEmptyRow (dt);
		AddEmptyRow (dt, "STATISTICS");
		AddSummaryRow (dt, "Native", native_a + aotdata_a, native_b + aotdata_b);
		if ((aotdata_a > 0) || (aotdata_b > 0)) {
			AddSummaryRow (dt, "    Executable", native_a, native_b);
			AddSummaryRow (dt, "    AOT data", aotdata_a, aotdata_b);
		}
		if ((managed_a > 0) || (managed_b > 0))
			AddSummaryRow (dt, "Managed *.dll/exe", managed_a, managed_b);
		AddEmptyRow (dt);
		AddSummaryRow (dt, "TOTAL", size_a, size_b);

		return dt;
	}

	public static DataTable GetObjCompareTable (string app1path, string app2path, Dictionary<string, string> mappings)
	{
		DataTable dt = new ("Object files compare");
		DataColumn files = new ("Files", typeof (string));
		dt.Columns.Add (files);
		dt.PrimaryKey = new [] { files };

		dt.Columns.Add (new DataColumn ("Size A", typeof ((FileInfo, long))));
		dt.Columns.Add (new DataColumn ("Size B", typeof ((FileInfo, long))));
		dt.Columns.Add (new DataColumn ("diff", typeof (long)));
		dt.Columns.Add (new DataColumn ("%", typeof (double)));
		dt.Columns.Add (new DataColumn (" ", typeof (string)));

		try {
			Populate (dt, app1path, app2path, mappings, filter_obj_files);
		} catch (Exception ex) {
			dt.ExtendedProperties.Add ("Exception", ex);
		}

		dt.DefaultView.Sort = "Files ASC";
		dt = dt.DefaultView.ToTable ();
		dt.ExtendedProperties.Add ("AppA", app1path);
		dt.ExtendedProperties.Add ("AppB", app2path);

		AddEmptyRow (dt);
		return dt;
	}

	static void Populate (DataTable dt, string app1path, string app2path, Dictionary<string, string> mappings, string filter)
	{
		DirectoryInfo Directory1 = new (app1path);
		if (Directory1.Exists) {
			foreach (var file in Directory1.GetFiles (filter, SearchOption.AllDirectories)) {
				dt.Rows.Add (new object? [] {
					file.FullName[(app1path.Trim (Path.DirectorySeparatorChar).Length + 2)..],
					(file, file.Length),
					empty,
					-file.Length,
					-1.0d,
					"",
				});
			}
		}

		DirectoryInfo Directory2 = new (app2path);
		if (!Directory2.Exists)
			return;

		foreach (var file in Directory2.GetFiles (filter, SearchOption.AllDirectories)) {
			var name = file.FullName[(app2path.Trim (Path.DirectorySeparatorChar).Length + 2)..];
			var row = dt.Rows.Find (name);
			var remapped = false;
			if (row is null) {
				// 2nd chance if the name is mapped to a different name in the first directory
				if (mappings.TryGetValue (name, out var mapped)) {
					row = dt.Rows.Find (mapped);
					remapped = true;
				}
			}
			if (row is null) {
				dt.Rows.Add (new object? [] {
					name,
					empty,
					(file, file.Length),
					file.Length,
					double.NaN,
					"",
				});
			} else {
				if (remapped) {
					var oldname = (string) row [0];
					row [0] = oldname + " -> " + name;
				}
				row [2] = (file, file.Length);
				var diff = file.Length;
				(_, long f1length) = ((FileInfo?, long)) row [1];
				diff -= f1length;
				row [3] = diff;
				row [4] = diff / (double) f1length;
				row [5] = "";
			}
		}
	}

	static readonly (FileInfo?, long) empty = (null, 0);

	static void AddEmptyRow (DataTable dt, string name = "")
	{
		dt.Rows.Add (new object [] { name, empty, empty, 0, double.NaN, "" });
	}

	static void AddSummaryRow (DataTable dt, string name, long a, long b)
	{
		dt.Rows.Add (new object [] { name,
			((FileInfo?) null, a),
			((FileInfo?) null, b),
			b - a,
			(a == 0) ? double.NaN : (b - a) / (double) a,
			"",
		});
	}

	static public string ExportMarkdown (List<DataTable> tables)
	{
		StringBuilder builder = new ();
		builder.AppendLine ("# Application Comparer");
		builder.AppendLine ();

		foreach (var table in tables) {
			builder.AppendLine ($"## {table.TableName}");
			builder.AppendLine ();
			builder.Append ("* Path A: `").Append (table.ExtendedProperties ["AppA"]).AppendLine ("`");
			builder.Append ("* Path B: `").Append (table.ExtendedProperties ["AppB"]).AppendLine ("`");
			builder.AppendLine ();

			var columns = table.Columns;
			// skip last "comment" column - it's for the tool itself, not for reporting
			for (int i = 0; i < columns.Count - 1; i++) {
				builder.Append ("| ").Append (columns [i].ColumnName).Append (' ');
			}
			builder.AppendLine ("|");

			builder.AppendLine ("|:----|----:|----:|----:|----:|");

			foreach (DataRow row in table.Rows) {
				var file = row [0] as string;
				if (file!.Length == 0) {
					builder.AppendLine ("| | | | | |");
				} else {
					builder.Append ("| ").Append (row [0]);
					(_, long f1length) = ((FileInfo?, long)) row [1];
					builder.Append (" | ").AppendFormat ("{0:N0}", f1length);
					(_, long f2length) = ((FileInfo?, long)) row [2];
					builder.Append (" | ").AppendFormat ("{0:N0}", f2length);
					builder.Append (" | ").AppendFormat ("{0:N0}", row [3]);
					var percentage = (double) row [4];
					var display = Double.IsNaN (percentage) ? "-" : percentage.ToString ("P1");
					builder.Append (" | ").Append (display);
					builder.AppendLine (" |)");
				}
			}
		}
		builder.AppendLine ();
		builder.AppendLine ("Generated by [appcompare](https://github.com/spouliot/appcompare)");
		return builder.ToString ();
	}

	static public string Gist (List<DataTable> tables, bool openUrl = true)
	{
		GistRequest request = new () {
			Description = "Application Comparison Report",
			Public = false,
		};
		request.AddFile ("report.md", ExportMarkdown (tables));

		Task<GistResponse>? task = Task.Run (async () => await GistClient.CreateAsync (request));
		var url = "https://github.com/spouliot/SimpleGist/wiki#errors";
		if (task.Result is null)
			return url;

		url = task.Result.Url;
		if (openUrl) {
			_ = Task.Run (async () => await Cli.Wrap ("open")
				.WithArguments (url)
				.WithValidation (CommandResultValidation.None)
				.ExecuteAsync ()
				).Result;
		}
		return url;
	}
}
