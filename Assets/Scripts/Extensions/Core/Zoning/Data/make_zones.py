#!/usr/bin/env python3
"""Builds uw1-zones.json from Karl's zoning upload (rando.zip): the painted mapN.bmp files
give each tile's zone id; Notes.md gives the zone names and the connections between zones,
transcribed below by hand (one tuple per connection). Run:

    python3 -I make_zones.py <dir with mapN.bmp> <out.json>

The requirement language used in the links (parsed by Uw1.Zoning.Requirement in the editor):
    key:N          hold a key whose owner field is N (the lock's key number)
    bash           bashing a breakable door is allowed (solver option)
    pick:Z         picking a lock of difficulty Z is allowed (solver option: Pick Lock skill)
    item:NAME      hold the named item (granted by a placement: medallion, crystal, lava_boots ...)
    talismans:N    hold at least N different talismans (each talisman grants talisman:<name>)
    flag:NAME      a quest or event flag (granted by a placement, e.g. gazer_dead)
    npc:NAME       the NPC can be talked to / is at the gate (granted by a placement)
    spell:NAME     cast or drink/read the effect: levitate, fly, waterwalk, jump, reveal, gate_travel
    zone:N         zone N must be reachable (a lever or button on the other side)
    free           nothing needed
Operators: '&' and, '|' or, parentheses. Confidence 'todo' marks Karl's open questions.
"""
import json, struct, sys, os

