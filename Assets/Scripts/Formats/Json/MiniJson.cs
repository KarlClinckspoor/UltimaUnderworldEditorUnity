using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UltimateEditor.Uw1.Zoning
{
	/// <summary>
	/// A small JSON reader and writer (Unity 2020's JsonUtility cannot do dictionaries or
	/// nested lists). Objects read as Dictionary&lt;string, object&gt;, arrays as List&lt;object&gt;,
	/// numbers as double, plus string, bool and null.
	/// </summary>
	public static class MiniJson
	{
		public static object Parse(string text)
		{
			int i = 0;
			var v = ParseValue(text, ref i);
			SkipWs(text, ref i);
			if (i != text.Length) throw new FormatException("JSON: trailing characters at " + i);
			return v;
		}

		static void SkipWs(string s, ref int i)
		{
			while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
		}

		static object ParseValue(string s, ref int i)
		{
			SkipWs(s, ref i);
			if (i >= s.Length) throw new FormatException("JSON: unexpected end");
			char c = s[i];
			if (c == '{')
			{
				i++;
				var d = new Dictionary<string, object>();
				SkipWs(s, ref i);
				if (s[i] == '}') { i++; return d; }
				while (true)
				{
					SkipWs(s, ref i);
					string key = ParseString(s, ref i);
					SkipWs(s, ref i);
					if (s[i] != ':') throw new FormatException("JSON: ':' expected at " + i);
					i++;
					d[key] = ParseValue(s, ref i);
					SkipWs(s, ref i);
					if (s[i] == ',') { i++; continue; }
					if (s[i] == '}') { i++; return d; }
					throw new FormatException("JSON: ',' or '}' expected at " + i);
				}
			}
			if (c == '[')
			{
				i++;
				var l = new List<object>();
				SkipWs(s, ref i);
				if (s[i] == ']') { i++; return l; }
				while (true)
				{
					l.Add(ParseValue(s, ref i));
					SkipWs(s, ref i);
					if (s[i] == ',') { i++; continue; }
					if (s[i] == ']') { i++; return l; }
					throw new FormatException("JSON: ',' or ']' expected at " + i);
				}
			}
			if (c == '"') return ParseString(s, ref i);
			if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
			if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
			if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
			int start = i;
			while (i < s.Length && ("+-0123456789.eE".IndexOf(s[i]) >= 0)) i++;
			if (start == i) throw new FormatException("JSON: unexpected '" + c + "' at " + i);
			return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		static string ParseString(string s, ref int i)
		{
			if (s[i] != '"') throw new FormatException("JSON: string expected at " + i);
			i++;
			var sb = new StringBuilder();
			while (true)
			{
				if (i >= s.Length) throw new FormatException("JSON: unterminated string");
				char c = s[i++];
				if (c == '"') return sb.ToString();
				if (c != '\\') { sb.Append(c); continue; }
				char e = s[i++];
				switch (e)
				{
					case '"': sb.Append('"'); break;
					case '\\': sb.Append('\\'); break;
					case '/': sb.Append('/'); break;
					case 'b': sb.Append('\b'); break;
					case 'f': sb.Append('\f'); break;
					case 'n': sb.Append('\n'); break;
					case 'r': sb.Append('\r'); break;
					case 't': sb.Append('\t'); break;
					case 'u':
						sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber));
						i += 4;
						break;
					default: throw new FormatException("JSON: bad escape at " + i);
				}
			}
		}

		// ---- access helpers

		public static Dictionary<string, object> Obj(object o) { return o as Dictionary<string, object>; }
		public static List<object> Arr(object o) { return o as List<object>; }

		public static object Get(Dictionary<string, object> d, string key)
		{
			object v;
			return d != null && d.TryGetValue(key, out v) ? v : null;
		}
		public static string Str(Dictionary<string, object> d, string key, string def = "")
		{
			var v = Get(d, key);
			return v == null ? def : v.ToString();
		}
		public static int Int(Dictionary<string, object> d, string key, int def = 0)
		{
			var v = Get(d, key);
			return v is double ? (int)Math.Round((double)v) : def;
		}
		public static bool Bool(Dictionary<string, object> d, string key, bool def = false)
		{
			var v = Get(d, key);
			return v is bool ? (bool)v : def;
		}

		// ---- writing

		public static string Write(object value, bool pretty = true)
		{
			var sb = new StringBuilder();
			WriteValue(sb, value, pretty ? 0 : -1);
			return sb.ToString();
		}

		static void Indent(StringBuilder sb, int level)
		{
			if (level < 0) return;
			sb.Append('\n');
			for (int i = 0; i < level; i++) sb.Append(' ');
		}

		static void WriteValue(StringBuilder sb, object v, int level)
		{
			if (v == null) { sb.Append("null"); return; }
			if (v is string) { WriteString(sb, (string)v); return; }
			if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
			if (v is int || v is long || v is byte || v is short) { sb.Append(Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture)); return; }
			if (v is double || v is float)
			{
				double d = Convert.ToDouble(v);
				if (Math.Abs(d - Math.Round(d)) < 1e-9 && Math.Abs(d) < 1e15) sb.Append(((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture));
				else sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
				return;
			}
			var dict = v as IDictionary<string, object>;
			if (dict != null)
			{
				sb.Append('{');
				bool first = true;
				foreach (var kv in dict)
				{
					if (!first) sb.Append(',');
					first = false;
					Indent(sb, level < 0 ? -1 : level + 1);
					WriteString(sb, kv.Key);
					sb.Append(level < 0 ? ":" : ": ");
					WriteValue(sb, kv.Value, level < 0 ? -1 : level + 1);
				}
				if (!first) Indent(sb, level);
				sb.Append('}');
				return;
			}
			var list = v as System.Collections.IEnumerable;
			if (list != null)
			{
				sb.Append('[');
				bool first = true;
				bool scalars = true;
				foreach (var item in list) if (item is IDictionary<string, object> || (item is System.Collections.IEnumerable && !(item is string))) scalars = false;
				foreach (var item in list)
				{
					if (!first) sb.Append(scalars ? ", " : ",");
					first = false;
					if (!scalars) Indent(sb, level < 0 ? -1 : level + 1);
					WriteValue(sb, item, level < 0 ? -1 : level + 1);
				}
				if (!first && !scalars) Indent(sb, level);
				sb.Append(']');
				return;
			}
			WriteString(sb, v.ToString());
		}

		static void WriteString(StringBuilder sb, string s)
		{
			sb.Append('"');
			foreach (char c in s)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("X4"));
						else sb.Append(c);
						break;
				}
			}
			sb.Append('"');
		}
	}
}
