namespace AppCompare;

static class CollectionsHelpers {

	public static int Replace<T> (this IList<T> source, T oldValue, T newValue)
	{
		ArgumentNullException.ThrowIfNull (source, nameof (source));

		var index = source.IndexOf (oldValue);
		if (index != -1)
			source [index] = newValue;
		return index;
	}

	public static void ReplaceAll<T> (this IList<T> source, T oldValue, T newValue)
	{
		ArgumentNullException.ThrowIfNull (source, nameof (source));

		int index;
		do {
			index = source.IndexOf (oldValue);
			if (index != -1)
				source [index] = newValue;
		} while (index != -1);
	}
}
