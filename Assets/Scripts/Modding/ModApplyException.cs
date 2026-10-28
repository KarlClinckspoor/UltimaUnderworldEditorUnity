using System;

namespace UltimateEditor.Modding
{
	/// <summary>A mod cannot be applied to these files (the files are not the ones it was made from, or it is broken).</summary>
	public sealed class ModApplyException : Exception
	{
		public ModApplyException(string message) : base(message) { }
	}
}
