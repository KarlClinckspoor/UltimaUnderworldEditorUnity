using System;
using System.Collections.Generic;
using System.Text;

namespace UltimateEditor.Uw1.Zoning
{
	/// <summary>One step of the forward search: what became reachable at once.</summary>
	public class Sphere
	{
		public int Index;
		public List<int> Zones = new List<int>();
		public List<string> Tokens = new List<string>();
		public List<string> TokenSources = new List<string>();   // "key:1 (zone 53)"
		public List<ZoneLink> LinksUsed = new List<ZoneLink>();
	}

	public class PathStep
	{
		public int Zone;
		public ZoneLink Link;       // the link used to get here, null for the start
		public bool Reverse;        // link used from To to From (two-way)
		public int Sphere;
	}

	public class BlockedLink
	{
		public ZoneLink Link;
		public bool Reverse;
		public int FromZone, ToZone;
		public List<string> Missing = new List<string>();
	}

	public class SolveResult
	{
		public bool GoalReached;
		public List<Sphere> Spheres = new List<Sphere>();
		public Dictionary<int, int> ZoneSphere = new Dictionary<int, int>();
		public Dictionary<string, int> TokenSphere = new Dictionary<string, int>();
		public Dictionary<int, PathStep> FirstReach = new Dictionary<int, PathStep>();
		public List<PathStep> Path = new List<PathStep>();
		public List<BlockedLink> Blocked = new List<BlockedLink>();
		public List<int> Unreached = new List<int>();
		/// <summary>Placements in reachable zones whose own requirement never held.</summary>
		public List<string> UngrantedPlacements = new List<string>();
		/// <summary>Tokens some link needs that no placement grants anywhere.</summary>
		public List<string> NoSource = new List<string>();
		/// <summary>Pairs of zones joined by a free two-way link: they could be one zone.</summary>
		public List<KeyValuePair<int, int>> MergeCandidates = new List<KeyValuePair<int, int>>();
		public List<string> Warnings = new List<string>();

		public string Report(ZoneSet zs)
		{
			var sb = new StringBuilder();
			sb.AppendLine(GoalReached ? "Goal reached." : "Goal NOT reached.");
			foreach (var w in Warnings) sb.AppendLine("Warning: " + w);
			sb.AppendLine();
			sb.AppendLine("Spheres (what becomes reachable, step by step):");
			foreach (var s in Spheres)
			{
				sb.Append("  ").Append(s.Index).Append(": zones ");
				for (int i = 0; i < s.Zones.Count; i++) { if (i > 0) sb.Append(", "); sb.Append(ZoneName(zs, s.Zones[i])); }
				if (s.TokenSources.Count > 0) sb.Append("\n       gains ").Append(string.Join(", ", s.TokenSources.ToArray()));
				sb.AppendLine();
			}
			sb.AppendLine();
			if (Path.Count > 0)
			{
				sb.AppendLine("Route from the start to the goal (one of the shortest in links):");
				foreach (var p in Path)
				{
					if (p.Link == null) sb.AppendLine("  start: " + ZoneName(zs, p.Zone));
					else sb.AppendLine(string.Format("  -> {0}  via {1} [{2}] needs: {3}{4}", ZoneName(zs, p.Zone), p.Link.Kind, p.Link.Id, p.Link.Requires, p.Link.Confidence == "todo" ? "  (todo)" : ""));
				}
				sb.AppendLine();
			}
			if (Blocked.Count > 0)
			{
				sb.AppendLine("Blocked at the end (reachable side -> other side: what is missing):");
				foreach (var b in Blocked)
					sb.AppendLine(string.Format("  {0} -> {1} [{2} {3}]: {4}", ZoneName(zs, b.FromZone), ZoneName(zs, b.ToZone), b.Link.Kind, b.Link.Id, string.Join(" | ", b.Missing.ToArray())));
				sb.AppendLine();
			}
			if (UngrantedPlacements.Count > 0)
			{
				sb.AppendLine("In reach but not obtained (the placement's own requirement):");
				foreach (var u in UngrantedPlacements) sb.AppendLine("  " + u);
				sb.AppendLine();
			}
			if (Unreached.Count > 0)
			{
				sb.Append("Never reached: ");
				for (int i = 0; i < Unreached.Count; i++) { if (i > 0) sb.Append(", "); sb.Append(ZoneName(zs, Unreached[i])); }
				sb.AppendLine().AppendLine();
			}
			if (NoSource.Count > 0)
			{
				sb.AppendLine("Required but granted nowhere (add a placement, or an option covers it):");
				foreach (var t in NoSource)
				{
					string why;
					sb.AppendLine("  " + t + (zs.OpenSources.TryGetValue(t, out why) ? "  - " + why : ""));
				}
				sb.AppendLine();
			}
			if (MergeCandidates.Count > 0)
			{
				sb.AppendLine("Joined by a free two-way link (could be one zone):");
				foreach (var m in MergeCandidates) sb.AppendLine("  " + ZoneName(zs, m.Key) + "  <->  " + ZoneName(zs, m.Value));
			}
			return sb.ToString();
		}