# ---------------------------------------------------------------- zones: id -> (name, levels, status, notes)
ZONES = {
    1:  ("Open, not assigned yet", "", "unassigned", "Label 1 = open tile without a zone. The editor lists these."),
    53: ("Starting area", "L1", "done", "Shrine of the Silver Sapling lock was missed at first; id shoehorned in."),
    91: ("Small room behind pull chain", "L1", "done", "Breakable locked door with no key; pull chain only on the zone 53 side."),
    2:  ("Main areas (levels 1-6)", "L1-L6", "unfinished", "One big zone that wraps through the stairs of levels 1 to 6. Water below the shrine on L1 is a soft lock unless you do the jumping puzzle. L3: the two waterfalls and the SW lever/button door can all be walked around. L3 east door (key 9, unbreakable): can it be swum around to the south? L5: should Judy's key of Love be its own zone?"),
    3:  ("Grey goblin area", "L1", "unfinished", "Is the secret door breakable? The storage with the button: can you get stuck if the pole is missing?"),
    4:  ("Grey goblin storage", "L1", "done", ""),
    5:  ("Little storage with mongbat", "L1", "unfinished", "Is the door breakable?"),
    6:  ("Green goblin area", "L1", "done", ""),
    7:  ("Green goblin storage", "L1", "done", ""),
    8:  ("Skeleton room", "L1", "unfinished", "Two doors, key 2. Breakable?"),
    9:  ("Pillar height puzzle control room and treasure", "L1", "done", ""),
    10: ("Bragit's area", "L1", "done", "Lever inside opens the door to zone 2."),
    11: ("Outcasts", "L1", "done", ""),
    12: ("Mountainfolk", "L2", "done", ""),
    13: ("Mountainfolk storage", "L2", "done", "Zone 14 (second storage) was merged into this one."),
    15: ("Mountainfolk stairs", "L2", "unfinished", "Reached from L3 zone 2 with no requirement: should it be zone 2?"),
    16: ("Royal bedroom", "L2", "done", ""),
    17: ("Vault ledge", "L2", "done", "Door cannot be opened from inside; leave with the Silver Sapling or by dropping into zone 14."),
    14: ("Vault lower area", "L2", "done", ""),
    19: ("One-way passage, undertow", "L2", "done", "Not 18: colour 18 is nearly the background colour."),
    20: ("Crystal ball room with skeletons", "L2", "done", "Peers into the moonstone area."),
    21: ("Maze room with potions", "L2", "done", ""),
    22: ("Area reached by flying", "L2", "done", ""),
    23: ("Locked area of alcove", "L2", "done", ""),
    83: ("Moonorb area", "L2", "done", "Entered by moving to a specific tile; Gate Travel can also land here."),
    24: ("Alcove behind waterfall", "L3", "unfinished", "What opens it? Just walking in?"),
    25: ("Sword blade area", "L3", "done", "Trigger is in zone 2 only. Holds the broken blade."),
    26: ("Red lizardman room", "L3", "done", ""),
    27: ("Lizardmen empty storage", "L3", "done", ""),
    28: ("Iss'leek's room", "L3", "done", ""),
    29: ("Shrine with lizardmen", "L3", "done", ""),
    30: ("Storage room west", "L3", "done", ""),
    31: ("Storage room east", "L3", "done", ""),
    32: ("Secret bandit room", "L3", "unfinished", "Behind an unbreakable secret door: what opens it?"),
    33: ("Corridor with skeletons", "L3", "done", ""),
    34: ("Murgo's prison", "L3", "done", ""),
    35: ("Lizardmen SW quarters", "L3", "done", ""),
    36: ("Lizardmen SE quarters", "L3", "done", ""),
    37: ("Lizardmen NE quarters", "L3", "done", ""),
    38: ("Lizardmen N quarters", "L3", "done", ""),
    39: ("Lizardmen NW quarters", "L3", "done", ""),
    66: ("Way up to the key of Courage (levels 3-7)", "L3-L7", "done", "Swamp with the key room on L3, wisps on L4, trap corridor on L5, lava corridor on L6, chasms on L7."),
    67: ("Shadow beasts", "L3", "done", "Holds key 10."),
    68: ("Key of Courage room", "L3", "done", ""),
    40: ("Rawstag's ankh shrine", "L4", "done", ""),
    41: ("Alcove after the lever puzzle (E)", "L4", "done", ""),
    43: ("Bullfrog exit SE", "L4", "done", "The NE exit leads to the tombs, which zone 2 reaches, so that corridor is zone 2 (old zone 42 removed)."),
    44: ("Maze west, Rodrick", "L4", "done", ""),
    45: ("Plate secret area", "L4", "done", ""),
    46: ("Waterfall area", "L4", "done", ""),
    47: ("Waterfall area, north step", "L4", "done", ""),
    48: ("Knight armoury", "L4", "done", ""),
    49: ("Zanium mine dispatch corridor", "L5", "done", "Open unless Kneenibble's code is needed; the teleport sits behind an unbreakable portcullis."),
    50: ("Zanium mine", "L5", "done", ""),
    51: ("Nolant tomb vestibule", "L5", "done", "A trigger summons a ghost carrying key 15."),
    52: ("Nolant tomb", "L5", "done", "Painted as 50 in map4.bmp; relabelled 52 by the importer (RELABEL)."),
    54: ("Lava south", "L6", "done", ""),
    55: ("Lava north", "L6", "done", ""),
    56: ("Small alcove on lava, east", "L6", "done", ""),
    57: ("Alcove with crown near bridge", "L6", "unfinished", "Reachable from zone 2 with a good jump? To test."),
    58: ("Alcove with shield near crown", "L6", "done", ""),
    59: ("Guard room 1", "L7", "unfinished", "Can the lever reopen the way back to zone 2?"),
    60: ("Spider maze (L7) and its L8 pockets", "L7-L8", "done", "Keys 24 and 25 are here. L8: sectioned-off mine (12,33) and the orb rock area (60,62)."),
    61: ("Guard room 2", "L7", "done", "Key 23 on the troll. Entering gets you imprisoned."),
    62: ("Prison foyer", "L7", "done", ""),
    63: ("Prison corridor", "L7", "done", ""),
    64: ("Prison cells, key 23", "L7", "done", "Fintor, Griffle, Kallistan, Dantes. Kallistan's crystal needs the password Deco Morono."),
    65: ("Prison cells, key 27", "L7", "done", "Smonden (key 26, way up to Courage), Bolinard (Tom's picture, for Love), Gurstang (phrase for Illomo, for Truth)."),
    92: ("Alcove with the Gate Travel scroll", "L7", "done", ""),
    93: ("Cavern of dead things", "L7", "done", ""),
    69: ("Entrance to the damaging maze", "L7", "done", ""),
    70: ("Damaging maze", "L7", "done", "Crown of maze navigation (zone 78) makes it safe; the route can be memorised."),
    71: ("Tyball's crib", "L7", "unfinished", "Secret wall to zone 2: opened by Reveal? Tyball has keys 23 and 27."),
    72: ("Arial's prison", "L7", "unpainted", "Not painted in map6.bmp."),
    73: ("Corridor to the stairs into main L8 (L7-L8)", "L7-L8", "done", ""),
    74: ("Guard room 3", "L7", "done", ""),
    75: ("Tombs part 1", "L7", "unfinished", "Rocks fall behind you, cutting off zone 60: add an intermediate zone?"),
    76: ("Tombs part 2", "L7", "unfinished", "Will Reveal open the secret doors?"),
    77: ("Puke corridor", "L7", "unfinished", ""),
    78: ("Passage to the imp (L7-L8)", "L7-L8", "done", "Crown of maze navigation."),
    79: ("Northern lava river", "L7", "done", "Lava floor, many fire elementals."),
    80: ("Alcoves with the imp", "L7", "done", ""),
    81: ("Ring of levitation and misc steps", "L7", "done", ""),
    82: ("Top of waterfall", "L7", "done", "One square."),
    94: ("Carasso", "L8", "done", "Key 28."),
    84: ("Corridors to the central chamber", "L8", "done", ""),
    85: ("Lava and connected regions", "L8", "done", "Lava boots; many fire elementals."),
    90: ("Slasher chamber", "L8", "done", "Not 86: the LUT gives 86 the same colour as 2."),
    88: ("Small lava eastern section", "L8", "done", ""),
    89: ("Lava corridors, western section", "L8", "done", ""),
    87: ("Final stretch (L9)", "L9", "unpainted", "No way back. map8 not painted yet."),
}

