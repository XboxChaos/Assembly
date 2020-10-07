using Blamite.Blam;
using Blamite.Blam.Scripting;
using Blamite.Blam.Util;
using Blamite.Serialization;
using System.CodeDom.Compiler;
using System.Text;

namespace ScriptWalker
{
    public class ScriptWalker
    {
        private ScriptTable _orig;
        private ScriptTable _mod;
        private IndentedTextWriter _output;
        private OpcodeLookup _op;
        private object _currentScriptObject;
        private bool _corrupt = false;

        public ScriptWalker(ScriptTable orig, ScriptTable mod, IndentedTextWriter output, EngineDescription desc)
        {
            _orig = orig;
            _mod = mod;
            _output = output;
            _op = desc.ScriptInfo;
        }

        public void Analyze()
        {
            _output.Flush();

            _output.Indent = 0;
            _output.WriteLine("### Globals ###");
            _output.WriteLine();
            _output.Indent++;

            AnalyzeGlobals();

            _output.Indent = 0;
            _output.WriteLine("### Scripts ###");
            _output.WriteLine();
            _output.Indent++;

            AnalyzeScripts();
        }

        public void AnalyzeGlobals()
        {         
            if (_orig.Globals.Count != _mod.Globals.Count)
            {
                _output.WriteLine($"GLOBALS COUNT MISMATCH! Original: {_orig.Globals.Count} Modified: {_mod.Globals.Count}");
                return;
            }

            for (int i = 0; i < _orig.Globals.Count; i++)
            {
                ScriptGlobal orig_Glo = _orig.Globals[i];
                ScriptGlobal mod_Glo = _mod.Globals[i];
                _currentScriptObject = mod_Glo;

                if (!CompareGlobals(orig_Glo, mod_Glo))
                {
                    WriteScriptObject();
                    _output.WriteLine($"Index: {i} - Information Mismatch");
                    continue;
                }

                var orig_Exp = _orig.Expressions.FindExpression(orig_Glo.ExpressionIndex);
                var mod_Exp = _mod.Expressions.FindExpression(mod_Glo.ExpressionIndex);

                _output.Indent++;
                WalkExpressions(orig_Exp, mod_Exp);
                _output.Indent--;

                _corrupt = false;
            }
        }
        
        public void AnalyzeScripts()
        {
            if (_orig.Scripts.Count != _mod.Scripts.Count)
            {
                _output.WriteLine($"Script COUNT MISMATCH! Original: {_orig.Scripts.Count} Modified: {_mod.Scripts.Count}");
                return;
            }

            for (int i = 0; i < _orig.Scripts.Count; i++)
            {
                Script origScript = _orig.Scripts[i];
                Script modScript = _mod.Scripts[i];
                _currentScriptObject = modScript;

                if (!CompareScripts(origScript, modScript))
                {
                    WriteScriptObject();
                    _output.WriteLine($"Index: {i} - Information Mismatch");
                    continue;
                }

                var orig_Exp = _orig.Expressions.FindExpression(origScript.RootExpressionIndex);
                var mod_Exp = _mod.Expressions.FindExpression(modScript.RootExpressionIndex);

                _output.Indent++;
                WalkExpressions(orig_Exp, mod_Exp);
                _output.Indent--;

                _corrupt = false;
            }
        }

        private bool CompareGlobals(ScriptGlobal orig, ScriptGlobal mod)
        {
            bool index = orig.ExpressionIndex.IsValid && mod.ExpressionIndex.IsValid;
            return index && (orig.Name == mod.Name) && (orig.Type == mod.Type);
        }

        private bool CompareScripts(Script orig, Script mod)
        {
            bool index = orig.RootExpressionIndex.IsValid && mod.RootExpressionIndex.IsValid;

            int origHash = SequenceHash.GetSequenceHashCode(orig.Parameters);
            int modHash = SequenceHash.GetSequenceHashCode(mod.Parameters);

            return index && (orig.Name == mod.Name) && (orig.ExecutionType == mod.ExecutionType) 
                && (orig.ReturnType == mod.ReturnType) && (origHash == modHash);
        }

