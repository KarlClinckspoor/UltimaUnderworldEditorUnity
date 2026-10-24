using System;
using System.Collections.Generic;
using System.Text;

namespace UltimateEditor.Uw1.Zoning
{
	/// <summary>Solver options: what the player is assumed able to do besides holding items.</summary>
	public class SolverOptions
	{
		public bool AllowBash = true;
		/// <summary>Pick Lock skill assumed; -1 never picks. A lock of difficulty z needs 3z (z 14: 31; z 15: never), as checkLock.</summary>
		public int LockpickSkill = -1;
		public bool AllowSpells = true;
		/// <summary>Tokens the player starts with (e.g. "spell:levitate" for a scroll in hand).</summary>
		public List<string> StartTokens = new List<string>();
		/// <summary>Links marked 'todo' count as usable.</summary>
		public bool TrustTodoLinks = true;
	}

	/// <summary>What the evaluator asks about the world.</summary>
	public interface IRequirementContext
	{
		bool HasToken(string token);
		bool ZoneReachable(int zone);
		int TalismanCount();
		SolverOptions Options { get; }
	}

	/// <summary>
	/// The little language of link requirements: tokens joined with '&amp;' (and) and '|' (or),
	/// with parentheses. See LanguageHelp for the tokens.
	/// </summary>
	public abstract class Requirement
	{
		public static readonly string[] LanguageHelp =
		{
			"key:N          hold a key whose owner field is N (the lock's key number)",
			"bash           bashing a breakable door is allowed (solver option)",
			"pick:Z         picking a lock of difficulty Z is allowed (solver option: Pick Lock skill; 3Z needed, Z 14 needs 31, Z 15 never)",
			"item:NAME      hold the named item (granted by a placement: medallion, crystal, lava_boots ...)",
			"talismans:N    hold at least N different talismans (each talisman grants talisman:<name>)",
			"flag:NAME      a quest or event flag (granted by a placement, e.g. gazer_dead)",
			"npc:NAME       the NPC can be talked to / is at the gate (granted by a placement)",
			"spell:NAME     levitate (also fly), waterwalk, jump, reveal, gate_travel: allowed by the option, or granted by a placement",
			"zone:N         zone N must be reachable (a lever or button on the other side)",
			"free           nothing needed",
			"Operators: '&' and, '|' or, parentheses.",
		};

		public abstract bool Eval(IRequirementContext ctx);
		/// <summary>The leaf terms that are false and would need to become true (all of them in an 'or').</summary>
		public abstract void Missing(IRequirementContext ctx, List<string> into);
		public abstract void Tokens(List<string> into);
		public abstract override string ToString();

		public static string Normalize(string token)
		{
			token = (token ?? "").Trim().ToLowerInvariant();
			if (token == "spell:fly" || token == "spell:levitation" || token == "spell:flying") token = "spell:levitate";
			if (token == "spell:water_walk" || token == "spell:waterwalking") token = "spell:waterwalk";
			return token;
		}

		// ---------------------------------------------------------------- nodes

		sealed class Leaf : Requirement
		{
			public readonly string Token;
			public Leaf(string t) { Token = Normalize(t); }

			public override bool Eval(IRequirementContext ctx)
			{
				var o = ctx.Options;
				if (Token == "free" || Token == "" || Token == "none" || Token == "true") return true;
				if (Token == "bash") return o.AllowBash;
				int colon = Token.IndexOf(':');
				string head = colon < 0 ? Token : Token.Substring(0, colon);
				string tail = colon < 0 ? "" : Token.Substring(colon + 1);
				int n;
				switch (head)
				{
					case "pick":
						if (o.LockpickSkill < 0) return false;
						if (!int.TryParse(tail, out n)) return false;
						if (n >= 15) return false;
						if (n == 14) return o.LockpickSkill >= 31;
						return o.LockpickSkill >= n * 3;
					case "zone":
						return int.TryParse(tail, out n) && ctx.ZoneReachable(n);
					case "talismans":
						return int.TryParse(tail, out n) && ctx.TalismanCount() >= n;
					case "spell":
						return o.AllowSpells || ctx.HasToken(Token);
					default:
						return ctx.HasToken(Token);
				}
			}

			public override void Missing(IRequirementContext ctx, List<string> into)
			{
				if (!Eval(ctx) && !into.Contains(Token)) into.Add(Token);
			}

			public override void Tokens(List<string> into) { if (!into.Contains(Token)) into.Add(Token); }
			public override string ToString() { return Token; }
		}

