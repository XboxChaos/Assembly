using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;

namespace Assembly.Metro.Controls.PageTemplates
{
	/// <summary>
	///     Interaction logic for MapNames.xaml
	/// </summary>
	public partial class MapNames : UserControl
	{
		public MapNames()
		{
			InitializeComponent();

			listH2c.ItemsSource = Halo2.Where(e => e.Type == MapType.Campaign);
			listH2m.ItemsSource = Halo2.Where(e => e.Type == MapType.Multiplayer);

			listH3c.ItemsSource = Halo3.Where(e => e.Type == MapType.Campaign);
			listH3m.ItemsSource = Halo3.Where(e => e.Type == MapType.Multiplayer);

			listOdstc.ItemsSource = HaloODST.Where(e => e.Type == MapType.Campaign);
			listOdsts.ItemsSource = HaloODST.Where(e => e.Type == MapType.Survival);

			listReachc.ItemsSource = HaloReach.Where(e => e.Type == MapType.Campaign);
			listReachs.ItemsSource = HaloReach.Where(e => e.Type == MapType.Survival);
			listReachm.ItemsSource = HaloReach.Where(e => e.Type == MapType.Multiplayer);

			listH4c.ItemsSource = Halo4.Where(e => e.Type == MapType.Campaign);
			listH4s.ItemsSource = Halo4.Where(e => e.Type == MapType.Survival);
			listH4m.ItemsSource = Halo4.Where(e => e.Type == MapType.Multiplayer);
		}

		private enum MapType
		{
			Campaign = 0,
			Multiplayer = 1,
			Survival = 2,
		}

		private class MapEntry
		{
			public string InternalName { get; private set; }
			public string Name { get; private set; }
			public MapType Type { get; private set; }

			public MapEntry(string intname, string name, MapType type)
			{
				InternalName = intname;
				Name = name;
				Type = type;
			}
		}

		private List<MapEntry> Halo2 = new List<MapEntry>()
		{
			new MapEntry("00a_introduction", "The Heretic", MapType.Campaign),
			new MapEntry("01a_tutorial", "Armory", MapType.Campaign),
			new MapEntry("01b_spacestation", "Cairo Station", MapType.Campaign),
			new MapEntry("03a_oldmombasa", "Outskirts", MapType.Campaign),
			new MapEntry("03b_newmombasa", "Metropolis", MapType.Campaign),
			new MapEntry("04a_gasgiant", "The Arbiter", MapType.Campaign),
			new MapEntry("04b_floodlab", "Oracle", MapType.Campaign),
			new MapEntry("05a_deltaapproach", "Delta Halo", MapType.Campaign),
			new MapEntry("05b_deltatowers", "Regret", MapType.Campaign),
			new MapEntry("06a_sentinelwalls", "Sacred Icon", MapType.Campaign),
			new MapEntry("06b_floodzone", "Quarantine Zone", MapType.Campaign),
			new MapEntry("07a_highcharity", "Gravemind", MapType.Campaign),
			new MapEntry("07b_forerunnership", "High Charity", MapType.Campaign),
			new MapEntry("08a_deltacliffs", "Uprising", MapType.Campaign),
			new MapEntry("08b_deltacontrol", "The Great Journey", MapType.Campaign),

			new MapEntry("ascension", "Ascension", MapType.Multiplayer),
			new MapEntry("backwash", "Backwash", MapType.Multiplayer),
			new MapEntry("beavercreek", "Beaver Creek", MapType.Multiplayer),
			new MapEntry("burial_mounds", "Burial Mounds", MapType.Multiplayer),
			new MapEntry("coagulation", "Coagulation", MapType.Multiplayer),
			new MapEntry("colossus", "Colossus", MapType.Multiplayer),
			new MapEntry("cyclotron", "Ivory Tower", MapType.Multiplayer),
			new MapEntry("foundation", "Foundation", MapType.Multiplayer),
			new MapEntry("headlong", "Headlong", MapType.Multiplayer),
			new MapEntry("lockout", "Lockout", MapType.Multiplayer),
			new MapEntry("midship", "Midship", MapType.Multiplayer),
			new MapEntry("waterworks", "Waterworks", MapType.Multiplayer),
			new MapEntry("zanzibar", "Zanzibar", MapType.Multiplayer),

			new MapEntry("containment", "Containment", MapType.Multiplayer),
			new MapEntry("deltatap", "Sanctuary", MapType.Multiplayer),
			new MapEntry("dune", "Relic", MapType.Multiplayer),
			new MapEntry("elongation", "Elongation", MapType.Multiplayer),
			new MapEntry("gemini", "Gemini", MapType.Multiplayer),
			new MapEntry("triplicate", "Terminal", MapType.Multiplayer),
			new MapEntry("turf", "Turf", MapType.Multiplayer),
			new MapEntry("warlock", "Warlock", MapType.Multiplayer),

			new MapEntry("needle", "Uplift", MapType.Multiplayer),
			new MapEntry("street_sweeper", "District", MapType.Multiplayer),

			new MapEntry("derelict", "Desolation", MapType.Multiplayer),
			new MapEntry("highplains", "Tombstone", MapType.Multiplayer),
		};

