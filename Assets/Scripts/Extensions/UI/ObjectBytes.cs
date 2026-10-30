namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Converts the editor's StaticObject / MobileObject to the record bytes the game uses
	/// and back through ObjectRecord, which the level reader and writer use too, so a field
	/// edited through a <see cref="Uw1Schemas"/> field lands where the save writes it.
	/// </summary>
	public static class ObjectBytes
	{
		public static byte[] From(StaticObject so) { return ObjectRecord.From(so); }

		/// <summary>
		/// Copies the record back into the object's fields. The item id, the next link and
		/// (for a non-quantity object) the contents link are list structure and are not taken
		/// from the record.
		/// </summary>
		public static void To(StaticObject so, byte[] b) { ObjectRecord.Read(so, b, 0, false); }

		/// <summary>Fields the inspector shows but must not change (list structure).</summary>
		public static bool IsStructural(FieldDef f, StaticObject so)
		{
			if (f.Name == "Item id" || f.Name == "Next") return true;
			if (f.Name == "Link" && !so.IsQuantity) return true;
			return false;
		}
	}
}