# ---------------------------------------------------------------- links
# (from, to, kind, requirement, two_way, note, confidence)
# kind: walk door portcullis secret doorman drop climb teleport lever stairs other
W = lambda *a: a
LINKS = [
    # Level 1
    W(53, 91, "door", "zone:53", False, "Pull chain in zone 53 opens a locked breakable door with no key; no chain on the 91 side.", "certain"),
    W(91, 53, "door", "bash", False, "Back through the same door: breakable, locked, no key.", "certain"),
    W(53, 10, "door", "key:1 | pick:6 | bash", True, "Door at L1 (22,5) near Bragit: key 1, pick 6, breakable (class 1). Key 1 is in the backpack near Bragit (zone 53).", "certain"),
    W(91, 2, "door", "key:1 | pick:6 | bash", True, "Doors at L1 (36,8) and (37,12): key 1, pick 6, breakable (class 1).", "certain"),
    W(2, 3, "doorman", "npc:eb | key:3 | pick:4", False, "Portcullis at L1 (47,51): key 3, pick 4, unbreakable; Eb opens it.", "certain"),
    W(3, 2, "lever", "free", False, "Button on the wall by the portcullis.", "certain"),
    W(2, 5, "door", "key:2 | pick:2 | bash", True, "Door at L1 (9,19): key 2, pick 2, breakable (class 2).", "certain"),
    W(2, 6, "doorman", "npc:drog | key:4 | pick:5", True, "Portcullis at L1 (15,34): key 4, pick 5, unbreakable; Drog opens it.", "certain"),
    W(2, 8, "door", "key:2 | pick:2 | bash", True, "Doors at L1 (52,36) and (56,36): key 2, pick 2, breakable (class 2).", "certain"),
    W(2, 9, "door", "key:2 | pick:0 | bash", False, "Door at L1 (55,46): lock key 2, pick difficulty 0, breakable (class 2). The notes said key 3 in one place; the lock says 2.", "certain"),
    W(9, 2, "drop", "free", False, "Falling down.", "certain"),
    W(2, 10, "door", "bash | pick:1 | zone:10", False, "Door at L1 (9,10) near the water: no key, pick 1, breakable (class 0); opened by a lever inside zone 10.", "certain"),
    W(10, 2, "lever", "free", False, "The lever inside zone 10 opens the door; or drop from the viewing area.", "certain"),
    W(2, 10, "climb", "spell:levitate", False, "Levitating up to the viewing area.", "certain"),
    W(3, 4, "door", "key:3 | pick:5 | bash", True, "Door at L1 (55,59): key 3, pick 5, breakable (class 1). Bashing angers the goblins.", "certain"),
    W(3, 2, "secret", "key:3 | pick:1 | bash", False, "Secret door at L1 (54,47): key 3, pick 1, breakable (class 2).", "certain"),
    W(3, 2, "stairs", "free", False, "Stairs down to L2 (47,53), the fountain with the lurker and grey goblins: zone 2. One way.", "certain"),
    W(6, 7, "door", "key:4 | pick:6 | bash", True, "Door at L1 (4,49): key 4, pick 6, breakable (class 1). Drog and Vernix each carry a key 4; bashing angers the goblins.", "certain"),
    W(10, 11, "door", "key:1 | pick:1 | bash", True, "Door at L1 (21,15): key 1, pick 1, breakable (class 0).", "certain"),
    # Level 2
    W(2, 12, "doorman", "npc:brawnclan | pick:6", False, "Portcullis at L2 (13,30): its lock has no key (key 0), pick difficulty 6; Brawnclan opens it, button on the inside. The notes said key 6.", "certain"),
    W(12, 2, "lever", "free", False, "Button on the wall opens the portcullis from inside.", "certain"),
    W(2, 12, "door", "key:5 | pick:6", True, "Massive door at L2 (30,13): key 5, pick 6, unbreakable. An unlocked breakable door stands two tiles south at (30,15) between the same zones: check the painting there.", "todo"),
    W(2, 19, "walk", "free", False, "One-way passage (undertow) leading to L3 (25,10).", "certain"),
    W(19, 2, "stairs", "free", False, "Comes out on L3 south, in the water near the bones: zone 2.", "certain"),
    W(2, 20, "climb", "spell:levitate | item:pole | pick:3", False, "Float up the viewing area, or trigger the lever with a pole (?). Portcullis at L2 (54,41): no key, pick difficulty 3.", "todo"),
    W(20, 2, "lever", "free", False, "Lever opens the portcullis; or drop down the viewing area.", "certain"),
    W(2, 21, "door", "key:6 | pick:14 | bash", True, "Door at L2 (15,51): key 6, pick 14 (skill 31), breakable (class 2).", "certain"),
    W(2, 22, "climb", "spell:levitate", False, "Flying or levitating only.", "certain"),
    W(22, 2, "drop", "free", False, "Falling down with the headlesses.", "certain"),
    W(2, 83, "teleport", "free", True, "Moving onto a specific tile; Gate Travel can also land here.", "certain"),
    W(12, 13, "door", "key:5 | pick:5 | bash", True, "Doors at L2 (17,26) and (23,26): key 5, pick 5, breakable (class 0); nobody gets angry.", "certain"),
    W(12, 15, "door", "key:5 | pick:7 | bash", True, "Door at L2 (7,5): key 5, pick 7, breakable (class 2); nobody gets angry.", "certain"),
    W(12, 16, "door", "key:5 | pick:4 | bash", True, "Door at L2 (5,10): key 5, pick 4, breakable (class 2); nobody gets angry.", "certain"),
    W(12, 17, "doorman", "flag:deco_morono | pick:10", False, "Massive door at L2 (11,13): no key, pick difficulty 10, unbreakable. Password Deco Morono to Steeltoe; cannot be opened from inside.", "certain"),
    W(17, 14, "drop", "free", False, "Falling down into the vault.", "certain"),
    W(14, 17, "climb", "spell:levitate", False, "Flying or levitation.", "certain"),
    W(15, 2, "stairs", "free", True, "Stairs to L3 (5,2), SW area with the poison floor: zone 2. If this is free both ways, zone 15 is zone 2.", "todo"),
    W(22, 23, "door", "key:6 | pick:14 | bash", True, "Door at L2 (21,37): key 6, pick 14, breakable (class 2).", "certain"),
    # Level 3
    W(2, 24, "walk", "free", False, "Alcove behind the waterfall: what is the requirement?", "todo"),
    W(2, 25, "lever", "free", False, "Remove the vines and pull the lever (trigger only in zone 2). Holds the broken blade.", "certain"),
    W(2, 26, "door", "key:9 | pick:5 | bash", True, "Door at L3 (46,29): key 9, pick 5, breakable (class 2).", "certain"),
    W(2, 27, "door", "key:9 | pick:5", True, "Door at L3 (45,25): key 9, pick 5, unbreakable.", "certain"),
    W(2, 28, "door", "key:9 | pick:5 | bash", True, "Door at L3 (42,27): key 9, pick 5, breakable (class 2).", "certain"),
    W(2, 29, "door", "key:9 | pick:5", True, "Door at L3 (42,25): key 9, pick 5, unbreakable.", "certain"),
    W(2, 30, "door", "key:8 | pick:6 | bash", True, "Door at L3 (30,7): key 8, pick 6, breakable (class 2).", "certain"),
    W(2, 31, "door", "key:8 | pick:4", True, "Door at L3 (34,7): key 8, pick 4, unbreakable.", "certain"),
    W(31, 32, "secret", "free", True, "Secret door at L3 (37,10): no lock, so using it opens it from either side (UseRect/OpenDoor). Reveal does not act on doors.", "certain"),
    W(2, 33, "door", "bash | pick:4", True, "Door at L3 (15,26): locked, no key, pick difficulty 4, breakable.", "certain"),
    W(2, 34, "doorman", "flag:lizard_friend | pick:13", True, "Portcullis at L3 (27,22): no key, pick difficulty 13. Sseetharee's conversation opens exactly this door (gronk_door 27,22) once quest 6 is set; quest 6 is set when whoami 0x18 (the 'prisoner', Murgo) dies. See the report.", "certain"),
    W(2, 35, "door", "key:11 | pick:5 | bash", True, "Door at L3 (31,21): key 11, pick 5, breakable (class 1).", "certain"),
    W(2, 36, "door", "key:11 | pick:5", True, "Door at L3 (33,21): key 11, pick 5, unbreakable.", "certain"),
    W(2, 37, "door", "key:11 | pick:5", True, "Door at L3 (33,26): key 11, pick 5, unbreakable.", "certain"),
    W(2, 38, "door", "key:11 | pick:5", True, "Door at L3 (29,26): key 11, pick 5, unbreakable.", "certain"),
    W(2, 39, "door", "key:11 | pick:5", True, "Door at L3 (25,26): key 11, pick 5, unbreakable.", "certain"),
    W(66, 67, "door", "bash | pick:6", True, "Door at L3 (4,55): locked, no key, pick 6, breakable.", "certain"),
    W(66, 68, "door", "key:10 | pick:13", True, "Massive door at L3 (10,55): key 10, pick 13, unbreakable.", "certain"),
    # Level 4
    W(2, 40, "doorman", "npc:rawstag | pick:14", False, "Massive door at L4 (61,5): no key, pick difficulty 14 (skill 31). Rawstag doorman; cannot be opened from inside.", "certain"),
    W(2, 41, "lever", "item:pole | spell:levitate | spell:jump", False, "Lever puzzle (E): pole, levitation or a jump? One way.", "todo"),
    W(2, 43, "walk", "spell:levitate | flag:bullfrog_solved", False, "Bullfrog puzzle (wand 555 resets it) or levitation/flying.", "certain"),
    W(43, 2, "drop", "free", False, "Falling down to the bullfrog.", "certain"),
    W(2, 44, "door", "key:13", True, "Unbreakable massive door, key 13 (Rodrick).", "certain"),
    W(44, 45, "lever", "free", False, "Lever puzzle; massive unbreakable door, no way back from 45.", "certain"),
    W(2, 46, "climb", "spell:levitate | (spell:waterwalk & spell:jump)", False, "Levitation or water walking plus jumping.", "certain"),
    W(46, 2, "drop", "free", False, "Falling down.", "certain"),
    W(2, 47, "climb", "spell:levitate | (spell:waterwalk & spell:jump)", False, "Levitation or water walking plus jumping.", "certain"),
    W(47, 2, "drop", "free", False, "Falling down.", "certain"),
    W(47, 46, "climb", "spell:levitate | (spell:waterwalk & spell:jump)", False, "", "certain"),
    W(46, 47, "drop", "free", False, "Falling down.", "certain"),
    W(2, 48, "doorman", "flag:crux_writ | pick:14", False, "Massive door at L4 (3,10): no key, pick difficulty 14. Dorna's conversation opens exactly this door (gronk_door 3,10) in the Knights' quest (quest byte 32).", "certain"),
    W(48, 2, "lever", "free", False, "Lever inside.", "certain"),
    # Level 5
    W(2, 49, "walk", "flag:kneenibble_code | free", True, "Needs Kneenibble's code, otherwise free; the teleport is behind an unbreakable portcullis.", "todo"),
    W(49, 50, "teleport", "free", False, "One-way teleport into the zanium mine.", "certain"),
    W(50, 2, "teleport", "free", False, "One-way teleport out.", "certain"),
    W(2, 51, "lever", "free", False, "Lever opens the way; back through the portcullis needs key 15.", "certain"),
    W(52, 2, "portcullis", "key:15", True, "Portcullis at L5 (57,2) between the tomb and zone 2: lock key 15, unpickable; stored open in LEV.ARK (a trap must shut it). The notes put this portcullis on zone 51.", "todo"),
    W(51, 52, "door", "key:15", True, "Massive door, key 15 (the ghost summoned in 51 carries it).", "certain"),
    # Level 6
    W(2, 54, "walk", "free", True, "Ramp to the south / stairs back up.", "certain"),
    W(2, 55, "walk", "free", True, "Several entrances and exits.", "certain"),
    W(2, 56, "drop", "free", False, "Falling from the square room with lava walls and gold floor.", "certain"),
    W(56, 2, "climb", "spell:levitate", False, "Floating up.", "certain"),
    W(2, 57, "climb", "spell:jump", True, "A good jump? To test.", "todo"),
    W(2, 58, "climb", "spell:levitate", True, "Levitation; drop back into 55.", "certain"),
    W(55, 56, "climb", "free", True, "Jumping up a little / dropping back.", "certain"),
    W(55, 57, "climb", "spell:jump | spell:levitate", False, "Jump spell (to test) or floating.", "todo"),
    W(57, 55, "drop", "free", False, "Falling down.", "certain"),
    W(55, 58, "climb", "spell:levitate", False, "Levitating up.", "certain"),
    W(58, 55, "drop", "free", False, "Falling down.", "certain"),
    # Level 7
    W(2, 59, "portcullis", "item:medallion | flag:fought_guards", False, "Indestructible portcullis; show the medallion or fight everyone.", "certain"),
    W(59, 2, "lever", "free", False, "Possibly by a lever; the editor does not show a way back.", "todo"),
    W(2, 71, "secret", "free", True, "Secret door at L7 (42,10): no lock, breakable. Using it opens it from either side; Reveal does not act on doors. The notes asked whether Reveal opens it.", "certain"),
    W(2, 69, "door", "key:24 | pick:8 | bash", True, "Door at L7 (30,20): key 24, pick 8, breakable.", "certain"),
    W(2, 75, "secret", "free", True, "Secret door at L7 (18,29): no lock. Using it opens it from either side.", "certain"),
    W(2, 82, "climb", "spell:levitate", False, "Levitation or flying.", "certain"),
    W(82, 2, "drop", "free", False, "Falling down.", "certain"),
    W(59, 60, "doorman", "item:medallion | flag:fought_guards", True, "Talk to the goblin with the medallion, or fight through; key 23 also works.", "certain"),
    W(60, 61, "doorman", "item:medallion | flag:fought_guards | key:23 | pick:13", True, "Portcullis at L7 (27,54) key 23 pick 13 and massive door at (29,57) key 23 unpickable.", "certain"),
    W(60, 63, "secret", "zone:64", False, "Opened from Dantes' cell (zone 64).", "certain"),
    W(60, 92, "climb", "spell:jump | spell:levitate", False, "Jump or levitate.", "certain"),
    W(92, 60, "drop", "free", False, "", "certain"),
    W(60, 79, "drop", "free", False, "Falling down to the lava river.", "certain"),
    W(60, 75, "secret", "item:crystal", False, "Kallistan's crystal opens the way to the tombs (a change-terrain trap, not a door).", "certain"),
    W(60, 93, "door", "bash", True, "Door at L7 (52,54): locked, no key, unpickable (z 15), breakable (class 2).", "certain"),
    W(61, 62, "door", "key:23", True, "Portcullis at L7 (24,56): key 23, unpickable (stored open).", "certain"),
    W(62, 63, "portcullis", "key:23 | pick:12", True, "Portcullis at L7 (21,56): key 23, pick 12.", "certain"),
    W(63, 64, "door", "key:23 | pick:1 | bash", True, "Cell doors (L7 (9,52), (13,54), (15,54), (19,57), (8,56)): key 23, pick 0-1, breakable (class 0).", "certain"),
    W(63, 65, "door", "key:27", True, "Three cells with unbreakable doors, key 27 (Tyball).", "certain"),
    W(64, 60, "walk", "free", False, "Hole through Dantes' room; makes the troll guard hostile.", "certain"),
    W(60, 66, "door", "key:26", True, "Over the chasms, unbreakable door, key 26 (Smonden).", "certain"),
    W(93, 69, "door", "bash", True, "Door at L7 (41,42): locked, no key, unpickable, breakable (class 2).", "certain"),
    W(69, 70, "walk", "item:crown_maze | flag:maze_memorised", True, "The damaging maze: crown of maze navigation, or memorise the route.", "certain"),
    W(69, 78, "secret", "free", True, "Secret door at L7 (36,35): no lock. Using it opens it from either side.", "certain"),
    W(69, 81, "drop", "free", True, "Falling down; back up by levitation.", "certain"),
    W(81, 69, "climb", "spell:levitate", False, "", "certain"),
    W(69, 79, "drop", "free", False, "", "certain"),
    W(79, 69, "climb", "spell:levitate", False, "Floating up.", "certain"),
    W(70, 79, "drop", "free", False, "", "certain"),
    W(70, 71, "walk", "free", True, "Just walk.", "certain"),
    W(71, 72, "portcullis", "key:27", True, "Unbreakable portcullis, key 27.", "certain"),
    W(71, 73, "door", "key:27", True, "Unbreakable door, key 27.", "certain"),
    W(74, 2, "doorman", "key:23 | pick:13 | item:medallion | flag:fought_guards", True, "Guard room 3: massive door at L7 (19,4) key 23 pick 13 from the west, the portcullis at (22,5) (no lock, guards open it) from the east, or fighting.", "certain"),
    W(75, 76, "secret", "free", True, "Secret door on the NE tomb.", "certain"),
    W(76, 77, "secret", "free", True, "Secret door.", "certain"),
    W(77, 78, "door", "key:25 | bash", True, "Door at L7 (36,49): key 25, unpickable, breakable (class 2).", "certain"),
    W(78, 79, "door", "key:25 | bash", True, "Door at L7 (35,53): key 25, unpickable, breakable (class 2).", "certain"),
    W(78, 80, "climb", "spell:levitate", False, "Levitating or flying up to the imp's alcoves.", "certain"),
    W(80, 78, "drop", "free", False, "", "certain"),
    W(79, 81, "climb", "spell:jump | spell:levitate", False, "Jumping up or levitation.", "certain"),
    W(81, 79, "drop", "free", False, "", "certain"),
    W(79, 60, "climb", "spell:levitate | free", False, "Floating up, or climbing up at the north-west.", "todo"),
    # Level 8
    W(60, 89, "walk", "free", True, "", "certain"),
    W(60, 88, "walk", "item:lava_boots", True, "Lava.", "certain"),
    W(73, 94, "door", "bash | pick:10", True, "Door at L8 (47,42): locked, no key, pick 10, breakable (class 1).", "certain"),
    W(73, 84, "door", "key:28 | spell:jump", True, "Massive door at L8 (54,32): key 28 (Carasso), unpickable, unbreakable; perhaps a jump from the southern bit.", "todo"),
    W(84, 85, "drop", "free", False, "Falling down.", "certain"),
    W(85, 84, "climb", "item:lava_boots", False, "Going up (ramps).", "certain"),
    W(85, 73, "walk", "item:lava_boots", True, "Many ways: ramps, flying.", "certain"),
    W(84, 90, "door", "item:tripartite_key", False, "Keys of Truth, Love and Courage assembled.", "certain"),
    W(90, 84, "door", "free", False, "Once opened.", "certain"),
    W(90, 87, "walk", "talismans:8 & flag:talismans_quest", False, "All eight talismans thrown into the lava after talking to Garamon.", "certain"),
]

