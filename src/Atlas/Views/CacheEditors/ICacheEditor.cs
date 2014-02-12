namespace Atlas.Views.CacheEditors
{
	public interface ICacheEditor : IAssemblyPage
	{
		string EditorTitle { get; }

		bool IsSingleInstance { get; }
	}
}
