using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Context
{
    public class ScriptingContextObject
    {
        private readonly Dictionary<string, ScriptingContextBlock> _children = new Dictionary<string, ScriptingContextBlock>();

        public string Name { get; set; }

        public int Index { get; set; }

        public string ObjectGroup { get; set; }

        public bool HasChildren { get { return _children.Count > 0; } }

        public void AddChildObject(ScriptingContextBlock child)
        {
            if (_children.ContainsKey(child.Name))
            {
                throw new InvalidOperationException($"The context object {Name} has multiple definitions for the child {child.Name}.");
            }
            else
            {
                _children.Add(child.Name, child);
            }
        }

        public bool TryGetChildObject(string name, out ScriptingContextBlock child)
        {
            return _children.TryGetValue(name, out child);
        }
    }
}
