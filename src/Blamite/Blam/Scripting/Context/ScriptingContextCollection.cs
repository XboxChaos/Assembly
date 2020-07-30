using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Context
{
    public class ScriptingContextCollection
    {
        private readonly Dictionary<string, ScriptingContextBlock> _objectGroups = new Dictionary<string, ScriptingContextBlock>();

        public void AddObjectGroup(ScriptingContextBlock group)
        {
            if (_objectGroups.ContainsKey(group.Name))
            {
                throw new InvalidOperationException($"The context collection has multiple definitions for the object group {group.Name}.");
            }
            else
            {
                _objectGroups.Add(group.Name, group);
            }
        }

        public bool TryGetObjectGroup(string name, out ScriptingContextBlock group)
        {
            return _objectGroups.TryGetValue(name, out group);
        }
    }
}