        private void CompareNormalExpressions(ScriptExpression origExp, ScriptExpression modExp)
        {
            bool areEqual = true;

            // the opcodes always have to match.
            areEqual = areEqual && (origExp.Opcode == modExp.Opcode);
            areEqual = areEqual && (origExp.ReturnType == modExp.ReturnType);

            // An expression's opcode determines its actual value type. The value type is used for casting. Function names are an exception.
            string valueType = _op.GetTypeInfo(origExp.ReturnType).Name == "function_name" ? "function_name" : _op.GetTypeInfo(origExp.Opcode).Name;

            switch (valueType)
            {
                case "void":
                case "boolean":
                case "real":
                case "short":
                case "long":
                    // ignore random strings.
                    areEqual = areEqual && (origExp.Value == modExp.Value);
                    break;

                case "string":
                case "string_id":
                case "function_name":
                    areEqual = areEqual && (origExp.StringValue == modExp.StringValue);
                    break;

                case "sound":
                case "effect":
                case "damage":
                case "looping_sound":
                case "animation_graph":
                case "damage_effect":
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
                case "ai_line":
                case "unit_seat_mapping":
                    areEqual = areEqual && (origExp.Value == modExp.Value);
                    // ignore missing tags, ai lines and unit seat mappings
                    if(origExp.Value != 0xFFFFFFFF)
                    {
                        areEqual = areEqual && (origExp.StringValue == modExp.StringValue);
                    }
                    break;


                default:
                    areEqual = areEqual && (origExp.StringValue == modExp.StringValue);
                    areEqual = areEqual && (origExp.Value == modExp.Value);
                    break;
            }

            if(!areEqual)
            {
                WriteScriptObject();

                _output.WriteLine("Unequal Expressions!");
                _output.WriteLine("### Original ###");
                _output.WriteLine(ExpressionToString(origExp));
                _output.WriteLine("### Modified ###");
                _output.WriteLine(ExpressionToString(modExp));
                _output.WriteLine();
            }

        }

        private void CompareGlobalsReferences(ScriptExpression origExp, ScriptExpression modExp)
        {
            bool areEqual = true;
            areEqual = areEqual && (origExp.Opcode == modExp.Opcode);
            areEqual = areEqual && (origExp.ReturnType == modExp.ReturnType);
            areEqual = areEqual && (origExp.StringValue == modExp.StringValue);
            areEqual = areEqual && (origExp.Value == modExp.Value);

            if (!areEqual)
            {
                WriteScriptObject();

                _output.WriteLine("Unequal Globals References!");
                _output.WriteLine("### Original ###");
                _output.WriteLine(ExpressionToString(origExp));
                _output.WriteLine("### Modified ###");
                _output.WriteLine(ExpressionToString(modExp));
                _output.WriteLine();
            }
        }

        private void CompareScriptReferences(ScriptExpression origExp, ScriptExpression modExp)
        {
            bool areEqual = true;
            areEqual = areEqual && (origExp.Opcode == modExp.Opcode);
            areEqual = areEqual && (origExp.ReturnType == modExp.ReturnType);

            if (!areEqual)
            {
                WriteScriptObject();

                _output.WriteLine("Unequal Script References!");
                _output.WriteLine("### Original ###");
                _output.WriteLine(ExpressionToString(origExp));
                _output.WriteLine("### Modified ###");
                _output.WriteLine(ExpressionToString(modExp));
                _output.WriteLine();
            }

            DatumIndex origVal = new DatumIndex(origExp.Value);
            DatumIndex modVal = new DatumIndex(modExp.Value);
            var origValExp = _orig.Expressions.FindExpression(origVal);
            var modValExp = _mod.Expressions.FindExpression(modVal);

            if (origValExp == null || modValExp == null)
            {
                WriteScriptObject();

                _output.WriteLine("[CRITICAL] Failed to follow script reference expression. "
                    + $"Original Index: \"{origExp.Index.Index.ToString("X4")}\" "
                    + $"Modified Index: \"{origExp.Index.Index.ToString("X4")}\"");
                return;
            }

            WalkExpressions(origValExp, modValExp);
        }

        private void CompareParameterReferences(ScriptExpression origExp, ScriptExpression modExp)
        {
            bool areEqual = true;
            areEqual = areEqual && (origExp.Opcode == modExp.Opcode);
            areEqual = areEqual && (origExp.ReturnType == modExp.ReturnType);
            areEqual = areEqual && (origExp.StringValue == modExp.StringValue);
            areEqual = areEqual && (origExp.Value == modExp.Value);

            if (!areEqual)
            {
                WriteScriptObject();

                _output.WriteLine("Unequal Parameter References!");
                _output.WriteLine("### Original ###");
                _output.WriteLine(ExpressionToString(origExp));
                _output.WriteLine("### Modified ###");
                _output.WriteLine(ExpressionToString(modExp));
                _output.WriteLine();
            }
        }

