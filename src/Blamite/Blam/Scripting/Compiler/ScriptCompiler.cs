﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime.Misc;
using System.Xml;
using Blamite.Blam.Scripting.Compiler.Expressions;
using Blamite.IO;
using System.IO;
using System.Diagnostics;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {
        // lookups
        private ICacheFile _cashefile;
        private OpcodeLookup _opcodes;
        private ScriptContext _scriptContext;
        private Dictionary<int, UnitSeatMapping> _seatMappings;
        private List<ScriptInfo> _scriptLookup = new List<ScriptInfo>();
        private List<GlobalInfo> _globalLookup = new List<GlobalInfo>();
        private List<ParameterInfo> _variables = new List<ParameterInfo>();

        // script tables
        private StringTable _strings = new StringTable();
        private List<ExpressionBase> _expressions = new List<ExpressionBase>();
        private List<Script> _scripts = new List<Script>();
        private List<ScriptGlobal> _globals = new List<ScriptGlobal>();
        private List<ITag> _references = new List<ITag>();

        // utility
        private const UInt32 _randomAddress = 0xCDCDCDCD;  // used for expressions where the string address doesn't point to the string table
        private ushort _currentSalt = 0xE373;
        private ushort _currentExpressionIndex = 0;
        private Stack<Int32> _openDatums = new Stack<Int32>();
        private Stack<string> _expectedTypes = new Stack<string>();


        public ScriptCompiler(ICacheFile casheFile, ScriptContext context, OpcodeLookup opCodes, Dictionary<int, UnitSeatMapping> seatMappings)
        {
            _cashefile = casheFile;
            _scriptContext = context;
            _opcodes = opCodes;
            _seatMappings = seatMappings;
        }

        /// <summary>
        /// Generate script and global lookups.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterHsc(BS_ReachParser.HscContext context)
        {
            if (context.gloDecl() != null)
                _globalLookup = context.gloDecl().Select(g => new GlobalInfo(g)).ToList();

            if (context.scriDecl() != null)
                _scriptLookup = context.scriDecl().Select(s => new ScriptInfo(s)).ToList();

        }

        /// <summary>
        /// Output Debug Info
        /// </summary>
        /// <param name="context"></param>
        public override void ExitHsc(BS_ReachParser.HscContext context)
        {
            DeclarationsToXML();
            ExpressionsToXML();
            StringsToFile();
        }

        /// <summary>
        /// Processes script declarations. Opens a datum. 
        /// Creates the script node and the initial "begin" expression.
        /// Generates the variable lookup. Pushes return types.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterScriDecl(BS_ReachParser.ScriDeclContext context)
        {
            Debug.Print($"Enter Script: {context.ID().GetText()}");
            // create new script object and add it to the table
            Script scr = ScriptFromContext(context);
            _scripts.Add(scr);

            var retType = context.retType().GetText();

            // if the function has a return type, the first call or global reference must match it.
            if (retType == "void")
            {
                var exp_count = context.gloRef().Count() + context.call().Count();
                for (int i = 0; i < exp_count; i++)
                    PushTypes("ANY");
            }
            else
            {
                PushTypes(retType);
            }

            // Create FunctionCall entry
            ScriptFunctionInfo info = _opcodes.GetFunctionInfo("begin").First();
            FunctionCall funcCall = new FunctionCall();
            funcCall.Salt = _currentSalt;
            funcCall.OpCode = info.Opcode;
            funcCall.ValueType = _opcodes.GetTypeInfo(retType).Opcode;
            funcCall.NextExpression = DatumIndex.Null; // closed
            funcCall.LineNumber = 0;
            IncrementDatum();
            funcCall.Value = new DatumIndex(_currentSalt, _currentExpressionIndex); // points to name
            _expressions.Add(funcCall);

            // Create function_name entry
            AddFunctionName("begin", funcCall.OpCode, 0);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitScriDecl(BS_ReachParser.ScriDeclContext context)
        {
            Debug.Print($"Exit Script: {context.ID().GetText()}");
            _variables.Clear();
            CloseDatum();
        }

        /// <summary>
        /// Processes global declarations. Opens a datum. Creates the global node. Pushes the value type.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterGloDecl( BS_ReachParser.GloDeclContext context)
        {
            Debug.Print($"Enter Global: {context.ID().GetText()}");

            //create a new Global and add it to the table
            ScriptGlobal glo = new ScriptGlobal();
            glo.Name = context.ID().GetText();
            glo.Type = (short)_opcodes.GetTypeInfo(context.VALUETYPE().GetText()).Opcode;
            glo.ExpressionIndex = new DatumIndex(_currentSalt, _currentExpressionIndex);
            _globals.Add(glo);

            Debug.Print("Global Open");
            PushTypes(context.VALUETYPE().GetText());
            _openDatums.Push(-1);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitGloDecl(BS_ReachParser.GloDeclContext context)
        {
            Debug.Print($"Exit Global: {context.ID().GetText()}");
            CloseDatum();
        }

        /// <summary>
        /// Processes function calls and script references. Links to a datum. Opens one or more datums. 
        /// Pops a value type. Pushes parameter types.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterCall(BS_ReachParser.CallContext context)
        {
            Debug.Print($"Enter Call: {context.funcID().GetText()}");
            LinkDatum();

            // retrieve information from the context.
            string name = context.funcID().GetText();
            string expectedType = _expectedTypes.Pop();
            Int32 contextParamCount = context.expr().Count();

            // handle script references
            if (IsScriptReference(context, expectedType, contextParamCount))
                return;

            // handle calls
            ScriptFunctionInfo info = RetrieveFunctionInfo(name, contextParamCount);
            PushCallParameters(info, context, contextParamCount);

            // Throw an exception if the value types don't match.
            if (expectedType != "ANY" && expectedType != "NUMBER" && info.ReturnType != expectedType)
            {
                if (!Casting.CanBeCasted(info.ReturnType, expectedType))
                {
                    throw new Exception($"Mismatched value types while processing function \"{name}\". Expected: \"{expectedType}\" Encountered: \"{info.ReturnType}\".");
                }
            }

            // Create Function Call Expression
            FunctionCall funcCall = new FunctionCall();
            funcCall.Salt = _currentSalt;
            funcCall.OpCode = info.Opcode;
            funcCall.LineNumber = (short)context.Start.Line;

            // return type
            if (expectedType != "ANY" && expectedType != "NUMBER" && info.Group == "Arithmetic")
            {
                funcCall.ValueType = _opcodes.GetTypeInfo(expectedType).Opcode;
            }
            else if (info.ReturnType == "passthrough")
            {
                funcCall.ValueType = _opcodes.GetTypeInfo("void").Opcode;
            }
            else
            {
                funcCall.ValueType = _opcodes.GetTypeInfo(info.ReturnType).Opcode;
            }
            
            IncrementDatum();
            funcCall.Value = new DatumIndex(_currentSalt, _currentExpressionIndex);

            // open next expression Datum
            OpenDatumAndAdd(funcCall);
            AddFunctionName(name, funcCall.OpCode, funcCall.LineNumber);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitCall(BS_ReachParser.CallContext context)
        {
            Debug.Print($"Exit Call: {context.funcID().GetText()}");
            CloseDatum();
        }

        /// <summary>
        /// Processes regular expressions, script variables and global references. Links to a datum. Opens a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterLit(BS_ReachParser.LitContext context)
        {
            Debug.Print($"Enter Lit: {context.GetText()}");
            LinkDatum();

            string txt = context.GetText();
            string valType = _expectedTypes.Pop();

            // handle "none" expressions
            if(txt == "none")
            {                
                UInt16 opc = _opcodes.GetTypeInfo(valType).Opcode;
                Expression32 exp = new Expression32(_currentSalt, opc, opc, _strings.Cache(txt), (short)context.Start.Line);
                IncrementDatum();
                OpenDatumAndAdd(exp);
                return;
            }

            // handle script variable references
            if (IsScriptVariable(context))
                return;

            // handle global references
            if (IsGlobalReference(context, valType))
                return;
            
            // handle regular expressions
            if (HandleValueType(context, valType))
                return;

            throw new Exception($"Failed to process literal. Name: \"{txt}\"");
        }



        private bool IsGlobalReference(BS_ReachParser.LitContext context, string expReturnType)
        {
            //todo: implement casting?

            string text = context.GetText();
            int index = -1;
            if(expReturnType == "GLOBALREFERENCE" || expReturnType == "NUMBER" || expReturnType == "ANY")  // todo: handle NUMBER separately?
                index = _globalLookup.FindIndex(glo => glo.Name == text);
            else
                index = _globalLookup.FindIndex(glo => glo.Name == text && glo.ValueType == expReturnType);

            // not found...not a global reference
            if (index == -1)
            {
                if (expReturnType == "GLOBALREFERENCE")
                    throw new Exception($"GLOBALREFERENCE: No matching global could be found. Name: \"{text}\".");
                else
                    return false;
            }
                

            GlobalReference exp = new GlobalReference();
            exp.Salt = _currentSalt;
            exp.ValueType = _opcodes.GetTypeInfo(_globalLookup[index].ValueType).Opcode;

            // "set" and "=" functions are special
            if (context.Parent.Parent.RuleIndex == BS_ReachParser.RULE_call)
            {
                var grandparent = (BS_ReachParser.CallContext)context.Parent.Parent;
                string funcName = grandparent.funcID().GetText();
                if (funcName == "=")
                    exp.OpCode = 0;
                else if (funcName == "set")
                {
                    exp.OpCode = 0xFFFF;
                    Debug.Print("global push");
                    PushTypes(_globalLookup[index].ValueType); // the next parameter must have the same return type as this global
                }

            }
            // default case: opcode = value type
            else
                exp.OpCode = (ushort)exp.ValueType;

            exp.StringAddress = _strings.Cache(text);
            exp.Value = index;
            exp.LineNumber = (short)context.Start.Line;

            IncrementDatum();

            //open next expression Datum
            OpenDatumAndAdd(exp);

            return true;
        }  // ANY NUMBER

        private bool IsScriptReference(BS_ReachParser.CallContext context, string expextedReturnType, Int32 expectedParamCount)
        {
            string name = context.funcID().GetText();
            if (expextedReturnType == "ANY")
                expextedReturnType = "void";

            int index;

            // number handling
            if(expextedReturnType == "NUMBER")
            {
                index = _scriptLookup.FindIndex(s => s.Name == name && _numTypes.Contains(s.ReturnType) && s.Parameters.Count == expectedParamCount);
            }
            else
            {
                index = _scriptLookup.FindIndex(s => s.Name == name && s.ReturnType == expextedReturnType && s.Parameters.Count == expectedParamCount);
            }

            // a matching script wasn't found...not a script reference
            if (index == -1)
                return false;

            // handle parameters. Push them to the stack
            if(expectedParamCount > 0)
            {
                string[] types = _scriptLookup[index].Parameters.Select(p=> p.ValueType).ToArray();
                PushTypes(types);
            }

            // create Script Reference node
            ScriptReference scrRef = new ScriptReference();
            scrRef.Salt = _currentSalt;
            scrRef.OpCode = (ushort)index;
            scrRef.ValueType = _opcodes.GetTypeInfo(_scriptLookup[index].ReturnType).Opcode;
            scrRef.LineNumber = (short)context.Start.Line;

            IncrementDatum();
            scrRef.Value = new DatumIndex(_currentSalt, _currentExpressionIndex);     // close Value: Index to name                                                                                     
            OpenDatumAndAdd(scrRef);                                                  // open next expression Datum

            AddFunctionName(name, scrRef.OpCode, scrRef.LineNumber);
            return true;
        }  // ANY NUMBER

        private bool IsScriptVariable(BS_ReachParser.LitContext context)
        {
            // this script doesn't have parameters
            if (_variables.Count == 0)
                return false;

            string name = context.GetText();
            int index = _variables.FindIndex(v=>v.Name == name);

            // no match
            if (index == -1)
                return false;

            UInt16 valType = _opcodes.GetTypeInfo(_variables[index].ValueType).Opcode;
            UInt16 opcode = valType;
            //"=" functions are special
            if (context.Parent.RuleIndex == BS_ReachParser.RULE_call)
            {
                var parent = (BS_ReachParser.CallContext)context.Parent;
                string funcName = parent.funcID().GetText();
                if (funcName == "=")
                    opcode = 0;
            }

            // create script parameter reference
            ScriptVariableReference exp = new ScriptVariableReference(_currentSalt, opcode, valType, _strings.Cache(name), (short)context.Start.Line);
            exp.Value = index;

            IncrementDatum();
            //open next expression Datum
            OpenDatumAndAdd(exp);

            return true;
        } // ANY NUMBER?

        private Script ScriptFromContext(BS_ReachParser.ScriDeclContext context)
        {
            // Create new Script
            Script scr = new Script();
            scr.Name = context.ID().GetText();
            scr.ExecutionType = (short)_opcodes.GetScriptTypeOpcode(context.SCRIPTTYPE().GetText());
            scr.ReturnType = (short)_opcodes.GetTypeInfo(context.retType().GetText()).Opcode;
            scr.RootExpressionIndex = new DatumIndex(_currentSalt, _currentExpressionIndex);
            var paramContext = context.scriptParams();

            //process parameters
            if (paramContext != null)
            {
                // extract strings from the context
                var names = paramContext.ID().Select(n => n.GetText()).ToArray();
                var valTypes = paramContext.VALUETYPE().Select(v => v.GetText()).ToArray();

                if (names.Count() != valTypes.Count())
                    throw new Exception($"Error while parsing Script: \"{scr.Name}\" - Mismatched parameter arrays.");

                // create parameters from the extracted strings
                for (int i = 0; i < names.Count(); i++)
                {
                    var param = new ScriptParameter();
                    param.Name = names[i];
                    param.Type = (short)_opcodes.GetTypeInfo(valTypes[i]).Opcode;
                    scr.Parameters.Add(param);
                    // create variable lookup
                    var info = new ParameterInfo(names[i], valTypes[i]);
                    _variables.Add(info);
                }
            }

            return scr;
        }

        private void AddFunctionName(string name, ushort opCode, short lineNumber)
        {
            // create function_name node
            Expression32 funcName = new Expression32(_currentSalt, opCode, _opcodes.GetTypeInfo("function_name").Opcode, _strings.Cache(name), lineNumber);
            funcName.Value = 0;

            IncrementDatum();
            //open next expression Datum
            OpenDatumAndAdd(funcName);
        }

        /// <summary>
        /// Calculates the value types for the parameters of a function and pushes them to the stack.
        /// </summary>
        /// <param name="info">The function's ScriptFunctionInfo</param>
        /// <param name="context">The call contect</param>
        /// <param name="actualParameterCount">The number of parameters which was extracted from the context</param>
        private void PushCallParameters(ScriptFunctionInfo info, BS_ReachParser.CallContext context, int contextParameterCount)
        {
            // handle parameters
            int expectedParamCount = info.ParameterTypes.Count();
            if (expectedParamCount > 0)
            {
                if (contextParameterCount != expectedParamCount)
                    throw new Exception($"Mismatched parameter counts while processing function \"{context.funcID().GetText()}\". Expected: \"{expectedParamCount}\" Encountered: \"{contextParameterCount}\".");

                // push params to the stack
                PushTypes(info.ParameterTypes);
            }
            #region special functions
            else
            {
                switch (info.Group)
                {
                    case "Begin":
                        PushTypes("ANY", contextParameterCount);
                        break;

                    case "BeginCount":
                        PushTypes("ANY", contextParameterCount - 1);
                        PushTypes("NUMBER");
                        break;

                    case "If":
                        PushTypes("boolean", "ANY");
                        break;

                    case "Cond":
                        for (int i = 0; i < (contextParameterCount / 2); i++)
                        {
                            PushTypes("boolean", "ANY");
                        }
                        break;

                    case "Set":
                            PushTypes("GLOBALREFERENCE");
                            break;

                    case "Logical":
                        PushTypes("boolean", contextParameterCount);
                        break;

                    case "Arithmetic":
                        PushTypes("NUMBER", contextParameterCount);
                        break;

                    case "Equality":
                        PushTypes("ANY", "ANY");
                        break;

                    case "Inequality":
                        PushTypes("NUMBER", "NUMBER");
                        break;

                    case "Sleep":
                        if (contextParameterCount > 1)
                            PushTypes("script");
                        PushTypes("short");
                        break;

                    case "SleepForever":
                        if (contextParameterCount > 0)
                            PushTypes("script");
                        break;

                    case "SleepUntil":
                        if (contextParameterCount > 1)
                        {
                            PushTypes("short");
                        }
                        PushTypes("boolean");
                        break;

                    case "Wake":
                        PushTypes("script");
                        break;

                    case "Inspect":
                        PushTypes("ANY");
                        break;

                    case "Branch":
                        throw new Exception("Branching is not supported yet.");
                        //_expectedTypes.Push("boolean");
                        //_expectedTypes.Push("BRANCH"); // not sure
                        //break;

                    case "ObjectCast":
                        PushTypes(info.ReturnType);
                        break;
                        #endregion
                }
            }
        }
    }
}