		public static string ZoneName(ZoneSet zs, int id)
		{
			var z = zs.GetZone(id);
			return z == null ? id.ToString() : id + " " + z.Name;
		}
	}

	/// <summary>
	/// Forward search over the zone graph: starting in StartZone with nothing, repeatedly
	/// cross every link whose requirement holds and pick up what the new zones grant, until
	/// nothing changes. The spheres are the randomizer's logic order; the path is the chain
	/// of links that first reached the goal.
	/// </summary>
	public class ZoneSolver : IRequirementContext
	{
		readonly ZoneSet zs;
		readonly SolverOptions options;
		readonly HashSet<string> tokens = new HashSet<string>();
		readonly HashSet<int> reachable = new HashSet<int>();
		readonly Dictionary<ZoneLink, Requirement> parsed = new Dictionary<ZoneLink, Requirement>();
		readonly Dictionary<Placement, Requirement> parsedPlacements = new Dictionary<Placement, Requirement>();

		public SolverOptions Options { get { return options; } }
		public bool HasToken(string token) { return tokens.Contains(Requirement.Normalize(token)); }
		public bool ZoneReachable(int zone) { return reachable.Contains(zone); }
		public int TalismanCount()
		{
			int n = 0;
			foreach (var t in tokens) if (t.StartsWith("talisman:")) n++;
			return n;
		}

		public ZoneSolver(ZoneSet set, SolverOptions opts)
		{
			zs = set;
			options = opts ?? new SolverOptions();
		}

		Requirement Req(ZoneLink k, SolveResult res)
		{
			Requirement r;
			if (parsed.TryGetValue(k, out r)) return r;
			string err;
			r = Requirement.TryParse(k.Requires, out err);
			if (r == null)
			{
				res.Warnings.Add(string.Format("Link {0} ({1} -> {2}): cannot read '{3}': {4}. Treated as impossible.", k.Id, k.From, k.To, k.Requires, err));
				r = Requirement.Parse("flag:__unparsable__");
			}
			parsed[k] = r;
			return r;
		}

		Requirement PlacementReq(Placement p, SolveResult res)
		{
			Requirement r;
			if (parsedPlacements.TryGetValue(p, out r)) return r;
			if (string.IsNullOrEmpty(p.Requires)) r = Requirement.Parse("free");
			else
			{
				string err;
				r = Requirement.TryParse(p.Requires, out err);
				if (r == null)
				{
					res.Warnings.Add("Placement " + p.Grants + " in zone " + p.Zone + ": cannot read '" + p.Requires + "': " + err + ". Never granted.");
					r = Requirement.Parse("flag:__unparsable__");
				}
			}
			parsedPlacements[p] = r;
			return r;
		}

