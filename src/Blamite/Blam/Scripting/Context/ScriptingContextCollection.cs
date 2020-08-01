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

        private readonly Dictionary<string, UnitSeatMapping> _unitSeatMappings = new Dictionary<string, UnitSeatMapping>();

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

        public void AddUnitSeatMapping(UnitSeatMapping mapping)
        {
            if (_unitSeatMappings.ContainsKey(mapping.Name))
            {
                throw new InvalidOperationException($"The context collection has multiple definitions for the unit seat mapping {mapping.Name}.");
            }
            else
            {
                _unitSeatMappings.Add(mapping.Name, mapping);
            }
        }

        public bool TryGetBlock(string name, out ScriptingContextBlock group)
        {
            return _objectGroups.TryGetValue(name, out group);
        }

        public bool TryGetUnitSeatMapping(string name, out UnitSeatMapping mapping)
        {
            return _unitSeatMappings.TryGetValue(name, out mapping);
        }

        public bool TryGetObjects(out IEnumerable<ScriptingContextObject> objects, params Tuple<string, string>[] path)
        {
            // Item1 = Block name
            // Item2 = Object name
            objects = new ScriptingContextObject[0];

            if (_objectGroups.TryGetValue(path[0].Item1, out ScriptingContextBlock block))
            {
                var result = block.GetObjects(path[0].Item2).ToArray();
                if(!result.Any())
                {
                    return false;
                }
                else if(path.Length == 1)
                {
                    objects = result;
                    return true;
                }

                // Handle children.
                for (int i = 1; i < path.Length; i++)
                {
                    // Just pick the first match for now. Duplicate names suck.
                    if(!result[0].TryGetChildBlock(path[i].Item1, out ScriptingContextBlock childBlock))
                    {
                        return false;
                    }

                    result = childBlock.GetObjects(path[i].Item2).ToArray();
                    if (result.Length == 0)
                    {
                        return false;
                    }
                }
                objects = result;
                return true;
            }
            return false;
        }

        public IEnumerable<ScriptingContextObject> GetAllContextObjects()
        {
            List<ScriptingContextObject> result = new List<ScriptingContextObject>();

            foreach (ScriptingContextBlock block in _objectGroups.Values)
            {
                result.AddRange(block.GetAllObjects());
            }

            return result;
        }

        public IEnumerable<UnitSeatMapping> GetAllUnitSeatMappings()
        {
            return _unitSeatMappings.Values;
        }
    }
}
