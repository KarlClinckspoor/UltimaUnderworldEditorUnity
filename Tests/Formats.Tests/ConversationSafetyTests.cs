extern alias current;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;
using Xunit;
using C = current::DataReader;
using CMap = current::MapCreator;
using Conv = current::Conversation;
using ConvState = current::ConversationState;
using Converter = current::ConversationConverter;
using Node = current::ConversationNode;

namespace Formats.Tests
{
	/// <summary>
	/// The conversation editor changes a conversation only when a conversion succeeds, and
	/// deleting a conversation's strings leaves the other blocks findable (review package 2).
	/// </summary>
	public class ConversationSafetyTests
	{
		static current::StringData LoadStrings(string game)
		{
			string dir = GameData.Copy(game);
			Application.dataPath = Path.Combine(dir, "unity-data");
			C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			typeof(C).GetField("uiManager", BindingFlags.NonPublic | BindingFlags.Static)
				.SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(current::UIManager)));
			current::LevArk.Game = GameProfile.For(game == "UW2" ? GameKind.UW2 : GameKind.UW1);
			var s = C.LoadStrings();
			typeof(CMap).GetProperty("StringData").SetValue(null, s);
			return s;
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Removing_a_block_keeps_every_other_block_findable(string game)
		{
			if (!GameData.Has(game)) return;
			var sd = LoadStrings(game);
			var conv = sd.Blocks.Where(b => b.BlockNumber >= 0x0E00).Select(b => b.BlockNumber).ToList();
			Assert.True(conv.Count > 3);
			int removed = conv[1];
			sd.RemoveBlock(removed);
			foreach (var b in sd.Blocks)
				Assert.Same(b, sd.Blocks[sd.BlockDictionary[b.BlockNumber]]);
			// converting the next conversation replaces its own block, not the one after it
			int next = conv[2], after = conv[3];
			var afterBlock = sd.Blocks[sd.BlockDictionary[after]];
			var block = sd.AddNewBlock(next - 0x0E00);
			Assert.Same(block, sd.Blocks[sd.BlockDictionary[next]]);
			Assert.Same(afterBlock, sd.Blocks[sd.BlockDictionary[after]]);
			Assert.Equal(sd.Blocks.Count, sd.Blocks.Select(b => b.BlockNumber).Distinct().Count());
		}

		static Node MakeNode(string content)
		{
			var go = new GameObject("node");
			var node = go.AddComponent<Node>();
			node.NodeContent = new GameObject("content").AddComponent<InputField>();
			node.NodeContent.text = content;
			node.ConditionsGO = new GameObject("conditions");
			return node;
		}

		[GameFact("UW")]
		public void A_failed_conversion_leaves_the_conversation_and_its_strings_as_they_were()
		{
			var sd = LoadStrings("UW");
			int slot = 1;
			int blockNumber = StringBlock.GetConversationBlock(slot);
			var blockBefore = sd.Blocks[sd.BlockDictionary[blockNumber]];
			string[] stringsBefore = blockBefore.Strings.ToArray();
			string nameBefore = sd.GetNPCName(slot);
			int blocksBefore = sd.Blocks.Count;

			var c = new Conv(ConvState.Converted) { Slot = slot, MemorySlots = 40, FirstMemorySlot = 31, StringBlock = blockNumber, CodeSize = 3, Code = new List<int> { 1, 2, 3 }, Unk00 = 2088 };
			var node = MakeNode("this is not a statement +");
			var ex = Record.Exception(() => Converter.Convert(c, node, new List<Node> { node }, new List<string> { "a", "b" }, "Somebody"));
			Assert.NotNull(ex);

			Assert.Equal(40, c.MemorySlots);
			Assert.Equal(31, c.FirstMemorySlot);
			Assert.Equal(3, c.CodeSize);
			Assert.Equal(new List<int> { 1, 2, 3 }, c.Code);
			Assert.Equal(ConvState.Converted, c.State);
			Assert.Same(blockBefore, sd.Blocks[sd.BlockDictionary[blockNumber]]);
			Assert.Equal(stringsBefore, blockBefore.Strings);
			Assert.Equal(nameBefore, sd.GetNPCName(slot));
			Assert.Equal(blocksBefore, sd.Blocks.Count);

			// a slot with no strings yet: the block a failed conversion added is removed again
			int empty = Enumerable.Range(1, 255).First(i => !sd.BlockDictionary.ContainsKey(StringBlock.GetConversationBlock(i)));
			var fresh = new Conv(ConvState.Unconverted) { Slot = empty };
			Assert.NotNull(Record.Exception(() => Converter.Convert(fresh, node, new List<Node> { node }, new List<string>(), "Nobody")));
			Assert.False(sd.BlockDictionary.ContainsKey(StringBlock.GetConversationBlock(empty)));
			Assert.Equal(blocksBefore, sd.Blocks.Count);
			Assert.Equal(ConvState.Unconverted, fresh.State);
		}
	}
}
