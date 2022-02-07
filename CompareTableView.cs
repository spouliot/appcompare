using Terminal.Gui;

namespace AppCompare;

class CompareTableView : TableView {

	public CompareTableView ()
	{
		FullRowSelect = true;
		Style.AlwaysShowHeaders = true;
	}

	public void Refresh ()
	{
		var columns = Table.Columns;
		// this gets lost when we re-assign a (sorted) data table
		Style.ColumnStyles.Add (columns [1], ColumnStyles.FileInfoLengthNumber);
		Style.ColumnStyles.Add (columns [2], ColumnStyles.FileInfoLengthNumber);
		Style.ColumnStyles.Add (columns [3], ColumnStyles.LongNumber);
		Style.ColumnStyles.Add (columns [4], ColumnStyles.Percentage);
	}
}
