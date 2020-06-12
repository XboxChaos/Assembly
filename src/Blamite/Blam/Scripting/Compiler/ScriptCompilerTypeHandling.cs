using System;
using System.Linq;
using System.Globalization;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {
        private bool ProcessLiteral(BS_ReachParser.LitContext context, string expectedValueType, string initialType)
        {
            CastInfo info = _opcodes.GetTypeCast(expectedValueType);
            if(info != null)
            {                
                if(!info.CastOnly && HandleValueType(context, expectedValueType))
                {
                    return true;
                }
                else
                {
                    foreach(string type in info.From)
                    {   
                        
                        // recursion                                       
                        if(type != initialType && ProcessLiteral(context, type, initialType))
                        {
                            // overwrite the value type of the added expression node with the casted type
                            _expressions[_expressions.Count - 1].ReturnType = _opcodes.GetTypeInfo(initialType).Opcode;
                            return true;
                        }
                    }
                    return false;
                }

            }
            else
            {
                return HandleValueType(context, expectedValueType);
            }
        }

        private bool HandleValueType(BS_ReachParser.LitContext context, string expectedValueType)
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
                    return IsAiLine(context, false);

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
                    throw new NotImplementedException($"Unimplemented Value Type: \"{expectedValueType}\". Line: {context.Start.Line}");
            }
        }

        private bool IsBoolean(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText();
            if (context.BOOLIT() == null)
                return false;

            var opCode = _opcodes.GetTypeInfo("boolean").Opcode;
            byte val;

            if (txt == "true")
            {
                val = 1;
            }
            else if (txt == "false")
            {
                val = 0;
            }
            else
                return false;

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression,
                _randomAddress, val, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush("boolean");
            return true;
        }

        private bool IsEnum32(BS_ReachParser.LitContext context, string valueType, string castTo)
        {
            string txt = context.GetText().Trim('"');
            ScriptValueType info = _opcodes.GetTypeInfo(valueType);
            var index = info.GetEnumIndex(txt);

            if (index == -1)
                return false;

            var exp = new ScriptExpression(_currentIndex, info.Opcode, _opcodes.GetTypeInfo(castTo).Opcode, ScriptExpressionType.Expression,
                _strings.Cache(txt), (uint)index, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush(valueType);
            return true;
        }

        private bool IsEnum16(BS_ReachParser.LitContext context, string expectedValueType)
        {
            string txt = context.GetText().Trim('"');
            ScriptValueType info = _opcodes.GetTypeInfo(expectedValueType);
            var val = info.GetEnumIndex(txt);

            if (val == -1)
                return false;
   
            var exp = new ScriptExpression(_currentIndex, info.Opcode, info.Opcode, ScriptExpressionType.Expression, 
                _strings.Cache(txt), (ushort)val, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush(expectedValueType);
            return true;
        }

        private bool IsNumber(BS_ReachParser.LitContext context)
        {
            // is the number an integer? The default integer is a short for now.
            if (context.INT() != null)
            {
                string txt = context.GetText();

                Int32 num;
                if (!Int32.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out num))
                    throw new ArgumentException($"Failed to parse Long. Text: {txt}");
                if(num >= Int16.MinValue && num <= Int16.MaxValue)
                {
                    return IsShort(context);
                }
                else
                {
                    return IsLong(context);
                }                               
            }
            // is the number a real?
            else if (context.FLOAT() != null)
            {
                return IsReal(context);
            }
            else
                return false;
        }

        private bool IsObject_Name(BS_ReachParser.LitContext context, string valueType, string castTo)
        {
            string name = context.GetText().Trim('"');
            int val = Array.FindIndex(_scriptContext.ObjectReferences, o => o.Name == name);
            // not found
            if (val == -1)
                return false;

            // create expression node
            ushort op = _opcodes.GetTypeInfo(valueType).Opcode;
            var exp = new ScriptExpression(_currentIndex, op, _opcodes.GetTypeInfo(castTo).Opcode, ScriptExpressionType.Expression, 
                _strings.Cache(name), (ushort)val, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush(valueType);
            return true;
        }

        private bool IsAI(BS_ReachParser.LitContext context, string expectedValueType)
        {
            // information
            string txt = context.GetText().Trim('"');
            if (txt == "ai_current_actor")
            {
                throw new NotImplementedException("The keyword ai_current_actor is not supported yet.");
            }
            string[] subStrings = txt.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var opCode = _opcodes.GetTypeInfo("ai").Opcode;
            var valType = _opcodes.GetTypeInfo(expectedValueType).Opcode;

            byte[] values = new byte[4];

            #region value generation
            if (subStrings[0] != "none")       // special case
            { 
                // squads
                int index = Array.FindIndex(_scriptContext.AISquads, sq => sq.Name == subStrings[0]);
                if (index != -1)
                {
                    var children = _scriptContext.AISquads[index].GetChildren(_scriptContext.AISquadSingleLocations);
                    if (subStrings.LongCount() == 2)
                    {
                        int loc = Array.FindIndex(children, c => c.Name == subStrings[1]);
                        if (loc != -1)
                        {
                            // squad/location
                            values[0] = 128;
                            values[1] = (byte)index;
                            values[2] = 0;
                            values[3] = (byte)loc;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        // squad
                        values[0] = 32;
                        values[1] = 0;
                        values[2] = 0;
                        values[3] = (byte)index;
                    }
                }
                else
                {
                    // groups
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
                        //objectives
                        index = Array.FindIndex(_scriptContext.AIObjects, o => o.Name == subStrings[0]);
                        if (index != -1)
                        {
                            var children = _scriptContext.AIObjects[index].GetChildren(_scriptContext.AIObjectWaves);
                            if (subStrings.LongCount() == 2)
                            {
                                int role = Array.FindIndex(children, r => r.Name == subStrings[1]);
                                if (role != -1)
                                {
                                    // objective/role
                                    values[0] = 192;
                                    values[1] = (byte)role;
                                    values[2] = 0;
                                    values[3] = (byte)index;
                                }
                                else
                                    return false;
                            }
                            else
                            {
                                // objective
                                values[0] = 223;
                                values[1] = 255;
                                values[2] = 0;
                                values[3] = (byte)index;
                            }
                        }
                        else
                            return false;                       // no match
                    }
                }
            }
            #endregion
            var exp = new ScriptExpression(_currentIndex, opCode, valType, ScriptExpressionType.Expression, _strings.Cache(txt), values, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush(expectedValueType);
            return true;
        }

        private bool IsIndex16(BS_ReachParser.LitContext context, string expectedValueType)
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
            // no match
            if (val == -1)
                return false;

            var opCode = _opcodes.GetTypeInfo(expectedValueType).Opcode;
            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                _strings.Cache(name), (ushort)val, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush(expectedValueType);
            return true;
        }

        private bool IsDeviceGroup(BS_ReachParser.LitContext context)
        {
            string name = context.GetText().Trim('"');
            int index = Array.FindIndex(_scriptContext.DeviceGroups, t => t.Name == name);
            if (index == -1)
                return false; // not found

            var opCode = _opcodes.GetTypeInfo("device_group").Opcode;

            // value stores a device group datum
            ushort device_salt = (ushort)(SaltGenerator.GetSalt("device groups") + index);
            DatumIndex value = new DatumIndex(device_salt, (ushort)index);

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, _strings.Cache(name), value, (short)context.Start.Line);
            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush("device_group");
            return true;
        }

        private bool IsPointReference(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText().Trim('"');
            string[] subStrings = txt.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

             // find set
            int set = Array.FindIndex(_scriptContext.PointSets, s => s.Name == subStrings[0]);
            if (set != -1)
            {
                int point = -1;
                // check if the string contains a point name
                if(subStrings.Count() == 2)
                {
                    // find point
                    ScriptObject[] points = _scriptContext.PointSets[set].GetChildren(_scriptContext.PointSetPoints);
                    point = Array.FindIndex(points, p => p.Name == subStrings[1]);
                    if (point == -1)
                    {
                        // a matching point couldn't be found
                        return false;
                    }
                }

                // create point_reference
                var opCode = _opcodes.GetTypeInfo("point_reference").Opcode;

                ushort[] value = new ushort[2];
                value[0] = (ushort)set;
                value[1] = (ushort)point;

                var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                    _strings.Cache(txt), value, (short)context.Start.Line);
                _currentIndex.Increment();
                OpenDatumAndAdd(exp);

                EqualityPush("point_reference");
                return true;
            }
            else
                return false;

        }

        private bool IsFolder(BS_ReachParser.LitContext context)
        {
            string name = context.GetText().Trim('"');
            int index = Array.FindIndex(_scriptContext.ObjectFolders, f => f.Name == name);

            // no match
            if (index == -1)
                return false;

            var opCode = _opcodes.GetTypeInfo("folder").Opcode;
            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                _strings.Cache(name), (uint)index, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush("folder");
            return true;
        }

        private bool IsTagref(BS_ReachParser.LitContext context, string expectedValueType)
        {
            string txt = context.GetText().Trim('"');
            string[] subStrings = txt.Split(new char[] { '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
            DatumIndex datum;
            ITag tagRef;

            // strings with optional tag class suffixes have priority
            if (subStrings.LongCount() == 2)
            {
                var ids = _cashefile.StringIDs;
                ITagGroup cla = _cashefile.TagGroups.Single(c => ids.GetString(c.Description) == subStrings[1]);
                ITag tag = _cashefile.Tags.FindTagByName(subStrings[0], cla, _cashefile.FileNames);
                if (tag != null)
                {
                    datum = tag.Index;
                    tagRef = tag;
                }
                else
                    return false;

            }
            else
            {
                string classMagic = _opcodes.GetTypeInfo(expectedValueType).TagGroup;
                if (classMagic == null)
                    return false;

                ITag tag;
                if (classMagic == "BLAM")
                    tag = _cashefile.Tags.FindTagByName(subStrings[0], _cashefile.FileNames);
                else
                    tag = _cashefile.Tags.FindTagByName(subStrings[0], classMagic, _cashefile.FileNames);


                if (tag != null)
                {
                    datum = tag.Index;
                    tagRef = tag;
                }
                else
                    return false;                    
            }

            // add the referenced tag to the tagref table if it wasn't included yet.
            if(!_references.Contains(tagRef))
                _references.Add(tagRef);

            // create expression.
            var opCode = _opcodes.GetTypeInfo(expectedValueType).Opcode;

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression,
                _strings.Cache(txt), datum, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush(expectedValueType);
            return true;
        }

        private bool IsAiLine(BS_ReachParser.LitContext context, bool any)
        {
            string name = context.GetText().Trim('"');
            // check if this is a simple line reference
            bool exists = _scriptContext.AILines.Any(s => s.Name == name);
            // check if a variant was specified
            if (!exists)
            {
                var split = name.Split('_');
                if(split.Count() > 1)
                {
                    string line = string.Join("_", split, 0, split.Count()-1);
                    string variant = split[split.Count()-1];

                    int lineIndex = Array.FindIndex(_scriptContext.AILines, l => l.Name == line);

                    // make sure that this variant exists
                    if(lineIndex != -1)
                    {
                        var variants = _scriptContext.AILines[lineIndex].GetChildren(_scriptContext.AILineVariants);
                        exists = variants.Any(v => v.Name == variant);
                    }
                }
            }
            // don't create an expression node when the line doesn't exist and the compiler is trying to guess the value type
            //if(any && !exists)
            //{
            //    return false;
            //}

            var opCode = _opcodes.GetTypeInfo("ai_line").Opcode;
            uint value;

            if(exists)
            {
                value = _cashefile.StringIDs.FindStringID(name).Value;
            }
            else
            {
                value = 0xFFFFFFFF;
            }

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression,
                _strings.Cache(name), value, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);
            EqualityPush("ai_line");
            return true;
        }

        private bool IsString(BS_ReachParser.LitContext context)
        {
            if (context.STRING() == null)
                return false;

            string txt = context.GetText().Trim('"');
            var opCode = _opcodes.GetTypeInfo("string").Opcode;
            uint value = _strings.Cache(txt);

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression,
                value, value, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            EqualityPush("string");
            return true;
        }

        private bool IsLong(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText();
            var opCode = _opcodes.GetTypeInfo("long").Opcode;
            Int32 value;
            if (!Int32.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                _randomAddress, (uint)value, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);
            EqualityPush("long");
            return true;
        }

        private bool IsShort(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText();
            var opCode = _opcodes.GetTypeInfo("short").Opcode;
            Int16 value;
            if (!Int16.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                _randomAddress, (ushort)value, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);
            EqualityPush("short");
            return true;
        }

        private bool IsReal(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText();
            var opCode = _opcodes.GetTypeInfo("real").Opcode;
            float value;
            if (!float.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                _randomAddress, value, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);
            EqualityPush("real");
            return true;
        }

        private bool IsSID(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText().Trim('"');
            var id = _cashefile.StringIDs.FindStringID(txt);
            if(id == StringID.Null)
            {
                return false;
            }

            var opCode = _opcodes.GetTypeInfo("string_id").Opcode;

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                _strings.Cache(txt), id, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);
            EqualityPush("string_id");
            return true;
        }

        private void CreateSID(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText().Trim('"');
            var opCode = _opcodes.GetTypeInfo("string_id").Opcode;
            StringID id = _cashefile.StringIDs.FindOrAddStringID(txt);
            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression,
                _strings.Cache(txt), id, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);
            EqualityPush("string_id");
        }

        private void CreateUnitSeatMapping(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText().Trim('"');           

            //if (!int.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int contextIndex))
            //{
            //    throw new ArgumentException($"Failed to parse unit seat mapping index. Text: {txt}. Line: {context.Start.Line}");
            //}

            if(!_seatMappings.TryGetValue(txt, out UnitSeatMapping mapping))
            {
                throw new CompilerException($"Failed to retrieve the unit seat mapping information. " +
                    $"Please ensure that the xml file contains the mapping.", context);
            }

            if (mapping.Index < 0 || mapping.Index >= _scriptContext.UnitSeatMappingCount)
            {
                throw new ArgumentException($"Invalid unit seat mapping index. Index: {mapping.Index}. Line: {context.Start.Line}");
            }

            var opCode = _opcodes.GetTypeInfo("unit_seat_mapping").Opcode;
            ushort[] values = new ushort[2];
            values[0] = (ushort)mapping.Count;
            values[1] = (ushort)mapping.Index;

            var exp = new ScriptExpression(_currentIndex, opCode, opCode, ScriptExpressionType.Expression, 
                _strings.Cache(mapping.Name), values, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);
            EqualityPush("unit_seat_mapping");

        }

        /// <summary>
        /// Attempts to predict and create a suitable expression node, solely based on the parser context. 
        /// ONLY USE THIS IF THERE IS ABSOLUTELY NO OTHER INFORMATION TO WORK OFF OF!
        /// </summary>
        /// <param name="context"></param>
        /// <returns>true if a expression node could be created and false if not.</returns>
        private bool GuessValueType(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText().Trim('"');
            if (context.BOOLIT() != null)
            {
                return IsBoolean(context);
            }
            else if(context.FLOAT() != null || context.INT() != null)
            {
                return IsNumber(context);
            }
            #region Hail Mary!
            else
            {
                bool result = IsEnum32(context, "player", "player")
                    || IsEnum32(context, "game_difficulty", "game_difficulty")
                    || IsEnum32(context, "team", "team")
                    || IsEnum32(context, "mp_team", "mp_team")
                    || IsEnum32(context, "controller", "controller")
                    || IsEnum32(context, "actor_type", "actor_type")
                    || IsEnum32(context, "model_state", "model_state")
                    || IsEnum32(context, "event", "event")
                    || IsEnum32(context, "character_physics", "character_physics")
                    || IsEnum32(context, "skull", "skull")
                    || IsEnum32(context, "firing_point_evaluator", "firing_point_evaluator")
                    || IsEnum32(context, "damage_region", "damage_region")
                    || IsEnum32(context, "actor_type", "actor_type")
                    || IsEnum32(context, "model_state", "model_state")
                    || IsEnum32(context, "event", "event")
                    || IsEnum32(context, "character_physics", "character_physics")
                    || IsEnum16(context, "button_preset")
                    || IsEnum16(context, "joystick_preset")
                    || IsEnum16(context, "player_color")
                    || IsEnum16(context, "player_model_choice")
                    || IsEnum16(context, "voice_output_setting")
                    || IsEnum16(context, "voice_mask")
                    || IsEnum16(context, "subtitle_setting")
                    || IsIndex16(context, "script")
                    //|| IsIndex16(context, "ai_command_script")
                    || IsIndex16(context, "trigger_volume")
                    || IsIndex16(context, "cutscene_flag")
                    || IsIndex16(context, "cutscene_camera_point")
                    || IsIndex16(context, "cutscene_title")
                    || IsIndex16(context, "starting_profile")
                    || IsIndex16(context, "zone_set")
                    || IsIndex16(context, "designer_zone")
                    || IsObject_Name(context, "Object_name", "Object_name")
                    || IsAI(context, "ai")
                    || IsAiLine(context, true)
                    || IsPointReference(context)
                    || IsFolder(context)
                    || IsDeviceGroup(context)
                    || IsTagref(context, "any_tag")
                    || IsSID(context)
                    || IsString(context);
                return result;
            }
            #endregion
        }
    }
}