		private List<MapEntry> Halo3 = new List<MapEntry>()
		{
			new MapEntry("005_intro", "Arrival", MapType.Campaign),
			new MapEntry("010_jungle", "Sierra 117", MapType.Campaign),
			new MapEntry("020_base", "Crow's Nest", MapType.Campaign),
			new MapEntry("030_outskirts", "Tsavo Highway", MapType.Campaign),
			new MapEntry("040_voi", "The Storm", MapType.Campaign),
			new MapEntry("050_floodvoi", "Floodgate", MapType.Campaign),
			new MapEntry("070_waste", "The Ark", MapType.Campaign),
			new MapEntry("100_citadel", "The Covenant", MapType.Campaign),
			new MapEntry("110_hc", "Cortana", MapType.Campaign),
			new MapEntry("120_halo", "Halo", MapType.Campaign),
			new MapEntry("130_epilogue", "Epilogue", MapType.Campaign),

			new MapEntry("chill", "Narrows", MapType.Multiplayer),
			new MapEntry("construct", "Construct", MapType.Multiplayer),
			new MapEntry("cyberdyne", "The Pit", MapType.Multiplayer),
			new MapEntry("deadlock", "High Ground", MapType.Multiplayer),
			new MapEntry("guardian", "Guardian", MapType.Multiplayer),
			new MapEntry("isolation", "Isolation", MapType.Multiplayer),
			new MapEntry("riverworld", "Valhalla", MapType.Multiplayer),
			new MapEntry("salvation", "Epitaph", MapType.Multiplayer),
			new MapEntry("shrine", "Sandtrap", MapType.Multiplayer),
			new MapEntry("snowbound", "Snowbound", MapType.Multiplayer),
			new MapEntry("zanzibar", "Last Resort", MapType.Multiplayer),

			new MapEntry("armory", "Rat's Nest", MapType.Multiplayer),
			new MapEntry("bunkerworld", "Standoff", MapType.Multiplayer),
			new MapEntry("chillout", "Cold Storage", MapType.Multiplayer),
			new MapEntry("descent", "Assembly", MapType.Multiplayer),
			new MapEntry("docks", "Longshore", MapType.Multiplayer),
			new MapEntry("fortress", "Citadel", MapType.Multiplayer),
			new MapEntry("ghosttown", "Ghost Town", MapType.Multiplayer),
			new MapEntry("lockout", "Blackout", MapType.Multiplayer),
			new MapEntry("midship", "Heretic", MapType.Multiplayer),
			new MapEntry("sandbox", "Sandbox", MapType.Multiplayer),
			new MapEntry("sidewinder", "Avalanche", MapType.Multiplayer),
			new MapEntry("spacecamp", "Orbital", MapType.Multiplayer),
			new MapEntry("warehouse", "Foundry", MapType.Multiplayer),
		};

