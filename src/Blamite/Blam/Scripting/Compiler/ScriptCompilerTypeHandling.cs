using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Blamite.Blam.Scripting.Compiler.Expressions;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {

        private bool HandleValueType(BS_ReachParser.LitContext context, string expectedValueType)
        {
            string name = context.GetText();

            switch (expectedValueType)
            {
                case "ANY":
                    return GuessValueType(context);

                case "NUMBER": 
                    return IsNumber(context);

                case "short":
                    CreateShort(context);
                    return true;

                case "long":
                    CreateLong(context);
                    return true;

                case "boolean":
                    return IsBoolean(context);

                case "real":
                    CreateReal(context);
                    return true;

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
                    if (IsIndex16(context, expectedValueType))
                        return true;
                    else
                        throw new ArgumentException($"Failed to retrieve the {expectedValueType} index. Name: {name}. Line: {context.Start.Line}");

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
                    if (IsAI(context, expectedValueType))
                        return true;
                    else
                        throw new ArgumentException($"Failed to retrieve a matching AI. Name: \"{name}\". Line: {context.Start.Line}");

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

                case "object":
                    return IsObject_Name(context, "object_name", "object") || IsAI(context, "object") || IsEnum32(context, "player", "object");

                case "unit":
                    return IsObject_Name(context, "unit_name", "unit") || IsAI(context, "unit") || IsEnum32(context, "player", "unit");

                case "vehicle":
                    return IsObject_Name(context, "vehicle_name", "vehicle") || IsAI(context, "vehicle");

                case "weapon":
                    return IsObject_Name(context, "weapon_name", "weapon");
                case "device":
                    return IsObject_Name(context, "device_name", "device");
                case "scenery":
                    return IsObject_Name(context, "scenery_name", "scenery");
                case "effect_scenery":
                    return IsObject_Name(context, "effect_scenery_name", "effect_scenery");


                case "object_name":
                case "unit_name":
                case "vehicle_name":
                case "weapon_name":
                case "device_name":
                case "scenery_name":
                case "effect_scenery_name":
                    if (IsObject_Name(context, expectedValueType, expectedValueType))
                        return true;
                    else
                        throw new ArgumentException($"Failed to retrieve a matching object name. Name: {name}. Line: {context.Start.Line}");

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
            var exp = new Expression8(_currentSalt, opCode, opCode, _randomAddress, (short)context.Start.Line);
            if (txt == "true")
            {
                exp.Values[0] = 1;
            }
            else if (txt == "false")
            {
                exp.Values[0] = 0;
            }
            else
                return false;

            IncrementDatum();
            OpenDatumAndAdd(exp);

            return true;
        }

        private bool IsEnum32(BS_ReachParser.LitContext context, string valueType, string castTo)
        {
            string txt = context.GetText().Trim('"');
            ScriptValueType info = _opcodes.GetTypeInfo(valueType);
            var index = info.GetEnumIndex(txt);

            if (index == -1)
                return false;

            Expression32 exp = new Expression32(_currentSalt, info.Opcode, _opcodes.GetTypeInfo(castTo).Opcode, _strings.Cache(txt), (short)context.Start.Line);
            exp.Value = index;

            IncrementDatum();
            OpenDatumAndAdd(exp);

            return true;
        }

        private bool IsEnum16(BS_ReachParser.LitContext context, string expectedValueType)
        {
            string txt = context.GetText().Trim('"');
            ScriptValueType info = _opcodes.GetTypeInfo(expectedValueType);
            var index = info.GetEnumIndex(txt);

            if (index == -1)
                return false;

            Expression16 exp = new Expression16(_currentSalt, info.Opcode, info.Opcode, _strings.Cache(txt), (short)context.Start.Line);
            exp.Values[0] = (Int16)index;

            IncrementDatum();
            OpenDatumAndAdd(exp);

            return true;
        }

        private bool IsNumber(BS_ReachParser.LitContext context)
        {
            // is the number an integer? The default integer is a long for now.
            if (context.INT() != null)
            {
                CreateLong(context);
                return true;
            }
            // if the number a real?
            else if (context.FLOAT() != null)
            {
                CreateReal(context);
                return true;
            }
            else
                return false;
        }

        private bool IsObject_Name(BS_ReachParser.LitContext context, string valueType, string castTo)
        {
            string name = context.GetText().Trim('"');
            int index = Array.FindIndex(_scriptContext.ObjectNames, o => o.Name == name);
            // not found
            if (index == -1)
                return false;

            // create expression node
            ushort op = _opcodes.GetTypeInfo(valueType).Opcode;
            Expression16 obj = new Expression16(_currentSalt, op, _opcodes.GetTypeInfo(castTo).Opcode, _strings.Cache(name), (short)context.Start.Line);
            obj.Values[0] = (Int16)index;
            IncrementDatum();
            OpenDatumAndAdd(obj);

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
            Expression8 exp = new Expression8();

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
                            exp.Values[0] = 128;
                            exp.Values[1] = (byte)index;
                            exp.Values[2] = 0;
                            exp.Values[3] = (byte)loc;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        // squad
                        exp.Values[0] = 32;
                        exp.Values[1] = 0;
                        exp.Values[2] = 0;
                        exp.Values[3] = (byte)index;
                    }
                }
                else
                {
                    // groups
                    index = Array.FindIndex(_scriptContext.AISquadGroups, gr => gr.Name == subStrings[0]);
                    if (index != -1)
                    {
                        exp.Values[0] = 64;
                        exp.Values[1] = 0;
                        exp.Values[2] = 0;
                        exp.Values[3] = (byte)index;
                    }
                    else
                    {
                        //objectives
                        index = Array.FindIndex(_scriptContext.AIObjectives, o => o.Name == subStrings[0]);
                        if (index != -1)
                        {
                            var children = _scriptContext.AIObjectives[index].GetChildren(_scriptContext.AIObjectiveRoles);
                            if (subStrings.LongCount() == 2)
                            {
                                int role = Array.FindIndex(children, r => r.Name == subStrings[1]);
                                if (role != -1)
                                {
                                    // objective/role
                                    exp.Values[0] = 192;
                                    exp.Values[1] = (byte)role;
                                    exp.Values[2] = 0;
                                    exp.Values[3] = (byte)index;
                                }
                                else
                                    return false;
                            }
                            else
                            {
                                // objective
                                exp.Values[0] = 223;
                                exp.Values[1] = 255;
                                exp.Values[2] = 0;
                                exp.Values[3] = (byte)index;
                            }
                        }
                        else
                            return false;                       // no match
                    }
                }
            }
            #endregion

            exp.SetCommonValues(_currentSalt, opCode, valType, _strings.Cache(txt), (short)context.Start.Line);

            IncrementDatum();
            OpenDatumAndAdd(exp);
            return true;
        }

        private bool IsIndex16(BS_ReachParser.LitContext context, string expectedValueType)
        {
            string name = context.GetText().Trim('"');
            int index = -1;
            switch (expectedValueType)
            {
                case "script":
                case "ai_command_script":
                    index = _scriptLookup.FindIndex(s => s.Name == name);
                    break;

                case "trigger_volume":
                    index = Array.FindIndex(_scriptContext.TriggerVolumes, t => t.Name == name);
                    break;

                case "cutscene_flag":
                    index = Array.FindIndex(_scriptContext.CutsceneFlags, t => t.Name == name);
                    break;

                case "cutscene_camera_point":
                    index = Array.FindIndex(_scriptContext.CutsceneCameraPoints, t => t.Name == name);
                    break;

                case "cutscene_title":
                    index = Array.FindIndex(_scriptContext.CutsceneTitles, t => t.Name == name);
                    break;

                case "starting_profile":
                    index = Array.FindIndex(_scriptContext.StartingProfiles, t => t.Name == name);
                    break;

                case "zone_set":
                    index = Array.FindIndex(_scriptContext.ZoneSets, t => t.Name == name);
                    break;

                case "designer_zone":
                    index = Array.FindIndex(_scriptContext.DesignerZones, t => t.Name == name);
                    break;
            }
            // no match
            if (index == -1)
                return false;

            var opCode = _opcodes.GetTypeInfo(expectedValueType).Opcode;
            Expression16 exp = new Expression16(_currentSalt, opCode, opCode, _strings.Cache(name), (short)context.Start.Line);
            exp.Values[0] = (Int16)index;

            IncrementDatum();
            OpenDatumAndAdd(exp);

            return true;
        }

        private bool IsDeviceGroup(BS_ReachParser.LitContext context)
        {
            string name = context.GetText().Trim('"');
            int index = Array.FindIndex(_scriptContext.DeviceGroups, t => t.Name == name);
            if (index == -1)
                return false; // not found

            var opCode = _opcodes.GetTypeInfo("device_group").Opcode;
            Expression16 exp = new Expression16(_currentSalt, opCode, opCode, _strings.Cache(name), (short)context.Start.Line);
            exp.Values[0] = (Int16)(-6812 + index); // guessing. no idea what this value represents. could be a salt.
            exp.Values[1] = (Int16)index;
            IncrementDatum();
            OpenDatumAndAdd(exp);
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
                Expression16 exp = new Expression16(_currentSalt, opCode, opCode, _strings.Cache(txt), (short)context.Start.Line);
                exp.Values[0] = (Int16)set;
                exp.Values[1] = (Int16)point;
                IncrementDatum();
                OpenDatumAndAdd(exp);
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
            Expression32 exp = new Expression32(_currentSalt, opCode, opCode, _strings.Cache(name), (short)context.Start.Line);
            exp.Value = index;

            IncrementDatum();
            OpenDatumAndAdd(exp);

            return true;
        }

        private bool IsTagref(BS_ReachParser.LitContext context, string expectedValueType)
        {
            string txt = context.GetText().Trim('"');
            string[] subStrings = txt.Split(new char[] { '.' }, 2, StringSplitOptions.RemoveEmptyEntries);
            DatumIndex index;
            ITag tagRef;


            // strings with optional tag class suffixes have priority
            if (subStrings.LongCount() == 2)
            {
                var ids = _cashefile.StringIDs;
                ITagClass cla = _cashefile.TagClasses.Single(c => ids.GetString(c.Description) == subStrings[1]);
                ITag tag = _cashefile.Tags.FindTagByName(subStrings[0], cla, _cashefile.FileNames);
                if (tag != null)
                {
                    index = tag.Index;
                    tagRef = tag;
                }
                else
                    return false;

            }
            else
            {
                string classMagic = _opcodes.GetTypeInfo(expectedValueType).TagClass;
                if (classMagic == null)
                    return false;

                ITag tag;
                if (classMagic == "BLAM")
                    tag = _cashefile.Tags.FindTagByName(subStrings[0], _cashefile.FileNames);
                else
                    tag = _cashefile.Tags.FindTagByName(subStrings[0], classMagic, _cashefile.FileNames);


                if (tag != null)
                {
                    index = tag.Index;
                    tagRef = tag;
                }
                else
                    return false;                    
            }

            // add the referenced tag to the tagref table
            _references.Add(tagRef);

            // create expression
            var opCode = _opcodes.GetTypeInfo(expectedValueType).Opcode;
            TagReferenceExpression exp = new TagReferenceExpression(_currentSalt, opCode, opCode, _strings.Cache(txt), (short)context.Start.Line);
            exp.Value = index;

            IncrementDatum();
            OpenDatumAndAdd(exp);

            return true;
        }

        private bool IsAiLine(BS_ReachParser.LitContext context)
        {
            string name = context.GetText().Trim('"');
            int index = Array.FindIndex(_scriptContext.AILines, l => l.Name == name);
            if (index == -1)
                return false;

            var opCode = _opcodes.GetTypeInfo("ai_line").Opcode;
            StringIDExpression exp = new StringIDExpression(_currentSalt, opCode, opCode, _strings.Cache(name), (short)context.Start.Line);
            exp.Value = _cashefile.StringIDs.FindOrAddStringID(name);

            IncrementDatum();
            OpenDatumAndAdd(exp);

            return true;
        }

        private bool IsString(BS_ReachParser.LitContext context)
        {
            if (context.STRING() == null)
                return false;

            string txt = context.GetText().Trim('"');
            var opCode = _opcodes.GetTypeInfo("string").Opcode;
            Expression32 exp = new Expression32(_currentSalt, opCode, opCode, _strings.Cache(txt), (short)context.Start.Line);
            exp.Value = (int)exp.StringAddress;

            IncrementDatum();
            OpenDatumAndAdd(exp);

            return true;
        }

        private void CreateSID(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText().Trim('"');
            var opCode = _opcodes.GetTypeInfo("string_id").Opcode;
            StringIDExpression exp = new StringIDExpression(_currentSalt, opCode, opCode, _strings.Cache(txt), (short)context.Start.Line);
            exp.Value = _cashefile.StringIDs.FindOrAddStringID(txt);
            IncrementDatum();
            OpenDatumAndAdd(exp);
        }

        private void CreateLong(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText();
            var opCode = _opcodes.GetTypeInfo("long").Opcode;
            Expression32 exp = new Expression32(_currentSalt, opCode, opCode, _randomAddress, (short)context.Start.Line);
            Int32 result;
            if (!Int32.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result))
                throw new ArgumentException($"Failed to parse Long. Text: {txt}");
            exp.Value = result;
            IncrementDatum();
            OpenDatumAndAdd(exp);
        }

        private void CreateShort(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText();
            var opCode = _opcodes.GetTypeInfo("short").Opcode;
            Expression16 exp = new Expression16(_currentSalt, opCode, opCode, _randomAddress, (short)context.Start.Line);
            Int16 result;
            if(txt == "65535")
            {
                result = -1;
            }
            else if (!Int16.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result))
            {
                throw new CompilerException($"Failed to parse short. Text: {txt}", context);
            }
            exp.Values[0] = result;
            IncrementDatum();
            OpenDatumAndAdd(exp);
        }

        private void CreateReal(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText();
            var opCode = _opcodes.GetTypeInfo("real").Opcode;
            var exp = new RealExpression(_currentSalt, opCode, opCode, (short)context.Start.Line);
            float result;
            if (!float.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                throw new ArgumentException($"Failed to parse float. Text: {txt}");

            exp.Value = result;
            IncrementDatum();
            OpenDatumAndAdd(exp);
        }

        private void CreateUnitSeatMapping(BS_ReachParser.LitContext context)
        {
            string txt = context.GetText();           
            Int32 contextIndex;

            if (!Int32.TryParse(txt, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out contextIndex))
            {
                throw new ArgumentException($"Failed to parse unit seat mapping index. Text: {txt}. Line: {context.Start.Line}");
            }

            if (contextIndex < 0 || contextIndex >= _scriptContext.UnitSeatMappingCount)
            {
                throw new ArgumentException($"Invalid unit seat mapping index. Index: {contextIndex}. Line: {context.Start.Line}");
            }

            UnitSeatMapping mapping;
            if(!_seatMappings.TryGetValue(contextIndex, out mapping))
            {
                throw new ArgumentException($"Failed to retrieve the unit seat mapping information. Please ensure that the xml file contains the mapping. Index: {contextIndex}. Line: {context.Start.Line}");
            }

            var opCode = _opcodes.GetTypeInfo("unit_seat_mapping").Opcode;
            Expression16 exp = new Expression16(_currentSalt, opCode, opCode, _strings.Cache(mapping.Name), (short)context.Start.Line);
            exp.Values[0] = mapping.Count;
            exp.Values[1] = mapping.Index;

            IncrementDatum();
            OpenDatumAndAdd(exp);

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
                    || IsAiLine(context)
                    || IsPointReference(context)
                    || IsFolder(context)
                    || IsDeviceGroup(context)
                    || IsTagref(context, "any_tag")
                    || IsString(context);
                if(result == false)
                {
                    CreateSID(context);
                }
                return true;
            }
            #endregion
        }
    }
}
