using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public static class Casting
    {
        private static readonly List<string> _numTypes = new List<string> { "real", "short", "long" };

        private static readonly List<string> _flexTypes = new List<string>() { "ANY", "NUMBER", "GLOBALREFERENCE" };

        //private static readonly Dictionary<string, string[]> _types = new Dictionary<string, string[]>()
        //{
        //    {"object", new string[]{"object_name", "player", "ai", "unit", "vehicle", "weapon", "device" , "scenery", "effect_scenery"} },
        //    {"object_list", new string[]{"unit", "object", "vehicle"} },
        //    {"unit", new string[]{"unit_name", "player", "ai", "vehicle", "object"} },
        //    {"vehicle", new string[]{"vehicle_name", "ai"} },
        //    {"weapon", new string[]{"weapon_name"} },
        //    {"device", new string[]{"device_name"} },
        //    {"scenery", new string[]{"scenery_name"} },
        //    {"effect_scenery", new string[]{"effect_scenery_name"} },
        //    {"real", new string[]{"short", "long"} },
        //    {"short", new string[]{"real", "long"} },
        //    {"long", new string[]{"real", "short"} },
        //    {"player", new string[]{"unit"} },
        //    {"boolean", new string[]{"short", "long"} }

        //};

        //public static bool CanBeCasted(string from, string to)
        //{
        //    bool found;
        //    string[] children;
        //    found = _types.TryGetValue(to, out children);
        //    if(found)
        //    {
        //        return children.Contains(from);
        //    }
        //    else
        //    {
        //        return false;
        //    }

        //}

        public static bool CanBeCasted(string from, string to, string initialTo, OpcodeLookup op)
        {
            if((IsNumType(from) && IsNumType(initialTo)) || (op.GetTypeInfo(from).Object && op.GetTypeInfo(initialTo).Object))
            {
                return true;
            }

            CastInfo info = op.GetTypeCast(to);
            // check if this type supports casting
            if (info != null)
            {
                // check if the type is contained in the list
                if(info.From.Contains(from))
                {
                    return true;
                }
                else
                {
                    // it wasn't contained...last chance...attempt to cast to one of it's intermediate types
                    foreach(string child in info.From)
                    {
                        // recursion
                        if(child != initialTo && CanBeCasted(from, child, initialTo, op))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool IsNumType(string type)
        {
            return _numTypes.Contains(type);
        }

        public static bool IsFlexType(string type)
        {
            return _flexTypes.Contains(type);
        }
    }
}
