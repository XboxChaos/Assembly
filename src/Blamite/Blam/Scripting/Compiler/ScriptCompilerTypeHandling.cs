﻿using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Blamite.Util;
using Blamite.Blam.Scripting.Context;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {
        //private bool ProcessLiteralOld(string expectedValueType, BS_ReachParser.LiteralContext context)
        //{
        //    CastInfo info = _opcodes.GetTypeCast(expectedValueType);
        //    // Types which can be casted to.
        //    if (info != null)
        //    {
        //        // Check if casting is necessary.
        //        if (HandleValueType(context, expectedValueType))
        //        {
        //            return true;
        //        }
        //        // Casting.
        //        else
        //        {
        //            FIFOStack<string> remainingTypes = new FIFOStack<string>();
        //            List<string> processedTypes = new List<string>();

        //            // Push immediate casts.
        //            foreach (string type in info.From)
        //            {
        //                remainingTypes.Push(type);
        //            }

        //            while (remainingTypes.Count > 0)
        //            {
        //                // Check if this is the right value type.
        //                string typeToProcess = remainingTypes.Pop();
        //                if (HandleValueType(context, typeToProcess))
        //                {
        //                    // Overwrite the value type of the added expression node with the casted type.
        //                    _expressions[_expressions.Count - 1].ReturnType = _opcodes.GetTypeInfo(expectedValueType).Opcode;
        //                    return true;
        //                }
        //                processedTypes.Add(typeToProcess);
        //                info = _opcodes.GetTypeCast(typeToProcess);
        //                if (info != null)
        //                {
        //                    // Push nested casts.
        //                    foreach (string type in info.From)
        //                    {
        //                        if (!remainingTypes.Contains(type) && !processedTypes.Contains(type))
        //                        {
        //                            remainingTypes.Push(type);
        //                        }
        //                    }
        //                }
        //            }
        //            return false;
        //        }
        //    }
        //    // This type can't be casted to.
        //    else
        //    {
        //        return HandleValueType(context, expectedValueType);
        //    }
        //}

        private bool ProcessLiteral(string expectedValueType, BS_ReachParser.LiteralContext context)
        {
            // Casting might not be necessary.
            if(HandleValueType(context, expectedValueType))
            {
                return true;
            }
            // Casting.
            else
            {
                CastInfo info = _opcodes.GetTypeCast(expectedValueType);

                // This type can't be casted to.
                if(info is null)
                {
                    return false;
                }

                foreach(string cast in info.From)
                {
                    if(HandleValueType(context, cast))
                    {
                        // Overwrite the value type of the added expression node with the casted type.
                        _expressions[_expressions.Count - 1].ReturnType = _opcodes.GetTypeInfo(expectedValueType).Opcode;
                        return true;
                    }
                }
                return false;
            }
        }

        private bool HandleValueType(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            ScriptExpression expression;
            // Enum expressions.
            if(_opcodes.GetTypeInfo(expectedValueType)?.IsEnum == true)
            {
                expression = GetEnumExpression(context, expectedValueType);
            }
            // Other expressions.
            else
            {
                switch (expectedValueType)
                {
                    case "ANY":
                        expression = GetNumberExpression(context);
                        if (expression is null)
                        {
                            expression = GetBooleanExpression(context);

                            if(expression is null)
                            {
                                if (TryCreateObjectExpression(context))
                                {
                                    return true;
                                }

                                throw new CompilerException($"Failed to process \"{context.GetTextSanitized()}\" because it didn't know which value type to expect." +
                                    $" Try using a different function with clearly defined parameters.", context);
                            }
                        }

                        break;

                    case "NUMBER":
                        expression = GetNumberExpression(context);
                        break;

                    case "short":
                        expression = GetShortExpression(context);
                        break;

                    case "long":
                        expression = GetLongExpression(context);
                        break;

                    case "boolean":
                        expression = GetBooleanExpression(context);
                        break;

                    case "real":
                        expression = GetRealExpression(context);
                        break;

                    case "string":
                        expression = GetStringExpression(context);
                        break;

                    case "string_id":
                        expression = CreateSIDExpression(context);
                        break;

                    case "unit_seat_mapping":
                        expression = CreateUnitSeatMappingExpression(context);
                        break;

                    // 16 Bit Index.
                    case "script":
                    case "ai_command_script":
                    case "trigger_volume":
                    case "cutscene_flag":
                    case "cutscene_camera_point":
                    case "cutscene_title":
                    case "starting_profile":
                    case "zone_set":
                    case "designer_zone":
                        expression = GetIndex16Expression(context, expectedValueType);
                        break;

                    // 32 Bit Index.
                    case "folder":
                    case "cinematic_lightprobe":
                        expression = GetIndex32Expression(context, expectedValueType);
                        break;

                    case "ai_line":
                        expression = CreateLineExpression(context);
                        break;

                    case "point_reference":
                        expression = GetPointReferenceExpression(context);
                        break;

                    case "sound":
                    case "effect":
                    case "damage":
                    case "looping_sound":
                    case "animation_graph":
                    case "object_definition":
                    case "bitmap":
                    case "shader":
                    case "render_model":
                    case "structure_definition":
                    case "lightmap_definition":
                    case "cinematic_definition":
                    case "cinematic_scene_definition":
                    case "cinematic_transition_definition":
                    case "bink_definition":
                    case "cui_screen_definition":
                    case "any_tag":
                    case "any_tag_not_resolving":
                        expression = GetTagrefExpression(context, expectedValueType);
                        break;

                    case "device_group":
                        expression = GetDeviceGroupExpression(context);
                        break;

                    case "ai":
                        // H3 and Reach do not share the same AI format.
                        if (_buildInfo.Name.Contains("Reach"))
                        {
                            expression = GetAIExpressionReach(context, expectedValueType);
                        }
                        else
                        {
                            expression = GetAIExpressionH3(context, expectedValueType);
                        }
                        break;

                    case "object_name":
                    case "unit_name":
                    case "vehicle_name":
                    case "weapon_name":
                    case "device_name":
                    case "scenery_name":
                    case "effect_scenery_name":
                        expression = GetObjectNameExpression(context, expectedValueType, expectedValueType);
                        break;
                    default:
                        // Return false if this value type has not been implemented yet and the expression could not be handled.
                        if (_opcodes.GetTypeInfo(expectedValueType) != null)
                        {
                            return false;
                        }
                        // Throw an exception for unknown and misspelled value types.
                        else
                        {
                            throw new CompilerException($"Unknown Value Type: \"{expectedValueType}\". " +
                                $"A type definition might be missing from the scripting XML file or this could be a bug.", context);
                        }
                }
            }

            // Failed to generate an expression.
            if(expression is null)
            {
                return false;
            }

            // Finalize the expression.
            OpenDatumAddExpressionIncrement(expression);
            string actualType = _opcodes.GetTypeInfo(expression.Opcode).Name;
            EqualityPush(actualType);
            return true;
        }

        private bool TryCreateObjectExpression(BS_ReachParser.LiteralContext context)
        {
            CastInfo info = _opcodes.GetTypeCast("object");
            foreach (string type in info.From)
            {
                if (HandleValueType(context, type))
                {
                    return true;
                }
            }

            return false;
        }

        private ScriptExpression GetBooleanExpression(BS_ReachParser.LiteralContext context)
        {

            string text = context.GetTextSanitized();
            if (context.BOOLEAN() == null)
            {
                return null;
            }

            byte val;
            if (text == "true")
            {
                val = 1;
            }
            else if (text == "false")
            {
                val = 0;
            }
            else
            {
                return null;
            }

            ushort opcode = _opcodes.GetTypeInfo("boolean").Opcode;
            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                context.GetCorrectTextPosition(_missingCarriageReturnPositions), (short)context.Start.Line, val);
        }

        private ScriptExpression GetEnumExpression(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string text = context.GetTextSanitized();
            ScriptValueType info = _opcodes.GetTypeInfo(expectedValueType);
            int val = info.GetEnumIndex(text);

            if (val == -1)
            {
                return null;
            }

            if(info.Size == 4)
            {
                return new ScriptExpression(_currentIndex, info.Opcode, info.Opcode, ScriptExpressionType.Expression,
                    _strings.Cache(text.Replace('_', ' ')), (short)context.Start.Line, (uint)val);
            }
            else if(info.Size == 2)
            {
                return new ScriptExpression(_currentIndex, info.Opcode, info.Opcode, ScriptExpressionType.Expression,
                    _strings.Cache(text.Replace('_', ' ')), (short)context.Start.Line, (ushort)val);
            }
            else
            {
                return null;
            }
        }

        private ScriptExpression GetNumberExpression(BS_ReachParser.LiteralContext context)
        {
            // Is the number an integer? The default integer is a short for now.
            if (context.INT() != null)
            {
                string txt = context.GetTextSanitized();

                if (!int.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int num))
                {
                    throw new ArgumentException($"Failed to parse Long. Text: {txt}");

                }
                if (num >= short.MinValue && num <= short.MaxValue)
                {
                    return GetShortExpression(context);
                }
                else
                {
                    return GetLongExpression(context);
                }                               
            }
            // Is the number a real?
            else if (context.FLOAT() != null)
            {
                return GetRealExpression(context);
            }
            else
            {
                return null;

            }
        }

        private ScriptExpression GetObjectNameExpression(BS_ReachParser.LiteralContext context, string valueType, string castTo)
        {
            string name = context.GetTextSanitized();
            if(TryGetObjectFromContext(out ScriptingContextObject obj, Tuple.Create("object_name", name)))
            {
                ushort opcode = _opcodes.GetTypeInfo(valueType).Opcode;
                return new ScriptExpression(_currentIndex, opcode, _opcodes.GetTypeInfo(castTo).Opcode, ScriptExpressionType.Expression,
                    _strings.Cache(name), (short)context.Start.Line, (ushort)obj.Index);
            }
            else
            {
                return null;
            }
        }

        private ScriptExpression GetAIExpressionReach(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string text = context.GetTextSanitized();
            string[] subStrings = text.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
            ushort opcode = _opcodes.GetTypeInfo("ai").Opcode;
            ushort valuetype = _opcodes.GetTypeInfo(expectedValueType).Opcode;

            byte b1;
            byte b2;
            byte b3;
            byte b4;

            #region value generation
            // Squads.
            if (TryGetObjectFromContext(out ScriptingContextObject squadObject, Tuple.Create("ai_squad", subStrings[0])))
            {
                // Squad.
                if(subStrings.Length == 1)
                {
                    b1 = 32;
                    b2 = 0;
                    b3 = 0;
                    b4 = (byte)squadObject.Index;
                }
                // Locations.
                else if(subStrings.Length == 2)
                {
                    // Group Location.
                    if (TryGetChildObjectFromObject(squadObject, "ai_group_location", subStrings[1], out ScriptingContextObject groupLocationObject))
                    {
                        b1 = 160;
                        b2 = (byte)squadObject.Index;
                        b3 = 0;
                        b4 = (byte)groupLocationObject.Index;
                    }
                    // Single Location.
                    else if (TryGetChildObjectFromObject(squadObject, "ai_single_location", subStrings[1], out ScriptingContextObject singleLocationObject))
                    {
                        b1 = 128;
                        b2 = (byte)squadObject.Index;
                        b3 = 0;
                        b4 = (byte)singleLocationObject.Index;
                    }
                    else
                    {
                        return null;
                    }    
                }
                else
                {
                    return null;
                }
            }
            // Squad Groups.
            else if (subStrings.Length == 1 && TryGetObjectFromContext(out ScriptingContextObject groupObject, Tuple.Create("ai_squad_group", subStrings[0])))
            {
                b1 = 64;
                b2 = 0;
                b3 = 0;
                b4 = (byte)groupObject.Index;
            }
            // Objectives.
            else if (TryGetObjectFromContext(out ScriptingContextObject objectiveObject, Tuple.Create("ai_objective", subStrings[0])))
            {
                // Objective.
                if (subStrings.Length == 1)
                {
                    b1 = 223;
                    b2 = 255;
                    b3 = 0;
                    b4 = (byte)objectiveObject.Index;
                }
                else if (subStrings.Length == 2)
                {
                    // Objective Role.
                    if (TryGetChildObjectFromObject(objectiveObject, "ai_role", subStrings[1], out ScriptingContextObject roleObject))
                    {
                        b1 = 192;
                        b2 = (byte)roleObject.Index;
                        b3 = 0;
                        b4 = (byte)objectiveObject.Index;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            // Not an AI object...
            else
            {
                return null;
            }
            #endregion
            return new ScriptExpression(_currentIndex, opcode, valuetype, ScriptExpressionType.Expression, _strings.Cache(text), (short)context.Start.Line, b1, b2, b3, b4);
        }

        private ScriptExpression GetAIExpressionH3(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string text = context.GetTextSanitized();
            string[] subStrings = text.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
            ushort opcode = _opcodes.GetTypeInfo("ai").Opcode;
            ushort valuetype = _opcodes.GetTypeInfo(expectedValueType).Opcode;

            ushort value1;
            ushort value2;

            #region value generation
            // Squads.
            if (TryGetObjectFromContext(out ScriptingContextObject squadObject, Tuple.Create("ai_squad", subStrings[0])))
            {
                // Squad.
                if (subStrings.Length == 1)
                {
                    value1 = 0x2000;
                    value2 = (ushort)squadObject.Index;
                }
                // Locations.
                else if (subStrings.Length == 2)
                {
                    // Starting location.
                    if (TryGetChildObjectFromObject(squadObject, "ai_starting_location", subStrings[1], out ScriptingContextObject startingLocationObject))
                    {
                        byte[] squadIndexBytes = BitConverter.GetBytes((short)squadObject.Index);
                        byte b1 = (byte)(squadIndexBytes[1] | 0x80);
                        byte b2 = squadIndexBytes[0];
                        byte b3 = (byte)startingLocationObject.WrapperIndex;
                        byte b4 = (byte)startingLocationObject.Index;
                        return new ScriptExpression(_currentIndex, opcode, valuetype, ScriptExpressionType.Expression, _strings.Cache(text), (short)context.Start.Line, b1, b2, b3, b4);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            // Squad Groups.
            else if (subStrings.Length == 1 && TryGetObjectFromContext(out ScriptingContextObject groupObject, Tuple.Create("ai_squad_group", subStrings[0])))
            {
                value1 = 0x4000;
                value2 = (ushort)groupObject.Index;
            }
            // Objectives.
            else if (TryGetObjectFromContext(out ScriptingContextObject objectiveObject, Tuple.Create("ai_objective", subStrings[0])))
            {
                // Objective.
                if (subStrings.Length == 1)
                {
                    value1 = 0xBFFF;
                    value2 = (ushort)objectiveObject.Index;
                }
                else if (subStrings.Length == 2)
                {
                    // Objective Role.
                    if (TryGetChildObjectFromObject(objectiveObject, "ai_role", subStrings[1], out ScriptingContextObject roleObject))
                    {
                        value1 = (ushort)(roleObject.Index | 0xA000);
                        value2 = (ushort)objectiveObject.Index;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            // Not an AI object...
            else
            {
                return null;
            }
            #endregion
            return new ScriptExpression(_currentIndex, opcode, valuetype, ScriptExpressionType.Expression, _strings.Cache(text), (short)context.Start.Line, value1, value2);
        }

        private ScriptExpression GetIndex16Expression(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string name = context.GetTextSanitized();
            int value = -1;
            switch (expectedValueType)
            {
                case "script":
                case "ai_command_script":
                    if(_scriptLookup.TryGetValue(name, out List<ScriptInfo> scripts))
                    {
                        // In case of overloaded scripts with matching names, choose the first script's index.
                        value = scripts[0].Opcode;
                    }
                    break;

                case "trigger_volume":
                case "cutscene_flag":
                case "cutscene_camera_point":
                case "cutscene_title":
                case "starting_profile":
                case "zone_set":
                case "designer_zone":
                    if(TryGetObjectFromContext(out ScriptingContextObject obj, Tuple.Create(expectedValueType, name)))
                    {
                        value = obj.Index;
                    }
                    break;
            }
            // No match.
            if (value == -1)
            {
                return null;
            }

            ushort opcode = _opcodes.GetTypeInfo(expectedValueType).Opcode;
            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, 
                _strings.Cache(name), (short)context.Start.Line, (ushort)value);
        }

        private ScriptExpression GetIndex32Expression(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string name = context.GetTextSanitized();

            if(TryGetObjectFromContext(out ScriptingContextObject obj, Tuple.Create(expectedValueType, name)))
            {
                ushort opcode = _opcodes.GetTypeInfo(expectedValueType).Opcode;
                return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                    _strings.Cache(name), (short)context.Start.Line, (uint)obj.Index);
            }
            else
            {
                return null;
            }

        }

        private ScriptExpression GetDeviceGroupExpression(BS_ReachParser.LiteralContext context)
        {
            string name = context.GetTextSanitized();

            if (TryGetObjectFromContext(out ScriptingContextObject deviceObject, Tuple.Create("device_group", name)))
            {
                int index = deviceObject.Index;
                ushort opcode = _opcodes.GetTypeInfo("device_group").Opcode;

                // Value stores a device group datum.
                ushort deviceSalt = (ushort)(SaltGenerator.GetSalt("device groups") + index);
                DatumIndex value = new DatumIndex(deviceSalt, (ushort)index);

                return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, _strings.Cache(name), (short)context.Start.Line, value);
            }
            else
            {
                return null;
            }
        }

        private ScriptExpression GetPointReferenceExpression(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetTextSanitized();
            string[] subStrings = text.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

            if(TryGetObjectFromContext(out ScriptingContextObject setObject, Tuple.Create("point_set", subStrings[0])))
            {
                int set;
                int point;

                // Set.
                if(subStrings.Length == 1)
                {
                    set = setObject.Index;
                    point = -1;
                }
                // Point.
                else if(subStrings.Length == 2 && TryGetChildObjectFromObject(setObject, "point_set_point", subStrings[1], out ScriptingContextObject pointObject))
                {
                    set = setObject.Index;
                    point = pointObject.Index;
                }
                else
                {
                    return null;
                }

                ushort opCode = _opcodes.GetTypeInfo("point_reference").Opcode;
                return new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression,
                    _strings.Cache(text), (short)context.Start.Line, (ushort)set, (ushort)point);
            }
            else
            {
                return null;
            }
        }

        private ScriptExpression GetTagrefExpression(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string text = context.GetTextSanitized();
            string[] subStrings = text.Split(new char[] { '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
            DatumIndex datum;
            ITag tagRef;

            // Strings with optional tag class suffixes have priority.
            if (subStrings.LongCount() == 2)
            {
                StringIDSource stringIDs = _cacheFile.StringIDs;
                ITagGroup tagGroup = _cacheFile.TagGroups.Single(c => stringIDs.GetString(c.Description) == subStrings[1]);
                ITag tag = _cacheFile.Tags.FindTagByName(subStrings[0], tagGroup, _cacheFile.FileNames);
                if (tag != null)
                {
                    datum = tag.Index;
                    tagRef = tag;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                string classMagic = _opcodes.GetTypeInfo(expectedValueType).TagGroup;
                if (classMagic == null)
                {
                    return null;
                }

                ITag tag;
                if (classMagic == "BLAM")
                {
                    tag = _cacheFile.Tags.FindTagByName(subStrings[0], _cacheFile.FileNames);
                }
                else
                {
                    tag = _cacheFile.Tags.FindTagByName(subStrings[0], classMagic, _cacheFile.FileNames);
                }

                if (tag != null)
                {
                    datum = tag.Index;
                    tagRef = tag;
                }
                else
                {
                    return null;
                }
            }

            // Add the referenced tag to the tagref table if it wasn't included yet.
            if(!_references.Contains(tagRef))
            {
                _references.Add(tagRef);
            }

            // Create expression.
            var opcode = _opcodes.GetTypeInfo(expectedValueType).Opcode;
            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                _strings.Cache(text), (short)context.Start.Line, datum);
        }

        private ScriptExpression CreateLineExpression(BS_ReachParser.LiteralContext context)
        {
            string name = context.GetTextSanitized();

            // Default value for expressions where the map doesn't contain a matching script object.
            uint value = 0xFFFFFFFF;

            // Check if this is a simple line reference.
            if (IsNone(name) || _scriptingContext.ContainsObject(Tuple.Create("line", name)))
            {
                value = _cacheFile.StringIDs.FindStringID(name).Value;
            }
            else
            {
                string[] split = name.Split('_');
                if(split.Length > 1)
                {
                    string line = string.Join("_", split, 0, split.Length - 1);
                    string variant = split.Last();

                    if(_scriptingContext.ContainsObject(Tuple.Create("line", line), Tuple.Create("line_variant", variant)))
                    {
                        value = _cacheFile.StringIDs.FindStringID(name).Value;
                    }
                }
            }

            if(name == "")
            {
                value = 0;
                _logger.Warning($"A dialog line didn't have a name.");
            }

            if(value == 0xFFFFFFFF)
            {
                _logger.Warning($"The dialog line \"{name}\" appears to be missing from the map.");
            }

            ushort opcode = _opcodes.GetTypeInfo("ai_line").Opcode;
            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                _strings.Cache(name), (short)context.Start.Line, value);
        }

        private ScriptExpression GetStringExpression(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetText().Trim('"');

            if (!IsNone(text) && context.STRING() == null)
            {
                return null;
            }

            // Replace the escaped quotes, which were inserted by the decompiler, with regular ones.
            uint value = _strings.Cache(ScriptStringHelpers.Unescape(text));
            var opcode = _opcodes.GetTypeInfo("string").Opcode;
            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                value, (short)context.Start.Line, value);
        }

        private ScriptExpression GetLongExpression(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetTextSanitized();
            ushort opcode = _opcodes.GetTypeInfo("long").Opcode;
            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            {
                return null;
            }

            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                context.GetCorrectTextPosition(_missingCarriageReturnPositions), (short)context.Start.Line, (uint)value);
        }

        private ScriptExpression GetShortExpression(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetTextSanitized();
            ushort opcode = _opcodes.GetTypeInfo("short").Opcode;
            if (!short.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out short value))
            {
                return null;
            }

            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                context.GetCorrectTextPosition(_missingCarriageReturnPositions), (short)context.Start.Line, (ushort)value);
        }

        private ScriptExpression GetRealExpression(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetTextSanitized();
            ushort opcode = _opcodes.GetTypeInfo("real").Opcode;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return null;
            }

            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                context.GetCorrectTextPosition(_missingCarriageReturnPositions), (short)context.Start.Line, value);
        }

        private ScriptExpression CreateSIDExpression(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetTextSanitized();
            ushort opcode = _opcodes.GetTypeInfo("string_id").Opcode;
            StringID id = _cacheFile.StringIDs.FindOrAddStringID(text);
            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                _strings.Cache(text), (short)context.Start.Line, id);
        }

        private ScriptExpression CreateUnitSeatMappingExpression(BS_ReachParser.LiteralContext context)
        {
            string name = context.GetTextSanitized();           
            if(!_scriptingContext.TryGetUnitSeatMapping(name, out UnitSeatMapping mapping))
            {
                throw new CompilerException($"Failed to retrieve the information for the Unit Seat Mapping {name}. " +
                    $"Please ensure that the XML file in the \"SeatMappings\" folder for this map contains this mapping.", context);
            }

            ushort opcode = _opcodes.GetTypeInfo("unit_seat_mapping").Opcode;
            return new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, 
                _strings.Cache(mapping.Name), (short)context.Start.Line, (ushort)mapping.Count, (ushort)mapping.Index);
        }
    }
}
