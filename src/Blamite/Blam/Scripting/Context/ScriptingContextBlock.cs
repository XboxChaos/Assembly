using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Context
{
    public class ScriptingContextBlock
    {
        private readonly Dictionary<string, ScriptingContextObject> _objects = new Dictionary<string, ScriptingContextObject>();

        public string Name { get; set; }

        public void AddObject(ScriptingContextObject obj)
        {
            if(_objects.ContainsKey(obj.Name))
            {
                throw new InvalidOperationException($"The context group {Name} has multiple definitions for the name {obj.Name}.");
            }
            else
            {
                _objects.Add(obj.Name, obj);
            }
        }

        public bool TryGetObject(string name, out ScriptingContextObject obj)
        {
            return _objects.TryGetValue(name, out obj);
        }
    }
}
