namespace Blamite.Plugins
{
	public class PluginRevision
	{
		public PluginRevision(string researcher, int version, string description)
		{
			Researcher = researcher;
			Version = version;
			Description = description;
		}

		public string Researcher { get; set; }
		public int Version { get; set; }
		public string Description { get; set; }
	}
}