# ---------------------------------------------------------------- placements: what a zone gives you
# (zone, token, note). Keys are also found automatically from LEV.ARK by the editor's audit.
PLACEMENTS = [
    (53, "key:1", "Backpack close to Bragit."),
    (2, "key:2", "L1: SE close to the acid slug; and on the spiral stairs NW."),
    (3, "key:3", "Grey goblin Retichall."),
    (6, "key:4", "Drog and Vernix."),
    (2, "key:5", "L2, near the stairs south of the central shaft."),
    (2, "key:6", "L2, after the zigzag maze with the damaging floor."),
    (2, "npc:eb", "Grey goblin doorman Eb stands at the portcullis."),
    (2, "npc:drog", "Green goblin doorman Drog."),
    (2, "npc:brawnclan", "Mountainman doorman Brawnclan."),
    (2, "npc:rawstag", "Rawstag, L4."),
    (2, "key:8", "L3 (37,36), in a pouch (audit)."),
    (2, "key:9", "L3 (35,40), in a sack (audit)."),
    (33, "key:11", "L3 (8,26), in a pouch in the corridor with skeletons (zone 33), per the audit of LEV.ARK."),
    (67, "key:10", "Shadow beasts."),
    (2, "key:13", "Rodrick, L4 (32,61), stands in zone 2 by the maze door (audit)."),
    (52, "key:15", "The ghost (create-object trap template at L5 (53,3), in the tomb) carries it; the notes put the summoning trigger in the vestibule."),
    (2, "item:pole", "Poles lie around on L1-L2 (goblin storage)."),
    (2, "talisman:cup", "Cup of Wonder, L3 (needs the flute and the mantra)."),
    (2, "talisman:taper", "Zak has the taper, L3 (47,38)."),
    (2, "talisman:standard", "Standard of Honor, carried by Dorna Ironfist, L4 (8,4) (audit)."),
    (2, "talisman:sword_needs_blade_and_hilt", "placeholder removed below"),
    (68, "talisman:key_of_courage_is_not_a_talisman", "placeholder removed below"),
    (25, "item:broken_blade", "Sword blade area."),
    (25, "talisman:sword", "Counts the Sword of Justice here for the solver: the blade (zone 25) and the hilt (L5 tombs, zone 2) are reforged at an anvil in zone 2."),
    (2, "talisman:ring", "Ring of Humility: created by a trap at L5 (16,59) (audit)."),
    (2, "item:broken_hilt", "Sword hilt in the tombs, L5."),
    (2, "talisman:book", "Book of Honesty, L6."),
    (2, "talisman:shield", "Shield of Valor with the golem, L6."),
    (2, "talisman:wine", "Wine of Compassion, L6."),
    (2, "item:medallion", "L7 (43,13) on the floor; Tyball carries another (audit)."),
    (60, "key:24", "Below a skull near the cavern of dead things."),
    (60, "key:25", "Near a spider den."),
    (61, "key:23", "On the troll."),
    (64, "item:crystal", "Kallistan, with the password Deco Morono."),
    (65, "key:26", "Smonden."),
    (65, "item:picture_tom", "Bolinard."),
    (65, "flag:gurstang_phrase", "Gurstang's phrase for Illomo (key of Truth)."),
    (71, "key:23", "Tyball."),
    (71, "key:27", "Tyball."),
    (78, "item:crown_maze", "Crown of maze navigation."),
    (94, "key:28", "Carasso."),
    (92, "item:scroll_gate_travel", "Gate Travel scroll."),
    (81, "item:ring_levitation", "Ring of levitation."),
    (68, "item:key_of_courage", "Key of Courage."),
]
PLACEMENTS = [p for p in PLACEMENTS if "placeholder" not in p[2]]

