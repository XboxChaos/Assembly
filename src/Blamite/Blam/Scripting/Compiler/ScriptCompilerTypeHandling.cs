using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Blamite.Util;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {
        private bool ProcessLiteral(string expectedValueType, BS_ReachParser.LiteralContext context)
        {
            CastInfo info = _opcodes.GetTypeCast(expectedValueType);
            // Types which can be casted to.
            if (info != null)
            {
                // Check if casting is necessary.
                if (HandleValueType(context, expectedValueType))
                {
                    return true;
                }
                // Casting.
                else
                {
                    FIFOStack<string> remainingTypes = new FIFOStack<string>();
                    List<string> processedTypes = new List<string>();

                    // Push immediate casts.
                    foreach (string type in info.From)
                    {
                        remainingTypes.Push(type);
                    }

                    while (remainingTypes.Count > 0)
                    {
                        // Check if this is the right value type.
                        string typeToProcess = remainingTypes.Pop();
                        if (HandleValueType(context, typeToProcess))
                        {
                            // Overwrite the value type of the added expression node with the casted type.
                            _expressions[_expressions.Count - 1].ReturnType = _opcodes.GetTypeInfo(expectedValueType).Opcode;
                            return true;
                        }
                        processedTypes.Add(typeToProcess);
                        info = _opcodes.GetTypeCast(typeToProcess);
                        if (info !=  null)
                        {
                            // Push nested casts.
                            foreach (string type in info.From)
                            {
                                if (!remainingTypes.Contains(type) && !processedTypes.Contains(type))
                                {
                                    remainingTypes.Push(type);
                                }
                            }
                        }
                    }
                    return false;
                }
            }
            // This type can't be casted to.
            else
            {
                return HandleValueType(context, expectedValueType);
            }
        }

        private bool HandleValueType(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string name = context.GetText();
            switch (expectedValueType)
            {
                case "ANY":
                    if(IsNumber(context))
                    {
                        return true;
                    }
                    else
                    {
                        throw new CompilerException($"The Compiler encountered the literal {name} which could be of ANY value type." +
                                "Guessing the value type would lead to too many inaccuracies.", context);
                    }

                case "NUMBER": 
                    return IsNumber(context);

                case "short":
                    return IsShort(context);

                case "long":
                    return IsLong(context);

                case "boolean":
                    return IsBoolean(context);

                case "real":
                    return IsReal(context);

                case "string":
                    return IsString(context);

                case "string_id":
                    CreateSID(context);
                    return true;

                case "unit_seat_mapping":
                    CreateUnitSeatMapping(context);
                    return true;

                case "script":
                case "ai_command_script":
                case "trigger_volume":
                case "cutscene_flag":
                case "cutscene_camera_point":
                case "cutscene_title":
                case "starting_profile":
                case "zone_set":
                case "designer_zone":
                    return IsIndex16(context, expectedValueType);

                case "ai_line":
                    return IsAiLine(context);

                case "point_reference":
                    return IsPointReference(context);

                case "object_list":
                    return IsObject_Name(context, "object_name", expectedValueType) || IsEnum32(context, "player", expectedValueType);

                case "folder":
                    return IsFolder(context);

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
                    return IsTagref(context, expectedValueType);

                case "device_group":
                    return IsDeviceGroup(context);

                case "ai":
                    return IsAI(context, expectedValueType);

                case "player":
                case "game_difficulty":
                case "team":
                case "mp_team":
                case "controller":
                case "actor_type":
                case "model_state":
                case "event":
                case "character_physics":
                case "skull":
                case "firing_point_evaluator":
                case "damage_region":
                    return IsEnum32(context, expectedValueType, expectedValueType);

                case "button_preset":
                case "joystick_preset":
                case "player_color":
                case "player_model_choice":
                case "voice_output_setting":
                case "voice_mask":
                case "subtitle_setting":
                    return IsEnum16(context, expectedValueType);

                case "object_name":
                case "unit_name":
                case "vehicle_name":
                case "weapon_name":
                case "device_name":
                case "scenery_name":
                case "effect_scenery_name":
                    return (IsObject_Name(context, expectedValueType, expectedValueType));

                default:
                    if(_opcodes.GetTypeInfo(expectedValueType) != null)
                    {
                        return false;
                    }
                    else
                    {
                        throw new CompilerException($"Unknown Value Type: \"{expectedValueType}\".", context);
                    }                   
            }
        }

        private bool IsBoolean(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetText();
            if (context.BOOLEAN() == null)
            {
                return false;
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
                return false;
            }

            ushort opcode = _opcodes.GetTypeInfo("boolean").Opcode;
            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                _randomAddress, val, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush("boolean");
            return true;
        }

        private bool IsEnum32(BS_ReachParser.LiteralContext context, string valueType, string castTo)
        {
            string text = context.GetText().Trim('"');
            ScriptValueType info = _opcodes.GetTypeInfo(valueType);
            int index = info.GetEnumIndex(text);

            if (index == -1)
            {
                return false;

            }

            var expression = new ScriptExpression(_currentIndex, info.Opcode, _opcodes.GetTypeInfo(castTo).Opcode, ScriptExpressionType.Expression,
                _strings.Cache(text), (uint)index, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush(valueType);
            return true;
        }

        private bool IsEnum16(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string text = context.GetText().Trim('"');
            ScriptValueType info = _opcodes.GetTypeInfo(expectedValueType);
            int val = info.GetEnumIndex(text);

            if (val == -1)
            {
                return false;

            }

            var expression = new ScriptExpression(_currentIndex, info.Opcode, info.Opcode, ScriptExpressionType.Expression, 
                _strings.Cache(text), (ushort)val, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush(expectedValueType);
            return true;
        }

        private bool IsNumber(BS_ReachParser.LiteralContext context)
        {
            // Is the number an integer? The default integer is a short for now.
            if (context.INT() != null)
            {
                string txt = context.GetText();

                if (!int.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int num))
                {
                    throw new ArgumentException($"Failed to parse Long. Text: {txt}");

                }
                if (num >= short.MinValue && num <= short.MaxValue)
                {
                    return IsShort(context);
                }
                else
                {
                    return IsLong(context);
                }                               
            }
            // Is the number a real?
            else if (context.FLOAT() != null)
            {
                return IsReal(context);
            }
            else
            {
                return false;

            }
        }

        private bool IsObject_Name(BS_ReachParser.LiteralContext context, string valueType, string castTo)
        {
            string name = context.GetText().Trim('"');
            int val = Array.FindIndex(_scriptContext.ObjectReferences, o => o.Name == name);

            // Not found.
            if (val == -1)
            {
                return false;

            }

            // Create expression node.
            ushort opcode = _opcodes.GetTypeInfo(valueType).Opcode;
            var expression = new ScriptExpression(_currentIndex, opcode, _opcodes.GetTypeInfo(castTo).Opcode, ScriptExpressionType.Expression, 
                _strings.Cache(name), (ushort)val, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush(valueType);
            return true;
        }

        private bool IsAI(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string text = context.GetText().Trim('"');
            string[] subStrings = text.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
            ushort opcode = _opcodes.GetTypeInfo("ai").Opcode;
            ushort valuetype = _opcodes.GetTypeInfo(expectedValueType).Opcode;

            byte[] values = new byte[4];
            #region value generation
            // Squads.
            int index = Array.FindIndex(_scriptContext.AISquads, squad => squad.Name == subStrings[0]);
            if (index != -1)
            {
                if (subStrings.LongCount() == 2)
                {
                    var singleLocations = _scriptContext.AISquads[index].GetChildren(_scriptContext.AISquadSingleLocations);
                    int singleIndex = Array.FindIndex(singleLocations, c => c.Name == subStrings[1]);
                    if (singleIndex != -1)
                    {
                        // Squad/single_location.
                        values[0] = 128;
                        values[1] = (byte)index;
                        values[2] = 0;
                        values[3] = (byte)singleIndex;
                    }
                    else
                    {
                        var groupLocations = _scriptContext.AISquads[index].GetChildren(_scriptContext.AISquadGroupLocations);
                        int groupIndex = Array.FindIndex(groupLocations, c => c.Name == subStrings[1]);
                        if(groupIndex != -1)
                        {
                            // Squad/group_location.
                            values[0] = 160;
                            values[1] = (byte)index;
                            values[2] = 0;
                            values[3] = (byte)groupIndex;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // Squad.
                    values[0] = 32;
                    values[1] = 0;
                    values[2] = 0;
                    values[3] = (byte)index;
                }
            }
            else
            {
                // Groups.
                index = Array.FindIndex(_scriptContext.AISquadGroups, gr => gr.Name == subStrings[0]);
                if (index != -1)
                {
                    values[0] = 64;
                    values[1] = 0;
                    values[2] = 0;
                    values[3] = (byte)index;
                }
                else
                {
                    // Objectives.
                    index = Array.FindIndex(_scriptContext.AIObjects, o => o.Name == subStrings[0]);
                    if (index != -1)
                    {
                        var children = _scriptContext.AIObjects[index].GetChildren(_scriptContext.AIObjectWaves);
                        if (subStrings.LongCount() == 2)
                        {
                            int role = Array.FindIndex(children, r => r.Name == subStrings[1]);
                            if (role != -1)
                            {
                                // Objective/role.
                                values[0] = 192;
                                values[1] = (byte)role;
                                values[2] = 0;
                                values[3] = (byte)index;
                            }
                            else
                            {
                                return false;

                            }
                        }
                        else
                        {
                            // Objective.
                            values[0] = 223;
                            values[1] = 255;
                            values[2] = 0;
                            values[3] = (byte)index;
                        }
                    }
                    // No match.
                    else
                    {
                        return false;
                    }
                }
            }
            #endregion
            var expression = new ScriptExpression(_currentIndex, opcode, valuetype, ScriptExpressionType.Expression, _strings.Cache(text), values, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush(expectedValueType);
            return true;
        }

        private bool IsIndex16(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string name = context.GetText().Trim('"');
            int val = -1;
            switch (expectedValueType)
            {
                case "script":
                case "ai_command_script":
                    val = _scriptLookup.Values.First(i => i.Name == name).Opcode;
                    break;

                case "trigger_volume":
                    val = Array.FindIndex(_scriptContext.TriggerVolumes, t => t.Name == name);
                    break;

                case "cutscene_flag":
                    val = Array.FindIndex(_scriptContext.CutsceneFlags, t => t.Name == name);
                    break;

                case "cutscene_camera_point":
                    val = Array.FindIndex(_scriptContext.CutsceneCameraPoints, t => t.Name == name);
                    break;

                case "cutscene_title":
                    val = Array.FindIndex(_scriptContext.CutsceneTitles, t => t.Name == name);
                    break;

                case "starting_profile":
                    val = Array.FindIndex(_scriptContext.StartingProfiles, t => t.Name == name);
                    break;

                case "zone_set":
                    val = Array.FindIndex(_scriptContext.ZoneSets, t => t.Name == name);
                    break;

                case "designer_zone":
                    val = Array.FindIndex(_scriptContext.DesignerZones, t => t.Name == name);
                    break;
            }
            // No match.
            if (val == -1)
            {
                return false;
            }

            ushort opcode = _opcodes.GetTypeInfo(expectedValueType).Opcode;
            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, 
                _strings.Cache(name), (ushort)val, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush(expectedValueType);
            return true;
        }

        private bool IsDeviceGroup(BS_ReachParser.LiteralContext context)
        {
            string name = context.GetText().Trim('"');
            int index = Array.FindIndex(_scriptContext.DeviceGroups, t => t.Name == name);

            // Not found.
            if (index == -1)
            {
                return false;

            }

            // Value stores a device group datum.
            ushort deviceSalt = (ushort)(SaltGenerator.GetSalt("device groups") + index);
            DatumIndex value = new DatumIndex(deviceSalt, (ushort)index);

            ushort opcode = _opcodes.GetTypeInfo("device_group").Opcode;
            var exp = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, _strings.Cache(name), value, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(exp);

            EqualityPush("device_group");
            return true;
        }

        private bool IsPointReference(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetText().Trim('"');
            string[] subStrings = text.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

             // Find set.
            int set = Array.FindIndex(_scriptContext.PointSets, s => s.Name == subStrings[0]);
            if (set != -1)
            {
                // Check if the string contains a point name.
                int point = -1;
                if(subStrings.Count() == 2)
                {
                    // Find point.
                    ScriptObject[] points = _scriptContext.PointSets[set].GetChildren(_scriptContext.PointSetPoints);
                    point = Array.FindIndex(points, p => p.Name == subStrings[1]);
                    if (point == -1)
                    {
                        // A matching point wasn't found.
                        {
                            return false;
                        }
                    }
                }

                // Create point_reference.
                ushort opCode = _opcodes.GetTypeInfo("point_reference").Opcode;
                ushort[] value = new ushort[2];
                value[0] = (ushort)set;
                value[1] = (ushort)point;
                var expression = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                    _strings.Cache(text), value, (short)context.Start.Line);
                OpenDatumAddExpressionIncrement(expression);

                EqualityPush("point_reference");
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsFolder(BS_ReachParser.LiteralContext context)
        {
            string name = context.GetText().Trim('"');
            int index = Array.FindIndex(_scriptContext.ObjectFolders, f => f.Name == name);

            // No match.
            if (index == -1)
            {
                return false;
            }

            ushort opCode = _opcodes.GetTypeInfo("folder").Opcode;
            var expression = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                _strings.Cache(name), (uint)index, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush("folder");
            return true;
        }

        private bool IsTagref(BS_ReachParser.LiteralContext context, string expectedValueType)
        {
            string text = context.GetText().Trim('"');
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
                    return false;
                }
            }
            else
            {
                string classMagic = _opcodes.GetTypeInfo(expectedValueType).TagGroup;
                if (classMagic == null)
                {
                    return false;
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
                    return false;
                }
            }

            // Add the referenced tag to the tagref table if it wasn't included yet.
            if(!_references.Contains(tagRef))
            {
                _references.Add(tagRef);
            }

            // Create expression.
            var opcode = _opcodes.GetTypeInfo(expectedValueType).Opcode;
            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                _strings.Cache(text), datum, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush(expectedValueType);
            return true;
        }

        private bool IsAiLine(BS_ReachParser.LiteralContext context)
        {
            string name = context.GetText().Trim('"');
            // Check if this is a simple line reference.
            bool exists = _scriptContext.AILines.Any(s => s.Name == name);
            // Maybe a variant was specified.
            if (!exists)
            {
                var split = name.Split('_');
                if(split.Count() > 1)
                {
                    string line = string.Join("_", split, 0, split.Count()-1);
                    string variant = split[split.Count()-1];
                    int lineIndex = Array.FindIndex(_scriptContext.AILines, l => l.Name == line);

                    // Make sure that this variant exists
                    if(lineIndex != -1)
                    {
                        ScriptObject[] variants = _scriptContext.AILines[lineIndex].GetChildren(_scriptContext.AILineVariants);
                        exists = variants.Any(v => v.Name == variant);
                    }
                }
            }

            uint value;
            if(exists || name == "none")
            {
                value = _cacheFile.StringIDs.FindStringID(name).Value;
            }
            else
            {
                value = 0xFFFFFFFF;
            }

            ushort opcode = _opcodes.GetTypeInfo("ai_line").Opcode;
            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                _strings.Cache(name), value, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush("ai_line");
            return true;
        }

        private bool IsString(BS_ReachParser.LiteralContext context)
        {
            if (context.STRING() == null)
            {
                return false;
            }

            string text = context.GetText().Trim('"');
            var opcode = _opcodes.GetTypeInfo("string").Opcode;
            uint value = _strings.Cache(text);
            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                value, value, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush("string");
            return true;
        }

        private bool IsLong(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetText();
            ushort opcode = _opcodes.GetTypeInfo("long").Opcode;
            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            {
                return false;
            }

            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, 
                _randomAddress, (uint)value, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush("long");
            return true;
        }

        private bool IsShort(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetText();
            ushort opcode = _opcodes.GetTypeInfo("short").Opcode;
            if (!short.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out short value))
            {
                return false;
            }

            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, 
                _randomAddress, (ushort)value, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush("short");
            return true;
        }

        private bool IsReal(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetText();
            ushort opcode = _opcodes.GetTypeInfo("real").Opcode;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return false;
            }

            var expressison = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, 
                _randomAddress, value, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expressison);

            EqualityPush("real");
            return true;
        }

        private void CreateSID(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetText().Trim('"');
            ushort opcode = _opcodes.GetTypeInfo("string_id").Opcode;
            StringID id = _cacheFile.StringIDs.FindOrAddStringID(text);
            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression,
                _strings.Cache(text), id, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush("string_id");
        }

        private void CreateUnitSeatMapping(BS_ReachParser.LiteralContext context)
        {
            string text = context.GetText().Trim('"');           
            if(!_seatMappings.TryGetValue(text, out UnitSeatMapping mapping))
            {
                throw new CompilerException($"Failed to retrieve the unit seat mapping information. " +
                    $"Please ensure that the xml file contains the mapping.", context);
            }

            if (mapping.Index < 0 || mapping.Index >= _scriptContext.UnitSeatMappingCount)
            {
                throw new ArgumentException($"Invalid unit seat mapping index. Index: {mapping.Index}. Line: {context.Start.Line}");
            }

            ushort opcode = _opcodes.GetTypeInfo("unit_seat_mapping").Opcode;
            ushort[] value = new ushort[2];
            value[0] = (ushort)mapping.Count;
            value[1] = (ushort)mapping.Index;
            var expression = new ScriptExpression(_currentIndex, opcode, opcode, ScriptExpressionType.Expression, 
                _strings.Cache(mapping.Name), value, (short)context.Start.Line);
            OpenDatumAddExpressionIncrement(expression);

            EqualityPush("unit_seat_mapping");
        }
    }
}