		private List<MapEntry> HaloODST = new List<MapEntry>()
		{
			new MapEntry("c100", "Prepare To Drop", MapType.Campaign),
			new MapEntry("sc100", "Tayari Plaza", MapType.Campaign),
			new MapEntry("sc110", "Uplift Reserve", MapType.Campaign),
			new MapEntry("sc120", "Kizingo Blvd.", MapType.Campaign),
			new MapEntry("sc130", "ONI Alpha Site", MapType.Campaign),
			new MapEntry("sc140", "NMPD HQ", MapType.Campaign),
			new MapEntry("sc150", "Kikowani Stn.", MapType.Campaign),
			new MapEntry("l200", "Data Hive", MapType.Campaign),
			new MapEntry("l300", "Coastal Highway", MapType.Campaign),
			new MapEntry("h100", "Mombasa Streets", MapType.Campaign),
			new MapEntry("c200", "Epilogue", MapType.Campaign),

			new MapEntry("sc100", "Crater", MapType.Survival),
			new MapEntry("sc110", "Lost Platoon", MapType.Survival),
			new MapEntry("sc120", "Rally Point", MapType.Survival),
			new MapEntry("sc130", "Security Zone", MapType.Survival),
			new MapEntry("sc130", "Alpha Site", MapType.Survival),
			new MapEntry("sc140", "Windward", MapType.Survival),
			new MapEntry("l200", "Chasm Ten", MapType.Survival),
			new MapEntry("l300", "Last Exit", MapType.Survival),
			new MapEntry("h100", "Crater (Night)", MapType.Survival),
			new MapEntry("h100", "Rally (Night)", MapType.Survival),
		};

		private List<MapEntry> HaloReach = new List<MapEntry>()
		{
			new MapEntry("m05", "Noble Actual", MapType.Campaign),
			new MapEntry("m10", "Winter Contingency", MapType.Campaign),
			new MapEntry("m20", "ONI Sword Base", MapType.Campaign),
			new MapEntry("m45", "Long Night of Solace", MapType.Campaign),
			new MapEntry("m35", "Tip of the Spear", MapType.Campaign),
			new MapEntry("m30", "Nightfall", MapType.Campaign),
			new MapEntry("m50", "Exodus", MapType.Campaign),
			new MapEntry("m52", "New Alexandria", MapType.Campaign),
			new MapEntry("m60", "The Package", MapType.Campaign),
			new MapEntry("m70", "The Pillar of Autumn", MapType.Campaign),
			new MapEntry("m70_a", "Credits", MapType.Campaign),
			new MapEntry("m70_bonus", "Lone Wolf", MapType.Campaign),

			new MapEntry("20_sword_slayer", "Sword Base", MapType.Multiplayer),
			new MapEntry("30_settlement", "Powerhouse", MapType.Multiplayer),
			new MapEntry("35_island", "Spire", MapType.Multiplayer),
			new MapEntry("45_aftship", "Zealot", MapType.Multiplayer),
			new MapEntry("45_launch_station", "Countdown", MapType.Multiplayer),
			new MapEntry("50_panopticon", "Boardwalk", MapType.Multiplayer),
			new MapEntry("52_ivory_tower", "Reflection", MapType.Multiplayer),
			new MapEntry("70_boneyard", "Boneyard", MapType.Multiplayer),
			new MapEntry("forge_halo", "Forge World", MapType.Multiplayer),

			new MapEntry("cex_beavercreek", "Battle Canyon", MapType.Multiplayer),
			new MapEntry("cex_damnation", "Penance", MapType.Multiplayer),
			new MapEntry("cex_hangemhigh", "High Noon", MapType.Multiplayer),
			new MapEntry("cex_headlong", "Breakneck", MapType.Multiplayer),
			new MapEntry("cex_prisoner", "Solitary", MapType.Multiplayer),
			new MapEntry("cex_timberland", "Ridgeline", MapType.Multiplayer),
			new MapEntry("condemned", "Condemned", MapType.Multiplayer),
			new MapEntry("dlc_invasion", "Breakpoint", MapType.Multiplayer),
			new MapEntry("dlc_medium", "Tempest", MapType.Multiplayer),
			new MapEntry("dlc_slayer", "Anchor 9", MapType.Multiplayer),
			new MapEntry("trainingpreserve", "Highlands", MapType.Multiplayer),

			new MapEntry("ff10_prototype", "Overlook", MapType.Survival),
			new MapEntry("ff20_courtyard", "Courtyard", MapType.Survival),
			new MapEntry("ff30_waterfront", "Waterfront", MapType.Survival),
			new MapEntry("ff45_corvette", "Corvette", MapType.Survival),
			new MapEntry("ff50_park", "Beachhead", MapType.Survival),
			new MapEntry("ff60_airview", "Outpost", MapType.Survival),
			new MapEntry("ff60_icecave", "Glacier", MapType.Survival),
			new MapEntry("ff70_holdout", "Holdout", MapType.Survival),

			new MapEntry("cex_ff_halo", "Installation 04", MapType.Survival),
			new MapEntry("ff_unearthed", "Unearthed", MapType.Survival),
		};