		public SolveResult Solve(int start, int goal)
		{
			var res = new SolveResult();
			tokens.Clear();
			reachable.Clear();
			foreach (var t in options.StartTokens) tokens.Add(Requirement.Normalize(t));

			var sphere0 = new Sphere { Index = 0 };
			reachable.Add(start);
			sphere0.Zones.Add(start);
			res.ZoneSphere[start] = 0;
			res.FirstReach[start] = new PathStep { Zone = start, Sphere = 0 };
			while (Grant(start, sphere0, res)) { }
			res.Spheres.Add(sphere0);

			while (true)
			{
				var next = new Sphere { Index = res.Spheres.Count };
				var newly = new List<KeyValuePair<int, PathStep>>();
				foreach (var k in zs.Links)
				{
					if (k.Confidence == "todo" && !options.TrustTodoLinks) continue;
					var r = Req(k, res);
					// forward
					if (reachable.Contains(k.From) && !reachable.Contains(k.To) && r.Eval(this))
						if (!next.Zones.Contains(k.To)) { next.Zones.Add(k.To); next.LinksUsed.Add(k); newly.Add(new KeyValuePair<int, PathStep>(k.To, new PathStep { Zone = k.To, Link = k, Reverse = false, Sphere = next.Index })); }
					// backward on two-way links
					if (k.TwoWay && reachable.Contains(k.To) && !reachable.Contains(k.From) && r.Eval(this))
						if (!next.Zones.Contains(k.From)) { next.Zones.Add(k.From); next.LinksUsed.Add(k); newly.Add(new KeyValuePair<int, PathStep>(k.From, new PathStep { Zone = k.From, Link = k, Reverse = true, Sphere = next.Index })); }
				}
				foreach (var kv in newly)
				{
					reachable.Add(kv.Key);
					res.ZoneSphere[kv.Key] = next.Index;
					res.FirstReach[kv.Key] = kv.Value;
				}
				// placements: everything a reachable zone grants whose own requirement now holds
				bool gained = true;
				while (gained)
				{
					gained = false;
					foreach (var z in new List<int>(reachable))
						if (Grant(z, next, res)) gained = true;
				}
				if (newly.Count == 0 && next.Tokens.Count == 0) break;
				res.Spheres.Add(next);
				if (res.Spheres.Count > 1000) { res.Warnings.Add("Stopped after 1000 spheres."); break; }
			}

			res.GoalReached = reachable.Contains(goal);
			if (res.GoalReached)
			{
				var chain = new List<PathStep>();
				int z = goal;
				var guard = new HashSet<int>();
				while (true)
				{
					var step = res.FirstReach[z];
					chain.Add(step);
					if (step.Link == null || !guard.Add(z)) break;
					z = step.Reverse ? step.Link.To : step.Link.From;
				}
				chain.Reverse();
				res.Path = chain;
			}

			// blocked links out of the reachable set
			foreach (var k in zs.Links)
			{
				var r = Req(k, res);
				if (reachable.Contains(k.From) && !reachable.Contains(k.To))
				{
					var b = new BlockedLink { Link = k, FromZone = k.From, ToZone = k.To };
					r.Missing(this, b.Missing);
					if (b.Missing.Count == 0) b.Missing.Add(k.Confidence == "todo" ? "(todo link, not trusted)" : "(?)");
					res.Blocked.Add(b);
				}
				if (k.TwoWay && reachable.Contains(k.To) && !reachable.Contains(k.From))
				{
					var b = new BlockedLink { Link = k, Reverse = true, FromZone = k.To, ToZone = k.From };
					r.Missing(this, b.Missing);
					if (b.Missing.Count == 0) b.Missing.Add(k.Confidence == "todo" ? "(todo link, not trusted)" : "(?)");
					res.Blocked.Add(b);
				}
			}
			foreach (var zone in zs.Zones)
				if (zone.Id > ZoneSet.Unassigned && !reachable.Contains(zone.Id)) res.Unreached.Add(zone.Id);
			foreach (var p in zs.Placements)
			{
				string t = Requirement.Normalize(p.Grants);
				if (tokens.Contains(t) || !reachable.Contains(p.Zone) || string.IsNullOrEmpty(p.Requires)) continue;
				var r = PlacementReq(p, res);
				var missing = new List<string>();
				r.Missing(this, missing);
				res.UngrantedPlacements.Add(t + " (zone " + p.Zone + ") needs " + string.Join(" | ", missing.ToArray()));
			}

			// tokens without a source
			var needed = new List<string>();
			foreach (var k in zs.Links) Req(k, res).Tokens(needed);
			foreach (var p in zs.Placements) if (!string.IsNullOrEmpty(p.Requires)) PlacementReq(p, res).Tokens(needed);
			var granted = new HashSet<string>();
			foreach (var p in zs.Placements) granted.Add(Requirement.Normalize(p.Grants));
			foreach (var t in options.StartTokens) granted.Add(Requirement.Normalize(t));
			foreach (var t in needed)
			{
				if (t == "free" || t == "" || t == "none" || t == "true" || t == "bash" || t.StartsWith("pick:") || t.StartsWith("zone:")) continue;
				if (t.StartsWith("talismans:"))
				{
					int n;
					int have = 0;
					foreach (var g in granted) if (g.StartsWith("talisman:")) have++;
					if (int.TryParse(t.Substring(10), out n) && have < n) res.NoSource.Add(t + " (" + have + " talisman placements known)");
					continue;
				}
				if (t.StartsWith("spell:") && options.AllowSpells) continue;
				if (!granted.Contains(t)) res.NoSource.Add(t);
			}
			res.NoSource.Sort(StringComparer.Ordinal);

			foreach (var k in zs.Links)
				if (k.TwoWay && Req(k, res).ToString() == "free" && k.From != k.To)
					res.MergeCandidates.Add(new KeyValuePair<int, int>(Math.Min(k.From, k.To), Math.Max(k.From, k.To)));
			// a one-way free link each way is the same thing
			var oneWay = new Dictionary<string, ZoneLink>();
			foreach (var k in zs.Links)
				if (!k.TwoWay && Req(k, res).ToString() == "free") oneWay[k.From + ">" + k.To] = k;
			foreach (var kv in oneWay)
			{
				var k = kv.Value;
				if (oneWay.ContainsKey(k.To + ">" + k.From) && k.From < k.To)
					res.MergeCandidates.Add(new KeyValuePair<int, int>(k.From, k.To));
			}
			return res;
		}

		/// <summary>Grants the tokens of zone's placements whose requirement holds. Returns true if any was new.</summary>
		bool Grant(int zone, Sphere sphere, SolveResult res)
		{
			bool any = false;
			foreach (var p in zs.Placements)
			{
				if (p.Zone != zone) continue;
				string t = Requirement.Normalize(p.Grants);
				if (string.IsNullOrEmpty(t) || tokens.Contains(t)) continue;
				if (!PlacementReq(p, res).Eval(this)) continue;
				tokens.Add(t);
				sphere.Tokens.Add(t);
				sphere.TokenSources.Add(t + " (zone " + zone + (string.IsNullOrEmpty(p.Requires) ? "" : ", with " + p.Requires) + ")");
				res.TokenSphere[t] = sphere.Index;
				any = true;
			}
			return any;
		}
	}
}
