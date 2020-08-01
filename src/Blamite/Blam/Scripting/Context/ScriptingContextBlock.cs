using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Context
{
    public class ScriptingContextBlock
    {
        private readonly Lookup<string, ScriptingContextObject> _objects;

        public string Name { get; set; }

        public ScriptingContextBlock(string name, IEnumerable<ScriptingContextObject> objects)
        {
            Name = name;
            _objects = (Lookup<string, ScriptingContextObject>)objects.ToLookup(obj => obj.Name, obj => obj);
        }

        public IEnumerable<ScriptingContextObject> GetObjects(string name)
        {
            return _objects[name];
        }

        public IEnumerable<ScriptingContextObject> GetAllObjects()
        {
            List<ScriptingContextObject> result = new List<ScriptingContextObject>();
            IEnumerable<ScriptingContextObject> values = _objects.SelectMany(b => b);
            result.AddRange(values);

            foreach(var obj in values)
            {
                result.AddRange(obj.GetAllChildObjects());
            }

            return result;
        }

    }
}