		private List<MapEntry> Halo4 = new List<MapEntry>()
		{
			new MapEntry("m05_prologue", "Prologue", MapType.Campaign),
			new MapEntry("m10_crash", "Dawn", MapType.Campaign),
			new MapEntry("m020", "Requiem", MapType.Campaign),
			new MapEntry("m30_cryptum", "Forerunner", MapType.Campaign),
			new MapEntry("m40_invasion", "Reclaimer", MapType.Campaign),
			new MapEntry("m60_rescue", "Infinity", MapType.Campaign),
			new MapEntry("m70_liftoff", "Shutdown", MapType.Campaign),
			new MapEntry("m80_delta", "Composer", MapType.Campaign),
			new MapEntry("m90_sacrifice", "Midnight", MapType.Campaign),
			new MapEntry("m95_epilogue", "Epilogue", MapType.Campaign),

			new MapEntry("ca_blood_cavern", "Abandon", MapType.Multiplayer),
			new MapEntry("ca_blood_crash", "Exile", MapType.Multiplayer),
			new MapEntry("ca_canyon", "Meltdown", MapType.Multiplayer),
			new MapEntry("ca_forge_bonanza", "Impact", MapType.Multiplayer),
			new MapEntry("ca_forge_erosion", "Erosion", MapType.Multiplayer),
			new MapEntry("ca_forge_ravine", "Ravine", MapType.Multiplayer),
			new MapEntry("ca_gore_valley", "Longbow", MapType.Multiplayer),
			new MapEntry("ca_redoubt", "Vortex", MapType.Multiplayer),
			new MapEntry("ca_tower", "Solace", MapType.Multiplayer),
			new MapEntry("ca_warhouse", "Adrift", MapType.Multiplayer),
			new MapEntry("wraparound", "Haven", MapType.Multiplayer),
			new MapEntry("z05_cliffside", "Complex", MapType.Multiplayer),
			new MapEntry("z11_valhalla", "Ragnarok", MapType.Multiplayer),

			new MapEntry("ca_basin", "Outcast", MapType.Multiplayer),
			new MapEntry("ca_creeper", "Pitfall", MapType.Multiplayer),
			new MapEntry("ca_deadlycrossing", "Monolith", MapType.Multiplayer),
			new MapEntry("ca_dropoff", "Vertigo", MapType.Multiplayer),
			new MapEntry("ca_highrise", "Perdition", MapType.Multiplayer),
			new MapEntry("ca_port", "Landfall", MapType.Multiplayer),
			new MapEntry("ca_rattler", "Skyline", MapType.Multiplayer),
			new MapEntry("ca_spiderweb", "Daybreak", MapType.Multiplayer),
			new MapEntry("dlc_dejewel", "Shatter", MapType.Multiplayer),
			new MapEntry("dlc_dejunkyard", "Wreckage", MapType.Multiplayer),
			new MapEntry("zd_02_grind", "Harvest", MapType.Multiplayer),

			new MapEntry("dlc01_engine", "Infinity", MapType.Survival),
			new MapEntry("dlc01_facory", "Lockup", MapType.Survival),
			new MapEntry("ff151_mezzanine", "Control", MapType.Survival),
			new MapEntry("ff152_vortex", "Cyclone", MapType.Survival),
			new MapEntry("ff153_caverns", "Warrens", MapType.Survival),
			new MapEntry("ff154_hillside", "Apex", MapType.Survival),
			new MapEntry("ff155_breach", "Harvester", MapType.Survival),
			new MapEntry("ff81_courtyard", "The Gate", MapType.Survival),
			new MapEntry("ff82_scurve", "The Cauldron", MapType.Survival),
			new MapEntry("ff84_temple", "The Refuge", MapType.Survival),
			new MapEntry("ff86_sniperalley", "Sniper Alley", MapType.Survival),
			new MapEntry("ff87_chopperbowl", "Quarry", MapType.Survival),
			new MapEntry("ff90_fortsw", "Fortress", MapType.Survival),
			new MapEntry("ff91_complex", "Galileo Base", MapType.Survival),
			new MapEntry("ff92_valhalla", "Two Giants", MapType.Survival),
		};
	}

}