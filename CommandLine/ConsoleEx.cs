namespace System
{
	/// <summary>
	/// Fix from https://stackoverflow.com/a/12527552
	/// </summary>
	public static class ConsoleEx
	{
		/// <summary>
		/// True if no console window is available.
		/// </summary>
		public static bool IsConsoleSizeZero
		{
			get
			{
				try
				{
					return (Console.WindowHeight + Console.WindowWidth) == 0;
				}
				catch (Exception)
				{
					return true;
				}
			}
		}
	}
}