        private void CompareGroups(ScriptExpression origExp, ScriptExpression modExp)
        {
            bool areEqual = true;
            areEqual = areEqual && (origExp.Opcode == modExp.Opcode);
            areEqual = areEqual && (origExp.ReturnType == modExp.ReturnType);

            if (!areEqual)
            {
                WriteScriptObject();

                _output.WriteLine("Unequal Groups!");
                _output.WriteLine("### Original ###");
                _output.WriteLine(ExpressionToString(origExp));
                _output.WriteLine("### Modified ###");
                _output.WriteLine(ExpressionToString(modExp));
                _output.WriteLine();
            }

            DatumIndex origVal = new DatumIndex(origExp.Value);
            DatumIndex modVal = new DatumIndex(modExp.Value);
            var origValExp = _orig.Expressions.FindExpression(origVal);
            var modValExp = _mod.Expressions.FindExpression(modVal);

            if(origValExp == null || modValExp == null)
            {
                WriteScriptObject();

                _output.WriteLine("[CRITICAL] Failed to follow group expression. "
                    + $"Original Index: \"{origExp.Index.Index.ToString("X4")}\" "
                    + $"Modified Index: \"{origExp.Index.Index.ToString("X4")}\"");
                _output.WriteLine();
                return;
            }

            WalkExpressions(origValExp, modValExp);
        }

        private void WalkExpressions(ScriptExpression origExp, ScriptExpression modExp)
        {
            ScriptExpression origNext = origExp;
            ScriptExpression modNext = modExp;

            while(origNext != null && modNext != null)
            {
                if (origNext.Type != modNext.Type)
                {
                    _output.WriteLine($"[CRITICAL] Unequal Expression Types. \"{origNext.Type}\" / \"{modNext.Type}\" "
                        + $"Original Index: \"{origNext.Index.Index.ToString("X4")}\" "
                        + $"Modified Index: \"{modNext.Index.Index.ToString("X4")}\"");
                    _output.WriteLine();
                    return;
                }

                switch (origNext.Type)
                {
                    case ScriptExpressionType.Expression:
                        CompareNormalExpressions(origNext, modNext);
                        break;
                    case ScriptExpressionType.GlobalsReference:
                        CompareGlobalsReferences(origNext, modNext);
                        break;
                    case ScriptExpressionType.ScriptReference:
                        CompareScriptReferences(origNext, modNext);
                        break;
                    case ScriptExpressionType.ParameterReference:
                        CompareParameterReferences(origNext, modNext);
                        break;
                    case ScriptExpressionType.Group:
                        CompareGroups(origNext, modNext);
                        break;
                    default:
                        _output.WriteLine($"[Critical] Unrecognized Expression Type {origNext.Type}");
                        break;
                }

                if((origNext.NextExpression == null && modNext.NextExpression != null) 
                    || (origNext.NextExpression != null && modNext.NextExpression == null))
                {
                    _output.WriteLine("[CRITICAL] Unequal Next Datums. "
                        + $"Original Index: \"{origNext.Index.Index.ToString("X4")}\" "
                        + $"Modified Index: \"{modNext.Index.Index.ToString("X4")}\"");
                    _output.WriteLine();
                    return;
                }

                origNext = origNext.NextExpression;
                modNext = modNext.NextExpression;
            }
        }

        private string ExpressionToString(ScriptExpression exp)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Index: \"{exp.Index.Index.ToString("X4")}\"");
            sb.Append($" Salt: \"{exp.Index.Salt.ToString("X4")}\"");
            sb.Append($" OP: \"{exp.Opcode.ToString("X4")}\"");
            sb.Append($" ReturnType: \"{exp.ReturnType.ToString("X4")}\"");
            sb.Append($" ExpType: \"{exp.Type.ToString()}\"");
            sb.Append($" NextSalt: \"{exp.Next.Salt.ToString("X4")}\"");
            sb.Append($" NextIndex: \"{exp.Next.Index.ToString("X4")}\"");
            sb.Append($" Value: \"{exp.Value.ToString("X8")}\"");
            sb.Append($" Line: \"{exp.LineNumber.ToString()}\"");

            if (exp.Type == ScriptExpressionType.Group)
            {
                sb.Append($" Name: \"{_op.GetFunctionInfo(exp.Opcode).Name}\"");
            }
            else if(exp.Type == ScriptExpressionType.Expression)
            {
                string retType = _op.GetTypeInfo(exp.ReturnType).Name;
                if(retType != "unparsed" &&
                    retType != "passthrough" &&
                    retType != "void" &&
                    retType != "boolean" &&
                    retType != "real" &&
                    retType != "short" &&
                    retType != "long")
                {
                    sb.Append($" String: \"{exp.StringValue}\"");
                }
            }

            return sb.ToString();
        }

        private void WriteScriptObject()
        {
            if(!_corrupt)
            {
                int currentIndent = _output.Indent;
                _output.Indent = 1;

                if (_currentScriptObject is Script scr)
                {
                    _output.WriteLine(scr.Name);
                }
                else if (_currentScriptObject is ScriptGlobal glo)
                {
                    _output.WriteLine(glo.Name);
                }
                else
                {
                    _output.WriteLine($"[CRITICAL] Unsupported object: {_currentScriptObject.GetType()}");
                }

                _output.WriteLine();
                _output.Indent = currentIndent;
                _corrupt = true;
            }
        }

    }
}
