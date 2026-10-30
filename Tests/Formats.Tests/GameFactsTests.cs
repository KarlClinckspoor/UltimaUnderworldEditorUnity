extern alias current;
using UltimateEditor.Games;
using UltimateEditor.Modding;
using Xunit;
using ScdBlock = current::UltimateEditor.Games.ScdBlock;
using ScdRow = current::UltimateEditor.Games.ScdRow;

namespace Formats.Tests
{
	/// <summary>Facts the tools take from the game (review package 6).</summary>
	public class GameFactsTests
	{
		[Fact]
		public void A_map_note_is_on_the_tile_the_game_draws_it_on()
		{
			// uw1/src/ui/AUTOMAP.C DoTile: tile (x, y) is drawn from ((x - 1) * 3 + 10, (y - 1) * 3 + 7)
			var n = new MapNote();
			n.TileX = 1; n.TileY = 1;
			Assert.Equal(10, n.X);
			Assert.Equal(7, n.Y);
			n.TileX = 40; n.TileY = 62;
			Assert.Equal(39 * 3 + 10, n.X);
			Assert.Equal(61 * 3 + 7, n.Y);
			for (int px = 10; px < 10 + 62 * 3; px++)
			{
				n.X = px;
				Assert.Equal((px - 10) / 3 + 1, n.TileX);
			}
		}

		static ScdRow Row(int time, int ev) { var r = new ScdRow(new byte[16]); r.Time = time; r.Event = ev; return r; }

		[Fact]
		public void Editing_a_schedule_moves_the_level_clocks_with_the_rows()
		{
			// level 3 has run rows A and B (next = 2); C has not run
			var b = new ScdBlock();
			ScdRow a = Row(1, 1), bb = Row(2, 1), c = Row(5, 1);
			b.Rows.AddRange(new[] { a, bb, c });
			b.Clocks[3 * 4 + 2] = 2;
			b = ScdBlock.Parse(b.ToBytes());
			a = b.Rows[0]; bb = b.Rows[1]; c = b.Rows[2];
			Assert.Equal(2, b.ClockNext(3));
			// a row added at the start (a time already passed) and the first run row removed
			b.Rows.Insert(0, Row(0, 2));
			b.Rows.Remove(a);
			b.RemapClocks();
			Assert.Equal(2, b.ClockNext(3));     // the new row and B are passed; C is next
			Assert.Same(c, b.Rows[b.ClockNext(3)]);
			Assert.Equal(0, b.ClockNext(4));     // a clock at 0 stays at 0
		}

		[Fact]
		public void A_conversation_in_an_empty_slot_is_flagged_as_new()
		{
			var c = new ArkBlocksComponent { File = "DATA/CNV.ARK" };
			c.Blocks.Add(new ArkBlocksComponent.Block { Index = 7, Base = "absent", Data = "x" });
			c.Blocks.Add(new ArkBlocksComponent.Block { Index = 9, Base = "0123", Data = "y" });
			c.Blocks.Add(new ArkBlocksComponent.Block { Index = 11, Base = "absent", Delete = true });
			Assert.Equal(new[] { 7 }, ModClassifier.AddedConversations(c));
			var f = new ModFlags();
			f.NewConversations.Add(7);
			Assert.Equal(new[] { 7 }, ModFlags.FromJson(f.ToJson()).NewConversations);
		}
	}
}
