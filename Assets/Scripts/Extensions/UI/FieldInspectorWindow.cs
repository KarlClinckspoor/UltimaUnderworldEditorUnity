using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Shows every field of the selected object with the reference's name, bit position,
	/// meaning and how sure that meaning is, plus what quality, owner and link mean for this
	/// item; fields that are not list structure can be edited. Values are entered in decimal
	/// or as 0x hex.
	/// </summary>
	public static class FieldInspectorWindow
	{
		static Window open;

		public static void Show(StaticObject so)
		{
			if (open != null) open.Close();
			var w = UiKit.Window("Object fields", 640, 560);
			open = w;
			w.Closed += () => { if (open == w) open = null; };
			if (so == null)
			{
				UiKit.Paragraph(w.Content, "Select an object on the map first, then open this window again.", 12, UiKit.TextColor);
				return;
			}
			Build(w, so);
		}

		static void Build(Window w, StaticObject so)
		{
			UiKit.Clear(w.Content);
			UiKit.Clear(w.Toolbar);
			var bytes = ObjectBytes.From(so);
			var schema = so is MobileObject ? Uw1Schemas.MobileObject : Uw1Schemas.StaticObject;
			w.Title.text = string.Format("Object fields: {0} (item 0x{1:X3}, index {2}, level {3})", so.Name, so.ObjectID, so.CurrentAdress, so.CurrentLevel);

			var head = UiKit.Row(w.Toolbar, 20);
			var rawLabel = UiKit.Label(head, "Raw: " + BitConverter.ToString(bytes).Replace("-", " "), 11, FontStyle.Normal, UiKit.DimTextColor);
			UiKit.Button(head, "Refresh", () => Build(w, so), 70);

			var roles = Uw1ObjectRoles.For(so.ObjectID, so.IsQuantity);
			if (roles.Count > 0)
			{
				UiKit.Label(w.Content, "What the fields mean for this item", 12, FontStyle.Bold);
				foreach (var r in roles)
				{
					var t = UiKit.Paragraph(w.Content, "<b>" + r.Field + "</b>: " + r.Meaning + "   <i>(" + r.Source + ")</i>", 11, UiKit.TextColor);
					t.supportRichText = true;
				}
				if (Uw1Traps.IsTrap(so.ObjectID) && so.ObjectID == Uw1Traps.FirstTrap + (int)Uw1Traps.TrapType.Hack)
					UiKit.Paragraph(w.Content, "This special trap: " + Uw1Traps.HackTrapName(so.Quality), 11, UiKit.WarnColor);
			}

			UiKit.Label(w.Content, "Fields (" + schema.Source + ")", 12, FontStyle.Bold);
			foreach (var f in schema.Fields)
			{
				var row = UiKit.Row(w.Content, 20);
				string marker = f.Certainty == Certainty.Certain ? "" : f.Certainty == Certainty.Inferred ? " (inferred)" : " (unknown)";
				var name = UiKit.Label(row, f.Name + marker, 11, FontStyle.Normal, f.Certainty == Certainty.Unknown ? UiKit.WarnColor : UiKit.TextColor, 170);
				UiKit.Label(row, f.Location, 10, FontStyle.Normal, UiKit.DimTextColor, 100);
				int value = f.Get(bytes);
				bool locked = ObjectBytes.IsStructural(f, so) || (f.Name == "Is quantity" && so.Special != 0);
				var field = f;
				Text hex = null;
				InputField input = null;
				input = UiKit.Input(row, value.ToString(CultureInfo.InvariantCulture), s =>
				{
					// onEndEdit also fires when the field just loses focus. Update in place rather
					// than rebuilding, so the field clicked next is not destroyed under the cursor.
					var b = ObjectBytes.From(so);
					int cur = field.Get(b);
					int v;
					if (!TryParse(s, out v) || v < field.Min || v > field.Max)
					{
						w.Status.text = string.Format("{0}: enter {1}..{2}", field.Name, field.Min, field.Max);
						input.text = cur.ToString(CultureInfo.InvariantCulture);
						return;
					}
					if (v == cur) return;
					field.Set(b, v);
					ObjectBytes.To(so, b);
					input.text = v.ToString(CultureInfo.InvariantCulture);
					if (hex) hex.text = string.Format("0x{0:X}", v);
					if (rawLabel) rawLabel.text = "Raw: " + BitConverter.ToString(ObjectBytes.From(so)).Replace("-", " ");
					w.Status.text = string.Format("{0} set to {1}. Refresh to update the meanings above.", field.Name, v);
					var ui = UiKit.FindUIManager();
					if (ui) ui.SetObjectProperties(so);
				}, 70);
				input.interactable = !locked;
				hex = UiKit.Label(row, string.Format("0x{0:X}", value), 10, FontStyle.Normal, UiKit.DimTextColor, 50);
				string tip = field.Description + (field.Source != null ? "\n(" + field.Source + ")" : "") + (locked ? "\nRead-only here: it is list structure." : "");
				UiKit.Tooltip(name.gameObject, tip);
				name.raycastTarget = true;
				UiKit.Tooltip(input.gameObject, tip);
			}
			UiKit.Paragraph(w.Content, "Hover a field name for its meaning and the reference routine that reads it. 'Inferred' means named from how the code uses it; 'unknown' means nothing in the code explains it.", 10);
		}

		public static bool TryParse(string s, out int v)
		{
			s = (s ?? "").Trim();
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				return int.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v);
			return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
		}
	}
}