		sealed class And : Requirement
		{
			public readonly List<Requirement> Parts = new List<Requirement>();
			public override bool Eval(IRequirementContext ctx) { foreach (var p in Parts) if (!p.Eval(ctx)) return false; return true; }
			public override void Missing(IRequirementContext ctx, List<string> into) { foreach (var p in Parts) if (!p.Eval(ctx)) p.Missing(ctx, into); }
			public override void Tokens(List<string> into) { foreach (var p in Parts) p.Tokens(into); }
			public override string ToString()
			{
				var sb = new StringBuilder();
				for (int i = 0; i < Parts.Count; i++)
				{
					if (i > 0) sb.Append(" & ");
					bool paren = Parts[i] is Or;
					if (paren) sb.Append('(');
					sb.Append(Parts[i]);
					if (paren) sb.Append(')');
				}
				return sb.ToString();
			}
		}

		sealed class Or : Requirement
		{
			public readonly List<Requirement> Parts = new List<Requirement>();
			public override bool Eval(IRequirementContext ctx) { foreach (var p in Parts) if (p.Eval(ctx)) return true; return false; }
			public override void Missing(IRequirementContext ctx, List<string> into) { foreach (var p in Parts) p.Missing(ctx, into); }
			public override void Tokens(List<string> into) { foreach (var p in Parts) p.Tokens(into); }
			public override string ToString()
			{
				var sb = new StringBuilder();
				for (int i = 0; i < Parts.Count; i++) { if (i > 0) sb.Append(" | "); sb.Append(Parts[i]); }
				return sb.ToString();
			}
		}

		// ---------------------------------------------------------------- parser

		public static Requirement Parse(string text)
		{
			string err;
			var r = TryParse(text, out err);
			if (r == null) throw new FormatException(err);
			return r;
		}

		public static Requirement TryParse(string text, out string error)
		{
			error = null;
			var p = new Parser(text ?? "");
			try
			{
				var r = p.ParseOr();
				p.SkipWs();
				if (p.Pos < p.Text.Length) { error = "Unexpected '" + p.Text[p.Pos] + "' at " + p.Pos; return null; }
				return r;
			}
			catch (FormatException e) { error = e.Message; return null; }
		}

		sealed class Parser
		{
			public readonly string Text;
			public int Pos;
			public Parser(string t) { Text = t; }

			public void SkipWs() { while (Pos < Text.Length && char.IsWhiteSpace(Text[Pos])) Pos++; }

			public Requirement ParseOr()
			{
				var first = ParseAnd();
				SkipWs();
				if (Pos < Text.Length && (Text[Pos] == '|' || StartsWord("or")))
				{
					var o = new Or();
					o.Parts.Add(first);
					while (true)
					{
						SkipWs();
						if (Pos < Text.Length && Text[Pos] == '|') Pos++;
						else if (StartsWord("or")) Pos += 2;
						else break;
						o.Parts.Add(ParseAnd());
					}
					return o;
				}
				return first;
			}

			Requirement ParseAnd()
			{
				var first = ParseTerm();
				SkipWs();
				if (Pos < Text.Length && (Text[Pos] == '&' || Text[Pos] == ',' || StartsWord("and")))
				{
					var a = new And();
					a.Parts.Add(first);
					while (true)
					{
						SkipWs();
						if (Pos < Text.Length && (Text[Pos] == '&' || Text[Pos] == ',')) Pos++;
						else if (StartsWord("and")) Pos += 3;
						else break;
						a.Parts.Add(ParseTerm());
					}
					return a;
				}
				return first;
			}

			bool StartsWord(string w)
			{
				if (Pos + w.Length > Text.Length) return false;
				if (string.Compare(Text, Pos, w, 0, w.Length, StringComparison.OrdinalIgnoreCase) != 0) return false;
				return Pos + w.Length == Text.Length || !IsTokenChar(Text[Pos + w.Length]);
			}

			static bool IsTokenChar(char c) { return char.IsLetterOrDigit(c) || c == '_' || c == ':' || c == '-' || c == '.'; }

			Requirement ParseTerm()
			{
				SkipWs();
				if (Pos >= Text.Length) return new Leaf("free");
				if (Text[Pos] == '(')
				{
					Pos++;
					var r = ParseOr();
					SkipWs();
					if (Pos >= Text.Length || Text[Pos] != ')') throw new FormatException("')' expected at " + Pos);
					Pos++;
					return r;
				}
				int start = Pos;
				while (Pos < Text.Length && IsTokenChar(Text[Pos])) Pos++;
				if (start == Pos) throw new FormatException("A token was expected at " + Pos);
				return new Leaf(Text.Substring(start, Pos - start));
			}
		}
	}
}
