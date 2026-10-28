using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Formats.Tests
{
	/// <summary>
	/// Field-by-field comparison of two object graphs whose types may come from different
	/// assemblies (the old editor and the moved code): types must have the same name, fields
	/// the same names and values. Cycles are followed once. Reports up to 20 differences.
	/// </summary>
	public static class Same
	{
		public static void Assert(object a, object b, string what)
		{
			var c = new Comparer();
			c.Compare(a, b, what);
			if (c.Diffs.Count > 0)
				throw new Xunit.Sdk.XunitException(what + " differs:\n" + string.Join("\n", c.Diffs));
		}

		sealed class Comparer
		{
			public readonly List<string> Diffs = new List<string>();
			readonly HashSet<(object, object)> seen = new HashSet<(object, object)>(new PairComparer());

			public void Compare(object a, object b, string path)
			{
				if (Diffs.Count >= 20) return;
				if (a == null || b == null)
				{
					if (!(a == null && b == null)) Diffs.Add(path + ": " + Show(a) + " vs " + Show(b));
					return;
				}
				Type ta = a.GetType(), tb = b.GetType();
				if (ta.Name != tb.Name) { Diffs.Add(path + ": type " + ta.FullName + " vs " + tb.FullName); return; }
				if (ta.IsEnum) { if (Convert.ToInt64(a) != Convert.ToInt64(b)) Diffs.Add(path + ": " + a + " vs " + b); return; }
				if (ta.IsPrimitive || a is string || a is decimal)
				{
					if (!a.Equals(b)) Diffs.Add(path + ": " + Show(a) + " vs " + Show(b));
					return;
				}
				if (a is Delegate || a is Type || a is MemberInfo) return;
				if (!ta.IsValueType && !seen.Add((a, b))) return;
				if (a is Array aa && b is Array ab)
				{
					if (aa.Rank != ab.Rank || aa.Length != ab.Length) { Diffs.Add(path + ": array of " + aa.Length + " vs " + ab.Length); return; }
					IEnumerator ea = aa.GetEnumerator(), eb = ab.GetEnumerator();
					int i = 0;
					while (ea.MoveNext() && eb.MoveNext()) Compare(ea.Current, eb.Current, path + "[" + i++ + "]");
					return;
				}
				if (a is IDictionary da && b is IDictionary db)
				{
					if (da.Count != db.Count) { Diffs.Add(path + ": " + da.Count + " entries vs " + db.Count); return; }
					foreach (DictionaryEntry e in da)
					{
						object other = null; bool found = false;
						foreach (DictionaryEntry f in db)
							if (Equals(Key(e.Key), Key(f.Key))) { other = f.Value; found = true; break; }
						if (!found) { Diffs.Add(path + ": key " + e.Key + " only in the first"); continue; }
						Compare(e.Value, other, path + "[" + e.Key + "]");
					}
					return;
				}
				if (a is IList la && b is IList lb)
				{
					if (la.Count != lb.Count) { Diffs.Add(path + ": " + la.Count + " items vs " + lb.Count); return; }
					for (int i = 0; i < la.Count; i++) Compare(la[i], lb[i], path + "[" + i + "]");
					return;
				}
				for (Type t = ta; t != null && t != typeof(object); t = t.BaseType)
				{
					foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
					{
						if (typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
						var g = FindField(tb, t.Name, f.Name);
						if (g == null) { Diffs.Add(path + ": field " + f.Name + " missing in the second"); continue; }
						Compare(f.GetValue(a), g.GetValue(b), path + "." + Name(f));
					}
				}
			}

			static object Key(object k) { return k == null ? null : k.GetType().IsEnum ? (object)Convert.ToInt64(k) : k is string || k.GetType().IsPrimitive ? k : k.ToString(); }

			static FieldInfo FindField(Type tb, string declaring, string name)
			{
				for (Type t = tb; t != null; t = t.BaseType)
					if (t.Name == declaring)
						return t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				return null;
			}

			static string Name(FieldInfo f)
			{
				string n = f.Name;
				if (n.StartsWith("<") && n.Contains(">k__BackingField")) n = n.Substring(1, n.IndexOf('>') - 1);
				return n;
			}

			static string Show(object o)
			{
				if (o == null) return "null";
				if (o is string s) return "\"" + (s.Length > 60 ? s.Substring(0, 60) + "..." : s) + "\"";
				return o.ToString();
			}
		}

		sealed class PairComparer : IEqualityComparer<(object, object)>
		{
			public bool Equals((object, object) x, (object, object) y) { return ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2); }
			public int GetHashCode((object, object) p) { return RuntimeHelpers.GetHashCode(p.Item1) * 31 + RuntimeHelpers.GetHashCode(p.Item2); }
		}

		/// <summary>Index of the first differing byte, or -1.</summary>
		public static int FirstDifference(byte[] a, byte[] b)
		{
			int n = Math.Min(a.Length, b.Length);
			for (int i = 0; i < n; i++) if (a[i] != b[i]) return i;
			return a.Length == b.Length ? -1 : n;
		}

		public static void Bytes(byte[] expected, byte[] actual, string what)
		{
			int d = FirstDifference(expected, actual);
			if (d >= 0)
				throw new Xunit.Sdk.XunitException(string.Format("{0}: {1} bytes vs {2}, first difference at 0x{3:X}", what, expected.Length, actual.Length, d));
		}
	}
}
