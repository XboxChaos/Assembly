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

        public bool IsChild { get; set; }

        public ScriptingContextObject(string name, int index, string group, bool isChild)
        {
            Name = name;
            Index = index;
            ObjectGroup = group;
            IsChild = isChild;
        }

        public void AddChildBlock(ScriptingContextBlock child)
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

        public bool TryGetChildBlock(string name, out ScriptingContextBlock child)
        {
            return _children.TryGetValue(name, out child);
        }

        public IEnumerable<ScriptingContextObject> GetChildObjects(string childBlockName, string name)
        {
            if(HasChildren && _children.TryGetValue(childBlockName, out ScriptingContextBlock childBlock))
            {
                return childBlock.GetObjects(name);
            }
            else
            {
                return new ScriptingContextObject[0];
            }    
        }

        public IEnumerable<ScriptingContextObject> GetAllChildObjects()
        {
            List<ScriptingContextObject> result = new List<ScriptingContextObject>();
            foreach(ScriptingContextBlock block in _children.Values)
            {
                result.AddRange(block.GetAllObjects());
            }
            return result;
        }
    }
}