# Flags the notes leave without a source. The solver reports them as 'no source'.
OPEN_SOURCES = {
    "flag:deco_morono": "Where is the password learned? Not in the notes.",
    "flag:lizard_friend": "Quest 6: set by the game when the creature with whoami 0x18 ('prisoner', Murgo) dies; Sseetharee's conversation reads it. Alternative routes to Murgo's prison are not in the notes.",
    "flag:crux_writ": "Knights' quest (quest byte 32 reaching 3/4): find the writ of Lorne; Dorna opens the armoury.",
    "flag:fought_guards": "Fighting through the L7 guard posts instead of showing the medallion.",
    "flag:bullfrog_solved": "Solving the bullfrog puzzle (wand 555 resets it).",
    "flag:maze_memorised": "Walking the damaging maze from memory.",
    "flag:kneenibble_code": "Kneenibble's code for the zanium mine (notes say the corridor is otherwise free).",
    "flag:talismans_quest": "Quest 5: Garamon, at his grave with the incense/figurine, asks for the talismans.",
    "item:tripartite_key": "Keys of Truth (Illomo, after Gurstang's phrase), Love (Judy, after Tom's picture) and Courage (zone 68).",
    "item:lava_boots": "Dragon skin boots: where found? Not in the notes.",
    "talismans:8": "sword (blade from zone 25 + hilt from L5 tombs, reforged at the anvil), shield, ring, taper, cup, wine, book, standard (Dorna). The sword counts only once reforged.",
}

