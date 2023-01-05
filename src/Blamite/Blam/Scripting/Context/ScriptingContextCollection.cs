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
                //mod tool maps can build with multiple entries I guess?

                //throw new InvalidOperationException($"The context collection has multiple definitions for the unit seat mapping {mapping.Name}.");
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

        /// <summary>
        /// Tries to get a <see cref="ScriptingContextObject"/> by its path from the context. Returns the first object if multiple are found.
        /// </summary>
        /// <param name="scriptObject">The script object, that was found; otherwise <c>null</c>.</param>
        /// <param name="path">The path of the script object. The tuples have the format <c>(Block Name, Object Name)</c>.</param>
        /// <returns>Returns <c>true</c> if a script object was found; otherwise returns <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// bool foundObj = TryGetObject(out ScriptingContextObject myObj, Tuple.Create("ai_squad", "sq_garage_cov_1"), Tuple.Create("ai_single_location", "spawn_points_3"));
        /// </code>
        /// </example>
        public bool TryGetObjectFirst(out ScriptingContextObject scriptObject, params Tuple<string, string>[] path)
        {
            // Item1 = Block name
            // Item2 = Object name
            scriptObject = null;
            if(path.Length > 0 && TryGetObjects(out IEnumerable<ScriptingContextObject>  objects, path))
            {
                scriptObject = objects.First();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to get a single <see cref="ScriptingContextObject"/> by its path from the context. Throws an exception if multiple are found.
        /// </summary>
        /// <param name="scriptObject">The script object, that was found; otherwise <c>null</c>.</param>
        /// <param name="path">The path of the script object. The tuples have the format <c>(Block Name, Object Name)</c>.</param>
        /// <returns>Returns <c>true</c> if a script object was found; otherwise returns <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// bool foundObj = TryGetObject(out ScriptingContextObject myObj, Tuple.Create("ai_squad", "sq_garage_cov_1"), Tuple.Create("ai_single_location", "spawn_points_3"));
        /// </code>
        /// </example>
        /// <exception cref="ScriptingContextException">Thrown when multiple context objects were found.</exception>
        public bool TryGetObjectSingle(out ScriptingContextObject scriptObject, params Tuple<string, string>[] path)
        {
            // Item1 = Block name
            // Item2 = Object name
            scriptObject = null;

            if (path.Length > 0 && TryGetObjects(out IEnumerable<ScriptingContextObject> objects, path))
            {
                if(objects.Count() == 1)
                {
                    scriptObject = objects.First();
                    return true;
                }    
                else
                {
                    var lastPathTuple = path.Last();
                    throw new ScriptingContextException($"Failed to retrieve the single context object \"{lastPathTuple.Item2}\". " +
                        $"The scripting context block \"{lastPathTuple.Item1}\" contained multiple objects with this name.");
                }
            }
            else
            {
                return false;
            }
        }

        public bool ContainsObject(params Tuple<string, string>[] path)
        {
            if (path.Length > 0 && _objectGroups.TryGetValue(path[0].Item1, out ScriptingContextBlock block))
            {
                var objects = block.GetObjects(path[0].Item2).ToArray();
                if (!objects.Any())
                {
                    return false;
                }
                else if (path.Length == 1)
                {
                    return true;
                }

                // Handle children.
                for (int i = 1; i < path.Length; i++)
                {
                    // Just pick the first match for now. Duplicate names suck.
                    if (!objects[0].TryGetChildBlock(path[i].Item1, out ScriptingContextBlock childBlock))
                    {
                        return false;
                    }

                    objects = childBlock.GetObjects(path[i].Item2).ToArray();
                    if (objects.Length == 0)
                    {
                        return false;
                    }
                }
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
