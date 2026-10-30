using UltimateEditor.Games;
using UltimateEditor.Modding;
using Xunit;

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
