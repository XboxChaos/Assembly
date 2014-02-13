namespace Atlas.Views.Cache
{
	public interface ICacheEditor : IAssemblyPage
	{
		string EditorTitle { get; }

		bool IsSingleInstance { get; }
	}
}