def read_bmp_labels(path):
    d = open(path, 'rb').read()
    off = struct.unpack_from('<I', d, 10)[0]
    w, h = struct.unpack_from('<ii', d, 18)
    assert w == 64 and abs(h) == 64, path
    px = d[off:off + 4096]
    rows = [px[r * 64:(r + 1) * 64] for r in range(64)]
    if h < 0:
        rows = rows[::-1]
    # stored bottom-up: row r = game tile y = r (checked against LEV.ARK: solid <-> 0 on every level)
    return rows

# Corrections to the painted maps, found by the editor's audit against LEV.ARK:
# (level, x0, x1, y0, y1, from, to, why)
RELABEL = [
    (5, 51, 57, 2, 4, 50, 52, "Nolant tomb painted with the zanium mine's id 50; the massive door at (53,5) joins it to the vestibule 51 with key 15, as the notes say for zone 52."),
]

def main(src, out):
    levels = []
    for lvl in range(9):
        p = os.path.join(src, 'map%d.bmp' % lvl)
        if os.path.exists(p):
            rows = [bytearray(r) for r in read_bmp_labels(p)]
            for (L, x0, x1, y0, y1, a, b, why) in RELABEL:
                if L == lvl + 1:
                    for y in range(y0, y1 + 1):
                        for x in range(x0, x1 + 1):
                            if rows[y][x] == a: rows[y][x] = b
            levels.append([''.join('%02X' % v for v in row) for row in rows])
        else:
            levels.append(None)
    zones = []
    for zid, (name, lv, status, notes) in sorted(ZONES.items()):
        zones.append({"id": zid, "name": name, "levels": lv, "status": status, "notes": notes})
    links = []
    for i, (a, b, kind, req, two, note, conf) in enumerate(LINKS):
        links.append({"id": i + 1, "from": a, "to": b, "kind": kind, "requires": req, "twoWay": two, "notes": note, "confidence": conf})
    places = [{"zone": z, "grants": t, "notes": n} for z, t, n in PLACEMENTS]
    doc = {
        "format": "uw1-zones",
        "version": 1,
        "game": "UW1",
        "source": "Karl's zoning (Notes.md, 2024-12-07) and painted maps; transcribed 2026-10-24",
        "start": {"zone": 53, "level": 1},
        "goal": {"zone": 87},
        "requirementLanguage": __doc__.split('editor):')[1].split('Operators')[0].strip().splitlines(),
        "zones": zones,
        "links": links,
        "placements": places,
        "openSources": OPEN_SOURCES,
        "tiles": {"encoding": "per level: 64 rows (y = 0..63), each 64 zone ids as two hex digits (x = 0..63); 00 solid, 01 open and unassigned",
                  "levels": levels},
    }
    with open(out, 'w') as f:
        json.dump(doc, f, indent=1)
    print("zones", len(zones), "links", len(links), "placements", len(places), "levels painted", sum(1 for l in levels if l))

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
