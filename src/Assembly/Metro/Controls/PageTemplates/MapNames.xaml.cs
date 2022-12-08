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

			listH1c.ItemsSource = Halo1.Where(e => e.Type == MapType.Campaign);
			listH1m.ItemsSource = Halo1.Where(e => e.Type == MapType.Multiplayer);

			listH2c.ItemsSource = Halo2.Where(e => e.Type == MapType.Campaign);
			listH2m.ItemsSource = Halo2.Where(e => e.Type == MapType.Multiplayer);

			listH2a.ItemsSource = Halo2A;

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

			listStc.ItemsSource = Stubbs;
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
			public string MapID { get; private set; }
			public MapType Type { get; private set; }

			public MapEntry(string intname, string name, string idname, MapType type)
			{
				InternalName = intname;
				Name = name;
				MapID = idname;
				Type = type;
			}
		}

		private List<MapEntry> Halo1 = new List<MapEntry>()
		{
			new MapEntry("a10", "The Pillar of Autumn", "-", MapType.Campaign),
			new MapEntry("a30", "Halo", "-", MapType.Campaign),
			new MapEntry("a50", "The Truth and Reconciliation", "-", MapType.Campaign),
			new MapEntry("b30", "The Silent Cartographer", "-", MapType.Campaign),
			new MapEntry("b40", "Assault on the Control Room", "-", MapType.Campaign),
			new MapEntry("c10", "343 Guilty Spark", "-", MapType.Campaign),
			new MapEntry("c20", "The Library", "-", MapType.Campaign),
			new MapEntry("c40", "Two Betrayals", "-", MapType.Campaign),
			new MapEntry("d20", "Keyes", "-", MapType.Campaign),
			new MapEntry("d40", "The Maw", "-", MapType.Campaign),

			new MapEntry("beavercreek", "Battle Creek", "-", MapType.Multiplayer),
			new MapEntry("bloodgulch", "Blood Gulch", "-", MapType.Multiplayer),
			new MapEntry("boardingaction", "Boarding Action", "-", MapType.Multiplayer),
			new MapEntry("chillout", "Chill Out", "-", MapType.Multiplayer),
			new MapEntry("putput", "Chiron TL-34", "-", MapType.Multiplayer),
			new MapEntry("damnation", "Damnation", "-", MapType.Multiplayer),
			new MapEntry("dangercanyon", "Danger Canyon", "-", MapType.Multiplayer),
			new MapEntry("deathisland", "Death Island", "-", MapType.Multiplayer),
			new MapEntry("carousel ", "Derelict", "-", MapType.Multiplayer),
			new MapEntry("gephyrophobia", "Gephyrophobia", "-", MapType.Multiplayer),
			new MapEntry("hangemhigh", "Hang 'Em High", "-", MapType.Multiplayer),
			new MapEntry("icefields", "Ice Fields", "-", MapType.Multiplayer),
			new MapEntry("infinity", "Infinity", "-", MapType.Multiplayer),
			new MapEntry("longest", "Longest", "-", MapType.Multiplayer),
			new MapEntry("prisoner", "Prisoner", "-", MapType.Multiplayer),
			new MapEntry("ratrace", "Rat Race", "-", MapType.Multiplayer),
			new MapEntry("sidewinder", "Sidewinder", "-", MapType.Multiplayer),
			new MapEntry("timberland", "Timberland", "-", MapType.Multiplayer),
			new MapEntry("wizard", "Wizard", "-", MapType.Multiplayer),
		};

		private List<MapEntry> Halo2 = new List<MapEntry>()
		{
			new MapEntry("00a_introduction", "The Heretic", "1", MapType.Campaign),
			new MapEntry("01a_tutorial", "Armory", "101", MapType.Campaign),
			new MapEntry("01b_spacestation", "Cairo Station", "105", MapType.Campaign),
			new MapEntry("03a_oldmombasa", "Outskirts", "301",  MapType.Campaign),
			new MapEntry("03b_newmombasa", "Metropolis", "305", MapType.Campaign),
			new MapEntry("04a_gasgiant", "The Arbiter", "401", MapType.Campaign),
			new MapEntry("04b_floodlab", "Oracle", "405", MapType.Campaign),
			new MapEntry("05a_deltaapproach", "Delta Halo", "501", MapType.Campaign),
			new MapEntry("05b_deltatowers", "Regret", "505", MapType.Campaign),
			new MapEntry("06a_sentinelwalls", "Sacred Icon", "601", MapType.Campaign),
			new MapEntry("06b_floodzone", "Quarantine Zone", "605", MapType.Campaign),
			new MapEntry("07a_highcharity", "Gravemind", "701", MapType.Campaign),
			new MapEntry("07b_forerunnership", "High Charity", "801", MapType.Campaign),
			new MapEntry("08a_deltacliffs", "Uprising", "705", MapType.Campaign),
			new MapEntry("08b_deltacontrol", "The Great Journey", "805", MapType.Campaign),

			new MapEntry("ascension", "Ascension", "80", MapType.Multiplayer),
			new MapEntry("backwash", "Backwash", "1201", MapType.Multiplayer),
			new MapEntry("beavercreek", "Beaver Creek", "100", MapType.Multiplayer),
			new MapEntry("burial_mounds", "Burial Mounds", "60", MapType.Multiplayer),
			new MapEntry("coagulation", "Coagulation", "110", MapType.Multiplayer),
			new MapEntry("colossus", "Colossus", "70", MapType.Multiplayer),
			new MapEntry("cyclotron", "Ivory Tower", "10", MapType.Multiplayer),
			new MapEntry("foundation", "Foundation", "120", MapType.Multiplayer),
			new MapEntry("headlong", "Headlong", "800", MapType.Multiplayer),
			new MapEntry("lockout", "Lockout", "50", MapType.Multiplayer),
			new MapEntry("midship", "Midship", "20", MapType.Multiplayer),
			new MapEntry("waterworks", "Waterworks", "40", MapType.Multiplayer),
			new MapEntry("zanzibar", "Zanzibar", "30", MapType.Multiplayer),

			new MapEntry("containment", "Containment", "1300", MapType.Multiplayer),
			new MapEntry("deltatap", "Sanctuary", "1302", MapType.Multiplayer),
			new MapEntry("dune", "Relic", "1200", MapType.Multiplayer),
			new MapEntry("elongation", "Elongation", "1001", MapType.Multiplayer),
			new MapEntry("gemini", "Gemini", "1002", MapType.Multiplayer),
			new MapEntry("triplicate", "Terminal", "1101", MapType.Multiplayer),
			new MapEntry("turf", "Turf", "1000", MapType.Multiplayer),
			new MapEntry("warlock", "Warlock", "1109", MapType.Multiplayer),

			new MapEntry("needle", "Uplift", "444678", MapType.Multiplayer),
			new MapEntry("street_sweeper", "District", "91101", MapType.Multiplayer),

			new MapEntry("derelict", "Desolation", "1400", MapType.Multiplayer),
			new MapEntry("highplains", "Tombstone", "1402", MapType.Multiplayer),
		};

		private List<MapEntry> Halo2A = new List<MapEntry>()
		{
			new MapEntry("ca_ascension.map", "Zenith", "15020", MapType.Multiplayer),
			new MapEntry("ca_coagulation", "Bloodline", "15070", MapType.Multiplayer),
			new MapEntry("ca_forge_skybox01", "Skyward", "15080", MapType.Multiplayer),
			new MapEntry("ca_forge_skybox02", "Nebula", "15090", MapType.Multiplayer),
			new MapEntry("ca_forge_skybox03", "Awash", "15100", MapType.Multiplayer),
			new MapEntry("ca_lockout", "Lockdown", "15050", MapType.Multiplayer),
			new MapEntry("ca_relic", "Remnant", "15110", MapType.Multiplayer),
			new MapEntry("ca_sanctuary", "Shrine", "15030", MapType.Multiplayer),
			new MapEntry("ca_warlock", "Warlord", "15060", MapType.Multiplayer),
			new MapEntry("ca_zanzibar", "Stonetown", "15040", MapType.Multiplayer),
		};

		private List<MapEntry> Halo3 = new List<MapEntry>()
		{
			new MapEntry("005_intro", "Arrival", "3005", MapType.Campaign),
			new MapEntry("010_jungle", "Sierra 117", "3010", MapType.Campaign),
			new MapEntry("020_base", "Crow's Nest", "3020", MapType.Campaign),
			new MapEntry("030_outskirts", "Tsavo Highway", "3030", MapType.Campaign),
			new MapEntry("040_voi", "The Storm", "3040", MapType.Campaign),
			new MapEntry("050_floodvoi", "Floodgate", "3050", MapType.Campaign),
			new MapEntry("070_waste", "The Ark", "3070", MapType.Campaign),
			new MapEntry("100_citadel", "The Covenant", "3100", MapType.Campaign),
			new MapEntry("110_hc", "Cortana", "3110", MapType.Campaign),
			new MapEntry("120_halo", "Halo", "3120", MapType.Campaign),
			new MapEntry("130_epilogue", "Epilogue", "3130", MapType.Campaign),

			new MapEntry("chill", "Narrows", "380", MapType.Multiplayer),
			new MapEntry("construct", "Construct", "300", MapType.Multiplayer),
			new MapEntry("cyberdyne", "The Pit", "390", MapType.Multiplayer),
			new MapEntry("deadlock", "High Ground", "310", MapType.Multiplayer),
			new MapEntry("guardian", "Guardian", "320", MapType.Multiplayer),
			new MapEntry("isolation", "Isolation", "330", MapType.Multiplayer),
			new MapEntry("riverworld", "Valhalla", "340", MapType.Multiplayer),
			new MapEntry("salvation", "Epitaph", "350", MapType.Multiplayer),
			new MapEntry("shrine", "Sandtrap", "400", MapType.Multiplayer),
			new MapEntry("snowbound", "Snowbound", "360", MapType.Multiplayer),
			new MapEntry("zanzibar", "Last Resort", "30", MapType.Multiplayer),

			new MapEntry("armory", "Rat's Nest", "580", MapType.Multiplayer),
			new MapEntry("bunkerworld", "Standoff", "410", MapType.Multiplayer),
			new MapEntry("chillout", "Cold Storage", "600", MapType.Multiplayer),
			new MapEntry("descent", "Assembly", "490",  MapType.Multiplayer),
			new MapEntry("docks", "Longshore", "440", MapType.Multiplayer),
			new MapEntry("fortress", "Citadel", "740", MapType.Multiplayer),
			new MapEntry("ghosttown", "Ghost Town", "590", MapType.Multiplayer),
			new MapEntry("lockout", "Blackout", "520", MapType.Multiplayer),
			new MapEntry("midship", "Heretic", "720", MapType.Multiplayer),
			new MapEntry("sandbox", "Sandbox", "730", MapType.Multiplayer),
			new MapEntry("sidewinder", "Avalanche", "470", MapType.Multiplayer),
			new MapEntry("spacecamp", "Orbital", "500", MapType.Multiplayer),
			new MapEntry("warehouse", "Foundry", "480", MapType.Multiplayer),

			new MapEntry("s3d_waterfall", "Waterfall", "706", MapType.Multiplayer),
			new MapEntry("s3d_edge", "Edge", "703", MapType.Multiplayer),
			new MapEntry("s3d_turf", "Icebox", "31", MapType.Multiplayer),
		};

		private List<MapEntry> HaloODST = new List<MapEntry>()
		{
			new MapEntry("c100", "Prepare To Drop", "4100", MapType.Campaign),
			new MapEntry("sc100", "Tayari Plaza", "6100", MapType.Campaign),
			new MapEntry("sc110", "Uplift Reserve", "6110", MapType.Campaign),
			new MapEntry("sc120", "Kizingo Blvd.", "6120", MapType.Campaign),
			new MapEntry("sc130", "ONI Alpha Site", "6130", MapType.Campaign),
			new MapEntry("sc140", "NMPD HQ", "6140", MapType.Campaign),
			new MapEntry("sc150", "Kikowani Stn.", "6150", MapType.Campaign),
			new MapEntry("l200", "Data Hive", "5200", MapType.Campaign),
			new MapEntry("l300", "Coastal Highway", "5300", MapType.Campaign),
			new MapEntry("h100", "Mombasa Streets", "5000", MapType.Campaign),
			new MapEntry("c200", "Epilogue", "4200", MapType.Campaign),

			new MapEntry("sc100", "Crater", "6100", MapType.Survival),
			new MapEntry("sc110", "Lost Platoon", "6110", MapType.Survival),
			new MapEntry("sc120", "Rally Point", "6120", MapType.Survival),
			new MapEntry("sc130", "Security Zone", "6130", MapType.Survival),
			new MapEntry("sc130", "Alpha Site", "6130", MapType.Survival),
			new MapEntry("sc140", "Windward", "6140", MapType.Survival),
			new MapEntry("l200", "Chasm Ten", "5200", MapType.Survival),
			new MapEntry("l300", "Last Exit", "5300", MapType.Survival),
			new MapEntry("h100", "Crater (Night)", "5000", MapType.Survival),
			new MapEntry("h100", "Rally (Night)", "5000", MapType.Survival),
		};

		private List<MapEntry> HaloReach = new List<MapEntry>()
		{
			new MapEntry("m05", "Noble Actual", "5005", MapType.Campaign),
			new MapEntry("m10", "Winter Contingency", "5010", MapType.Campaign),
			new MapEntry("m20", "ONI Sword Base", "5020", MapType.Campaign),
			new MapEntry("m30", "Nightfall", "5030", MapType.Campaign),
			new MapEntry("m35", "Tip of the Spear", "5035", MapType.Campaign),
			new MapEntry("m45", "Long Night of Solace", "5045", MapType.Campaign),
			new MapEntry("m50", "Exodus", "5050", MapType.Campaign),
			new MapEntry("m52", "New Alexandria", "5052", MapType.Campaign),
			new MapEntry("m60", "The Package", "5060", MapType.Campaign),
			new MapEntry("m70", "The Pillar of Autumn", "5070", MapType.Campaign),
			new MapEntry("m70_a", "Credits", "5075", MapType.Campaign),
			new MapEntry("m70_bonus", "Lone Wolf", "5080", MapType.Campaign),

			new MapEntry("20_sword_slayer", "Sword Base", "1000", MapType.Multiplayer),
			new MapEntry("30_settlement", "Powerhouse", "1055", MapType.Multiplayer),
			new MapEntry("35_island", "Spire", "1200", MapType.Multiplayer),
			new MapEntry("45_aftship", "Zealot", "1040", MapType.Multiplayer),
			new MapEntry("45_launch_station", "Countdown", "1020", MapType.Multiplayer),
			new MapEntry("50_panopticon", "Boardwalk", "1035", MapType.Multiplayer),
			new MapEntry("52_ivory_tower", "Reflection", "1150", MapType.Multiplayer),
			new MapEntry("70_boneyard", "Boneyard", "1080", MapType.Multiplayer),
			new MapEntry("forge_halo", "Forge World", "3006", MapType.Multiplayer),

			new MapEntry("cex_beavercreek", "Battle Canyon", "10020", MapType.Multiplayer),
			new MapEntry("cex_damnation", "Penance", "10010", MapType.Multiplayer),
			new MapEntry("cex_hangemhigh", "High Noon", "10060", MapType.Multiplayer),
			new MapEntry("cex_headlong", "Breakneck", "10050", MapType.Multiplayer),
			new MapEntry("cex_prisoner", "Solitary", "10070", MapType.Multiplayer),
			new MapEntry("cex_timberland", "Ridgeline", "10030", MapType.Multiplayer),
			new MapEntry("condemned", "Condemned", "1500", MapType.Multiplayer),
			new MapEntry("dlc_invasion", "Breakpoint", "2002", MapType.Multiplayer),
			new MapEntry("dlc_medium", "Tempest", "2004", MapType.Multiplayer),
			new MapEntry("dlc_slayer", "Anchor 9", "2001", MapType.Multiplayer),
			new MapEntry("trainingpreserve", "Highlands", "1510", MapType.Multiplayer),

			new MapEntry("ff10_prototype", "Overlook", "7000", MapType.Survival),
			new MapEntry("ff20_courtyard", "Courtyard", "7020", MapType.Survival),
			new MapEntry("ff30_waterfront", "Waterfront", "7040", MapType.Survival),
			new MapEntry("ff45_corvette", "Corvette", "7110", MapType.Survival),
			new MapEntry("ff50_park", "Beachhead", "7060", MapType.Survival),
			new MapEntry("ff60_airview", "Outpost", "7030", MapType.Survival),
			new MapEntry("ff60_icecave", "Glacier", "7130", MapType.Survival),
			new MapEntry("ff70_holdout", "Holdout", "7080", MapType.Survival),

			new MapEntry("cex_ff_halo", "Installation 04", "10080", MapType.Survival),
			new MapEntry("ff_unearthed", "Unearthed", "7500", MapType.Survival),
		};

		private List<MapEntry> Halo4 = new List<MapEntry>()
		{
			new MapEntry("m05_prologue", "Prologue", "12000", MapType.Campaign),
			new MapEntry("m10_crash", "Dawn", "12010", MapType.Campaign),
			new MapEntry("m020", "Requiem", "12020", MapType.Campaign),
			new MapEntry("m30_cryptum", "Forerunner", "12030", MapType.Campaign),
			new MapEntry("m40_invasion", "Reclaimer", "12040", MapType.Campaign),
			new MapEntry("m60_rescue", "Infinity", "12060", MapType.Campaign),
			new MapEntry("m70_liftoff", "Shutdown", "12070", MapType.Campaign),
			new MapEntry("m80_delta", "Composer", "12080", MapType.Campaign),
			new MapEntry("m90_sacrifice", "Midnight", "12090", MapType.Campaign),
			new MapEntry("m95_epilogue", "Epilogue", "12100", MapType.Campaign),

			new MapEntry("ca_blood_cavern", "Abandon", "10225", MapType.Multiplayer),
			new MapEntry("ca_blood_crash", "Exile", "10226", MapType.Multiplayer),
			new MapEntry("ca_canyon", "Meltdown", "10261", MapType.Multiplayer),
			new MapEntry("ca_forge_bonanza", "Impact", "10255", MapType.Multiplayer),
			new MapEntry("ca_forge_erosion", "Erosion", "10245", MapType.Multiplayer),
			new MapEntry("ca_forge_ravine", "Ravine", "10256", MapType.Multiplayer),
			new MapEntry("ca_gore_valley", "Longbow", "10200", MapType.Multiplayer),
			new MapEntry("ca_redoubt", "Vortex", "10252", MapType.Multiplayer),
			new MapEntry("ca_tower", "Solace", "10202", MapType.Multiplayer),
			new MapEntry("ca_warhouse", "Adrift", "10210", MapType.Multiplayer),
			new MapEntry("wraparound", "Haven", "10080", MapType.Multiplayer),
			new MapEntry("z05_cliffside", "Complex", "10085", MapType.Multiplayer),
			new MapEntry("z11_valhalla", "Ragnarok", "10091", MapType.Multiplayer),

			new MapEntry("ca_basin", "Outcast", "13140", MapType.Multiplayer),
			new MapEntry("ca_creeper", "Pitfall", "15000", MapType.Multiplayer),
			new MapEntry("ca_deadlycrossing", "Monolith", "13131", MapType.Multiplayer),
			new MapEntry("ca_dropoff", "Vertigo", "15010", MapType.Multiplayer),
			new MapEntry("ca_highrise", "Perdition", "13120", MapType.Multiplayer),
			new MapEntry("ca_port", "Landfall", "13110", MapType.Multiplayer),
			new MapEntry("ca_rattler", "Skyline", "13160", MapType.Multiplayer),
			new MapEntry("ca_spiderweb", "Daybreak", "13130", MapType.Multiplayer),
			new MapEntry("dlc_dejewel", "Shatter", "13302", MapType.Multiplayer),
			new MapEntry("dlc_dejunkyard", "Wreckage", "13301", MapType.Multiplayer),
			new MapEntry("dlc_forge_island", "Forge Island", "14100", MapType.Multiplayer),
			new MapEntry("zd_02_grind", "Harvest", "10102", MapType.Multiplayer),

			new MapEntry("dlc01_engine", "Infinity", "11250", MapType.Survival),
			new MapEntry("dlc01_factory", "Lockup", "11302", MapType.Survival),
			new MapEntry("ff151_mezzanine", "Control", "11200", MapType.Survival),
			new MapEntry("ff152_vortex", "Cyclone", "11210", MapType.Survival),
			new MapEntry("ff153_caverns", "Warrens", "11230", MapType.Survival),
			new MapEntry("ff154_hillside", "Apex", "11240", MapType.Survival),
			new MapEntry("ff155_breach", "Harvester", "11061", MapType.Survival),
			new MapEntry("ff81_courtyard", "The Gate", "11081", MapType.Survival),
			new MapEntry("ff82_scurve", "The Cauldron", "11071", MapType.Survival),
			new MapEntry("ff84_temple", "The Refuge", "11084", MapType.Survival),
			new MapEntry("ff86_sniperalley", "Sniper Alley", "11101", MapType.Survival),
			new MapEntry("ff87_chopperbowl", "Quarry", "11111", MapType.Survival),
			new MapEntry("ff90_fortsw", "Fortress", "11141", MapType.Survival),
			new MapEntry("ff91_complex", "Galileo Base", "11151", MapType.Survival),
			new MapEntry("ff92_valhalla", "Two Giants", "11161", MapType.Survival),
		};

		private List<MapEntry> Stubbs = new List<MapEntry>()
		{
			new MapEntry("a10_plaza", "Welcome to Punchbowl", "-", MapType.Campaign),
			new MapEntry("a30_greenhouse", "Bleeding Ground", "-", MapType.Campaign),
			new MapEntry("a40_police_station", "The Slammer", "-", MapType.Campaign),
			new MapEntry("a45_dance", "Cop Rock", "-", MapType.Campaign),
			new MapEntry("a50_maul", "Painting the Town Red", "-", MapType.Campaign),
			new MapEntry("a60_maulfight", "Punchbowl Maul", "-", MapType.Campaign),
			new MapEntry("b10_farm_house", "Fall of the House of Otis", "-", MapType.Campaign),
			new MapEntry("b30_dam", "When the Zombie Breaks", "-", MapType.Campaign),
			new MapEntry("c10_offender", "The Sacking of Punchbowl", "-", MapType.Campaign),
			new MapEntry("c30_lab", "The Doctor Will See You Now", "-", MapType.Campaign),
			new MapEntry("c40_cityhall", "Paved with Good Intentions", "-", MapType.Campaign),
			new MapEntry("c50_end", "The Ghoul of Your Dreams", "-", MapType.Campaign),
		};
	}

}
