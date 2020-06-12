﻿using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using System.Diagnostics;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {
        // debug
        public bool PrintDebugInfo = true;
        private Logger _logger;

        // lookups
        private ICacheFile _cashefile;
        private OpcodeLookup _opcodes;
        private ScriptContext _scriptContext;
        private Dictionary<string, UnitSeatMapping> _seatMappings;
        private Dictionary<string, ScriptInfo> _scriptLookup = new Dictionary<string, ScriptInfo>();
        private Dictionary<string, GlobalInfo> _mapGlobalsLookup = new Dictionary<string, GlobalInfo>();
        private List<ParameterInfo> _variables = new List<ParameterInfo>();

        // script tables
        private StringTable _strings = new StringTable();
        private List<ScriptExpression> _expressions = new List<ScriptExpression>();
        private List<Script> _scripts = new List<Script>();
        private List<ScriptGlobal> _globals = new List<ScriptGlobal>();
        private List<ITag> _references = new List<ITag>();

        // utility
        private const uint _randomAddress = 0xCDCDCDCD;  // used for expressions where the string address doesn't point to the string table
        private DatumIndex _currentIndex;
        private Stack<int> _openDatums = new Stack<int>();
        private Stack<string> _expectedTypes = new Stack<string>();

        // branching
        private ushort _branchBoolIndex = 0;
        private Dictionary<string, ScriptExpression[]> _genBranches = new Dictionary<string, ScriptExpression[]>();

        // cond
        private string _condType;
        private Stack<int> _condIndeces = new Stack<int>();

        // equality
        private bool _equality = false;

        // set
        private bool _set = false;

        // progress
        private IProgress<int> _progress;
        private int _declarationCount = 0;
        private int _processedDeclarations = 0;

        // returned data
        private ScriptData _result = null;



        public ScriptCompiler(ICacheFile casheFile, ScriptContext context, OpcodeLookup opCodes, Dictionary<string, UnitSeatMapping> seatMappings, IProgress<int> progress, Logger logger)
        {
            _progress = progress;
            _cashefile = casheFile;
            _scriptContext = context;
            _opcodes = opCodes;
            _seatMappings = seatMappings;
            _logger = logger;

            UInt16 salt = SaltGenerator.GetSalt("script node");
            UInt16 index = 0;
            _currentIndex = new DatumIndex(salt, index);
        }

        public ScriptData Result()
        {
            return _result;
        }

        /// <summary>
        /// Generate script and global lookups.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterHsc(BS_ReachParser.HscContext context)
        {
            if (context.gloDecl() != null)
                _mapGlobalsLookup = context.gloDecl().Select((g, index) => new GlobalInfo(g, (ushort)index)).ToDictionary(g => g.Name);

            if (context.scriDecl() != null)
                _scriptLookup = context.scriDecl().Select((s, index) => new ScriptInfo(s, (ushort)index)).ToDictionary(s => s.Name + "_" + s.Parameters.Count);

            _declarationCount = context.scriDecl().Count() + context.gloDecl().Count();
        }

        /// <summary>
        /// Output Debug Info
        /// </summary>
        /// <param name="context"></param>
        public override void ExitHsc(BS_ReachParser.HscContext context)
        {
            GenerateBranches();
            DeclarationsToXML();
            ExpressionsToXML();
            StringsToFile();
            _result = new ScriptData(_scripts, _globals, _references, _expressions, _strings);
        }

        /// <summary>
        /// Processes script declarations. Opens a datum. 
        /// Creates the script node and the initial "begin" expression.
        /// Generates the variable lookup. Pushes return types.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterScriDecl(BS_ReachParser.ScriDeclContext context)
        {
            if(PrintDebugInfo)
               _logger.WriteLine("SCRIPT", $"Enter: {context.scriptID().GetText()} ,  Line: {context.Start.Line}");

            // create new script object and add it to the table
            Script scr = ScriptFromContext(context);
            _scripts.Add(scr);

            string retType = context.retType().GetText();
            int expCount = context.gloRef().Count() + context.call().Count() + context.branch().Count() + context.cond().Count();

            // The final expression must match the return type of this script.
            PushTypes(retType);
            // The other expressions can be of any type.
            if (expCount > 1)
            {
                PushTypes("void", expCount - 1);
            }
            //PushTypes(retType, expCount);

            CreateInitialBegin(_opcodes.GetTypeInfo(retType).Opcode);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitScriDecl(BS_ReachParser.ScriDeclContext context)  
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("SCRIPT", $"Exit: {context.scriptID().GetText()} , Line: {context.Start.Line}");
                _logger.WriteNewLine();
            }

            _variables.Clear();
            CloseDatum();
            ReportProgress();
        }

        /// <summary>
        /// Processes global declarations. Opens a datum. Creates the global node. Pushes the value type.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterGloDecl( BS_ReachParser.GloDeclContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("GLOBAL", $"Enter: {context.ID().GetText()} , Line: {context.Start.Line}");
            }

            //create a new Global and add it to the table
            ScriptGlobal glo = new ScriptGlobal();
            glo.Name = context.ID().GetText();
            string valType = context.VALUETYPE().GetText();
            glo.Type = (short)_opcodes.GetTypeInfo(valType).Opcode;
            glo.ExpressionIndex = _currentIndex;
            _globals.Add(glo);

            if (PrintDebugInfo)
            {
                _logger.WriteLine("GLOBAL", $"Open: -1");
                _logger.WriteLine("GLOBAL", $"Type Push: {valType}");
            }
            PushTypes(valType);
            _openDatums.Push(-1);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitGloDecl(BS_ReachParser.GloDeclContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("GLOBAL", $"Exit: {context.ID().GetText()}");
                _logger.WriteNewLine();
            }

            CloseDatum();
            ReportProgress();
        }

        /// <summary>
        /// Processes function calls and script references. Links to a datum. Opens one or more datums. 
        /// Pops a value type. Pushes parameter types.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterCall(BS_ReachParser.CallContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("CALL", $"Enter: {context.funcID().GetText()} , Line: {context.Start.Line}");
            }

            LinkDatum();

            // retrieve information from the context.
            string name = context.funcID().GetText();
            string expectedType = PopType();
            int contextParamCount = context.expr().Count();

            // handle script references.
            if (IsScriptReference(context, expectedType, contextParamCount))
            {
                return;
            }

            // handle calls.
            ScriptFunctionInfo info = RetrieveFunctionInfo(name, contextParamCount, context.Start.Line);

            // equality
            EqualityPush(info.ReturnType);
            PushCallParameters(info, context, contextParamCount, expectedType);

            ushort returnType = GetTypeOpCode(info, expectedType, context);

            CreateFunctionCall(returnType, info, (short)context.Start.Line);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitCall(BS_ReachParser.CallContext context)
        {

            if (PrintDebugInfo)
            {
                _logger.WriteLine("CALL", $"Exit: {context.funcID().GetText()} , Line: {context.Start.Line}");
            }

            CloseDatum();
        }

        public override void EnterBranch(BS_ReachParser.BranchContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("BRANCH", $"Enter , Line: {context.Start.Line}");
            }

            LinkDatum();
            _ = PopType();    // just ignore the type for now

            // branch excepts two parameters
            if (context.expr().Count() != 2)
            {
                throw new CompilerException("A branch call had an unexpected number of parameters.", context);
            }

            PushTypes("boolean", "SCRIPTREFERENCE");
            ScriptFunctionInfo info = _opcodes.GetFunctionInfo("branch")[0];
            CreateFunctionCall(_opcodes.GetTypeInfo("void").Opcode, info, (short)context.Start.Line);

            _branchBoolIndex = _currentIndex.Index;
        }

        public override void ExitBranch(BS_ReachParser.BranchContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("BRANCH", $"Exit , Line: {context.Start.Line}");
            }

            // generate the script name
            BS_ReachParser.ScriDeclContext scriptContext = GetParentScriptContext(context);
            string fromScript = scriptContext.scriptID().GetText();

            var parameters = context.expr();
            if (parameters[1].call() == null)
            {
                throw new CompilerException("A branch statements second parameter was not a script name.", context);
            }
            var param = parameters[1].call();
            string toScript = param.funcID().GetText();
            string genName = fromScript + "_to_" + toScript;

            ScriptExpression[] expressions = new ScriptExpression[2];
            // grab boolean expression
            var bol = _expressions[_branchBoolIndex].Clone();
            expressions[0] = bol;
            
            // grab script ref
            var sr = _expressions[bol.Next.Index].Clone();
            expressions[1] = sr;
            // modify the original script ref. the opcode points to the generated script
            _expressions[bol.Next.Index].Opcode = (ushort)_scriptLookup.Count;
            // add the generated script to the lookup
            ScriptInfo decl = new ScriptInfo(genName, "static", "void", (ushort)_scriptLookup.Count);
            string infoKey = decl.Name + "_" + 0;
            _scriptLookup[infoKey] = decl;

            _genBranches.Add(genName, expressions);
            CloseDatum();
        }

        public override void EnterCond(BS_ReachParser.CondContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("COND", $"Enter, Line: {GetLineNumber(context)}");
            }

            // tell the groups what type to expect.
            _condType = PopType();

            if (_condType == "ANY")
                _condType = "void";

            // push the index to the first group, so that we can open it later on.
            _condIndeces.Push(_expressions.Count);
        }

        public override void ExitCond(BS_ReachParser.CondContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("COND", $"Exit, Line: {GetLineNumber(context)}");
            }

            // Link to the compiler begin of the last open cond group.
            LinkDatum();

            // Add the final expression of the cond construct. Not sure why the official Blam Script compiler adds these.
            ushort typeOp = _opcodes.GetTypeInfo(_condType).Opcode;
            ScriptExpression exp = new ScriptExpression(_currentIndex, typeOp, typeOp, ScriptExpressionType.Expression, DatumIndex.Null ,
               _randomAddress, (uint)0, 0);

            _currentIndex.Increment();
            AddExpression(exp);

            // open the first group.
            int firstGroupIndex = _condIndeces.Pop();
            OpenDatum(firstGroupIndex);
        }

        public override void EnterCondGroup(BS_ReachParser.CondGroupContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("COND GROUP", $"Enter, Line: {GetLineNumber(context)}");
            }

            // Link to the previous expression or cond group.
            LinkDatum();

            ushort expectedOp = _opcodes.GetTypeInfo(_condType).Opcode;
            ushort funcNameOp = _opcodes.GetTypeInfo("function_name").Opcode;
            var ifInfo = _opcodes.GetFunctionInfo("if")[0];

            // push the types of the group members.
            PushTypes(_condType, context.expr().Count() - 1);
            PushTypes("boolean");

            ScriptExpression compIf = new ScriptExpression(_currentIndex, ifInfo.Opcode, expectedOp, 
                ScriptExpressionType.Group, _randomAddress, _currentIndex.Next(), 0);

            // Keep the if call closed. We will open the initial one later on.
            _currentIndex.Increment();
            AddExpression(compIf);

            ScriptExpression compIfName = new ScriptExpression(_currentIndex, ifInfo.Opcode, funcNameOp, ScriptExpressionType.Expression,
                _strings.Cache(ifInfo.Name), (uint)0, GetLineNumber(context));

            _currentIndex.Increment();
            OpenDatumAndAdd(compIfName);

            // Push the index to the condition so that we can modify it later on.
            _condIndeces.Push(_expressions.Count);
        }

        public override void ExitCondGroup(BS_ReachParser.CondGroupContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("COND GROUP", $"Exit, Line: {GetLineNumber(context)}");
            }

            // close the final value expression.
            CloseDatum();

            int index = _condIndeces.Pop();
            ushort funcNameOp = _opcodes.GetTypeInfo("function_name").Opcode;
            var beginInfo = _opcodes.GetFunctionInfo("begin")[0];

            // Grab the index to value expression of the cond group. Modify the condition expression afterwards.
            // Next has to point to the begin group, which is added by the compiler.
            var valueExpDatum = _expressions[index].Next;
            _expressions[index].Next = _currentIndex;

            ScriptExpression compilerBegin = new ScriptExpression(_currentIndex, beginInfo.Opcode, _opcodes.GetTypeInfo(_condType).Opcode,
                ScriptExpressionType.Group, _randomAddress, _currentIndex.Next(), 0);

            _currentIndex.Increment();
            OpenDatumAndAdd(compilerBegin);

            ScriptExpression compilerBeginName = new ScriptExpression(_currentIndex, beginInfo.Opcode, funcNameOp, ScriptExpressionType.Expression, valueExpDatum, 
                _strings.Cache(beginInfo.Name), (uint)0, 0);

            _currentIndex.Increment();
            AddExpression(compilerBeginName);
        }

        public override void EnterGloRef(BS_ReachParser.GloRefContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("GloRef", $"Enter: {context.GetText()} , Line: {context.Start.Line}");
            }

            LinkDatum();

            string retType = PopType();

            if (!IsGlobalReference(context, retType))
            {
                throw new CompilerException("The parser detected a Globals Reference, but the expression doesn't seem to be one.", context);
            }
        }

        /// <summary>
        /// Processes regular expressions, script variables and global references. Links to a datum. Opens a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterLit(BS_ReachParser.LitContext context)
        {
            if (PrintDebugInfo)
            {
                _logger.WriteLine("LITERAL", $"Enter: {context.GetText()} , Line: {context.Start.Line}");
            }

            LinkDatum();

            string txt = context.GetText();
            string valType = PopType();

            // handle "none" expressions
            if(txt == "none")
            {                
                ushort opc = _opcodes.GetTypeInfo(valType).Opcode;
                uint value = 0xFFFFFFFF;
                var exp = new ScriptExpression(_currentIndex, opc, opc, ScriptExpressionType.Expression,
                    _strings.Cache(txt), value, (short)context.Start.Line);

                _currentIndex.Increment();
                OpenDatumAndAdd(exp);
                return;
            }

            // handle script variable references
            if (IsScriptVariable(context, valType))
                return;

            // handle global references
            if (IsGlobalReference(context, valType))
                return;
            
            // handle regular expressions
            if (ProcessLiteral(context, valType, valType))
                return;

            throw new CompilerException($"Failed to process \"{txt}\".", context);
        }

        private bool IsGlobalReference(ParserRuleContext context, string expReturnType)
        {
            string text = context.GetText();
            GlobalInfo globalInfo = _opcodes.GetGlobalInfo(text);
            object value;

            // engine global.
            if(globalInfo != null)
            {
                ushort[] arr = { 0xFFFF, globalInfo.MaskedOpcode };
                value = arr;

            }
            // map global.
            else if(_mapGlobalsLookup.ContainsKey(text))
            {
                globalInfo = _mapGlobalsLookup[text];
                value = (uint)globalInfo.Opcode;
            }
            // not a global...
            else if (expReturnType == "GLOBALREFERENCE")
            {
                throw new CompilerException($"GLOBALREFERENCE: No matching global could be found.", context);
            }
            else
            {
                return false;
            }

            ushort typeOp = GetTypeOpCode(globalInfo, expReturnType, context);
            ushort opc = GetGlobalOpCode(typeOp, context);

            ScriptExpression exp = new ScriptExpression(_currentIndex, opc, typeOp, ScriptExpressionType.GlobalsReference,
                _strings.Cache(text), value, (short)context.Start.Line);

            _currentIndex.Increment();
            OpenDatumAndAdd(exp);

            return true;
        }

        private ushort GetGlobalOpCode(ushort retTypeOpCode, RuleContext context)
        {
            var grandparent = GetParentContext(context, BS_ReachParser.RULE_call);
            ushort opcode = retTypeOpCode;

            // "set" and (In)Equality functions are special
            if (grandparent is BS_ReachParser.CallContext call)
            {
                string funcName = call.funcID().GetText();
                List<ScriptFunctionInfo> funcInfo = _opcodes.GetFunctionInfo(funcName);

                if (funcInfo != null)
                {
                    if ((funcInfo[0].Group == "Equality" || funcInfo[0].Group == "Inequality") && _equality)
                    {
                        opcode = 0;
                    }

                    else if (_set && funcInfo[0].Group == "Set")
                    {
                        opcode = 0xFFFF;

                        if (PrintDebugInfo)
                        {
                            _logger.WriteLine("SET", $"Set Push!");
                        }
                        // the next parameter must have the same return type as this global
                        PushTypes(_opcodes.GetTypeInfo(retTypeOpCode).Name);
                        _set = false;
                    }
                }
            }

            // equality
            EqualityPush(_opcodes.GetTypeInfo(retTypeOpCode).Name);

            return opcode;
        }

        private ushort GetTypeOpCode(IScriptingConstantInfo info, string expectedType, ParserRuleContext context)
        {
            string calculatedType = expectedType switch
            {
                "ANY" when info.ReturnType == "passthrough" => "void",
                "ANY" => info.ReturnType,
                // cast globals in arithmetic functions to real.
                "NUMBER" when info is GlobalInfo && Casting.IsNumType(info.ReturnType) => "real",
                "NUMBER" when Casting.IsNumType(info.ReturnType) => info.ReturnType,
                "NUMBER" when !Casting.IsNumType(info.ReturnType) => "",
                "void" => "void",
                "GLOBALREFERENCE" when info is GlobalInfo => info.ReturnType,
                "SCRIPTREFERENCE" when info is ScriptInfo => info.ReturnType,
                _ when expectedType == info.ReturnType => expectedType,
                _ when info.ReturnType == "passthrough" => expectedType,
                _ when Casting.CanBeCasted(info.ReturnType, expectedType, expectedType, _opcodes) => expectedType,
                _ => ""
            };

            if (calculatedType == "")
            {
                throw new CompilerException($"The compiler failed to calculate the return type of an expression. It expected the return type \"{expectedType}\" while processing \"{info.Name}\"." +
                    $" It encountered the return type \"{info.ReturnType}\".", context);
            }

            return _opcodes.GetTypeInfo(calculatedType).Opcode;
        }

        private bool IsScriptReference(BS_ReachParser.CallContext context, string expectedReturnType, int expectedParamCount)
        {
            string key = context.funcID().GetText() + "_" + expectedParamCount;

            ScriptInfo info;

            if(!_scriptLookup.TryGetValue(key, out info))
            {
                if (expectedReturnType == "SCRIPTREFERENCE")
                    throw new CompilerException("The compiler expected a Script Reference but was unable to find a matching one. " +
                        "Please check your script declarations and your spelling.", context);
                else
                    return false;
            }

            ushort retType = GetTypeOpCode(info, expectedReturnType, context);

            // check for equality functions
            EqualityPush(_opcodes.GetTypeInfo(retType).Name);

            // handle parameters. Push them to the stack
            if (expectedParamCount > 0)
            {
                string[] types = info.Parameters.Select(p=> p.ValueType).ToArray();
                PushTypes(types);
            }

            // create Script Reference node
            ushort valType = _opcodes.GetTypeInfo(retType).Opcode;
            CreateScriptReference(info.Name, info.Opcode, valType, (short)context.Start.Line);

            return true;
        }

        private bool IsScriptVariable(BS_ReachParser.LitContext context, string expectedReturnType)
        {
            // this script doesn't have parameters
            if (_variables.Count == 0)
                return false;

            string name = context.GetText();
            int index = _variables.FindIndex(v=>v.Name == name);

            // no match
            if (index == -1)
                return false;

            string valType;

            // casting is not required
            if(Casting.IsFlexType(expectedReturnType) || expectedReturnType == _variables[index].ValueType)
            {
                valType = _variables[index].ValueType;
            }
            // casting
            else
            {
                if(Casting.CanBeCasted(_variables[index].ValueType, expectedReturnType, expectedReturnType, _opcodes))
                {
                    valType = expectedReturnType;
                }
                else
                {
                    throw new CompilerException($"The variable  \"{name}\" can't be casted from \"{_variables[index].ValueType}\" to \"{expectedReturnType}\".", context);
                }
            }

            ushort valop = _opcodes.GetTypeInfo(valType).Opcode;
            ushort opcode = valop;

            // (In)Equality functions are special
            var grandparent = context.Parent.Parent as BS_ReachParser.CallContext;
            if (grandparent != null)
            {
                string funcName = grandparent.funcID().GetText();
                var funcInfo = _opcodes.GetFunctionInfo(funcName);
                if(funcInfo != null)
                {
                    if ((funcInfo[0].Group == "Equality" || funcInfo[0].Group == "Inequality") && _equality)
                    {
                        opcode = 0;
                    }
                }
            }
            EqualityPush(_variables[index].ValueType);

            // create script parameter reference
            var exp = new ScriptExpression(_currentIndex, opcode, valop, ScriptExpressionType.ParameterReference,
                _strings.Cache(name), (uint)index, (short)context.Start.Line);

            _currentIndex.Increment();
            //open next expression Datum
            OpenDatumAndAdd(exp);

            return true;
        }

        private Script ScriptFromContext(BS_ReachParser.ScriDeclContext context)
        {
            // Create new Script
            Script scr = new Script();
            scr.Name = context.scriptID().GetText();
            scr.ExecutionType = (short)_opcodes.GetScriptTypeOpcode(context.SCRIPTTYPE().GetText());
            scr.ReturnType = (short)_opcodes.GetTypeInfo(context.retType().GetText()).Opcode;
            scr.RootExpressionIndex = _currentIndex;
            var paramContext = context.scriptParams();

            //process parameters
            if (paramContext != null)
            {
                // extract strings from the context
                var names = paramContext.ID().Select(n => n.GetText()).ToArray();
                var valTypes = paramContext.VALUETYPE().Select(v => v.GetText()).ToArray();

                if (names.Count() != valTypes.Count())
                    throw new CompilerException($"Failed to parse Script \"{scr.Name}\" - Mismatched parameter arrays.", context);

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

        /// <summary>
        /// Calculates the value types for the parameters of a function and pushes them to the stack.
        /// </summary>
        /// <param name="info">The function's ScriptFunctionInfo</param>
        /// <param name="context">The call contect</param>
        /// <param name="actualParameterCount">The number of parameters which was extracted from the context</param>
        private void PushCallParameters(ScriptFunctionInfo info, BS_ReachParser.CallContext context, int contextParameterCount, string expectedReturnType)
        {
            // handle parameters
            int expectedParamCount = info.ParameterTypes.Count();
            if (expectedParamCount > 0)
            {
                if (contextParameterCount != expectedParamCount)
                    throw new CompilerException($"Failed to push function parameters for function \"{context.funcID().GetText()}\". Mismatched counts. Expected: \"{expectedParamCount}\" Encountered: \"{contextParameterCount}\".", context);

                // push params to the stack
                PushTypes(info.ParameterTypes);
            }
            #region special functions
            else
            {
                switch (info.Group)
                {
                    case "Begin":
                        // the last evaluated expression.
                        PushTypes(expectedReturnType);
                        if (info.Name.Contains("random"))
                            PushTypes(expectedReturnType, contextParameterCount - 1);
                        else
                            PushTypes("void", contextParameterCount - 1);
                        break;

                    case "BeginCount":
                        PushTypes(expectedReturnType, contextParameterCount - 1);
                        PushTypes("long");
                        break;

                    case "If":
                        if(expectedReturnType == "ANY")
                        {
                            PushTypes("void", contextParameterCount - 1);
                        }
                        else
                        {
                            PushTypes(expectedReturnType, contextParameterCount - 1);
                        }
                        PushTypes("boolean");
                        break;

                    case "Cond":
                        throw new CompilerException("A cond call was not regognized by the parser.", context);

                    case "Set":
                            PushTypes("GLOBALREFERENCE");
                            _set = true;
                            break;

                    case "Logical":
                        PushTypes("boolean", contextParameterCount);
                        break;

                    case "Arithmetic":
                        //PushTypes("NUMBER", contextParameterCount);
                        PushTypes("real", contextParameterCount);
                        break;

                    case "Equality":
                    case "Inequality":
                        PushTypes("ANY");
                        _equality = true;
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
                        if (contextParameterCount == 3)
                        {
                            PushTypes("short", "long");
                        }
                        else if(contextParameterCount == 2)
                        {
                            PushTypes("short");
                        }
                        PushTypes("boolean");
                        break;

                    case "SleepUntilGameTicks":
                        if (contextParameterCount != 2)
                        {
                            throw new CompilerException("The Compiler encountered a sleep_until_game_ticks " +
                                "call with more than two arguments.", context);
                        }

                        PushTypes("boolean", "short");
                        break;

                    case "CinematicSleep":
                        if (contextParameterCount != 1)
                        {
                            throw new CompilerException("The Compiler encountered a sleep_until_game_ticks " +
                                "call with more than one arguments.", context);
                        }

                        PushTypes("short");
                        break;

                    case "Wake":
                        PushTypes("script");
                        break;

                    case "Inspect":
                        PushTypes("ANY");
                        break;

                    case "Branch":
                        throw new CompilerException("A branch call was not regognized by the parser.", context);

                    case "ObjectCast":
                        PushTypes("object");
                        break;

                    case null:
                        break;

                    default:
                        throw new CompilerException($"Unimplemented function group: {info.Group}", context);
                }
            }
            #endregion
        }

        private void GenerateBranches()
        {
            foreach(var branch in _genBranches)
            {
                // create script entry
                Script scr = new Script();
                scr.Name = branch.Key;
                scr.ReturnType = (short)_opcodes.GetTypeInfo("void").Opcode;
                scr.ExecutionType = (short)_opcodes.GetScriptTypeOpcode("static");
                scr.RootExpressionIndex = _currentIndex;
                _scripts.Add(scr);

                // create the begin call
                ScriptFunctionInfo beginInfo = _opcodes.GetFunctionInfo("begin").First();
                var begin = new ScriptExpression(_currentIndex, beginInfo.Opcode, _opcodes.GetTypeInfo("void").Opcode,
                    ScriptExpressionType.Group, _randomAddress, _currentIndex.Next(), 0);
                _currentIndex.Increment();
                _expressions.Add(begin);

                // create the begin name
                var beginName = new ScriptExpression(_currentIndex, beginInfo.Opcode, _opcodes.GetTypeInfo("function_name").Opcode,
                    ScriptExpressionType.Expression, _currentIndex.Next(), _strings.Cache("begin"), (uint)0, 0);
                _currentIndex.Increment();
                _expressions.Add(beginName);

                // create the sleep_until call
                ScriptFunctionInfo sleepInfo = _opcodes.GetFunctionInfo("sleep_until").First();
                var sleepCall = new ScriptExpression(_currentIndex, sleepInfo.Opcode, _opcodes.GetTypeInfo("void").Opcode,
                    ScriptExpressionType.Group, _randomAddress, _currentIndex.Next(), 0);

                // link to the script reference
                ushort srIndex = (ushort)(_currentIndex.Index + 3);
                ushort srSalt = IndexToSalt(srIndex);
                sleepCall.Next = new DatumIndex(srSalt, srIndex);
                _currentIndex.Increment();
                _expressions.Add(sleepCall);

                // create the sleep_until name
                var sleepName = new ScriptExpression(_currentIndex, sleepInfo.Opcode, _opcodes.GetTypeInfo("function_name").Opcode,
                    ScriptExpressionType.Expression, _currentIndex.Next(), _strings.Cache("sleep_until"), (uint)0, 0);
                _currentIndex.Increment();
                _expressions.Add(sleepName);

                // adjust the boolean expression
                ScriptExpression bo = branch.Value[0];
                bo.Index = _currentIndex;
                bo.Next = DatumIndex.Null;
                _currentIndex.Increment();
                _expressions.Add(bo);

                // adjust the script reference
                ScriptExpression sr = branch.Value[1];
                sr.Index = _currentIndex;
                sr.Next = DatumIndex.Null;
                _currentIndex.Increment();
                _expressions.Add(sr);
            }
        }

        private void CreateInitialBegin(ushort returnType)
        {
            ScriptFunctionInfo info = _opcodes.GetFunctionInfo("begin").First();
            ushort op = info.Opcode;
            short line = 0;

            DatumIndex current = _currentIndex;
            _currentIndex.Increment();
            DatumIndex next = _currentIndex;

            // create the begin call
            var call = new ScriptExpression(current, op, returnType, ScriptExpressionType.Group, _randomAddress, next, line);
            _expressions.Add(call);

            // create the function name
            ushort valType = _opcodes.GetTypeInfo("function_name").Opcode;
            var funcName = new ScriptExpression(next, op, valType, ScriptExpressionType.Expression, 
                _strings.Cache("begin"), (uint)0, line);
            _currentIndex.Increment();
            OpenDatumAndAdd(funcName);
        }

        private void CreateFunctionCall(ushort returnType, ScriptFunctionInfo funcInfo, short lineNumber)
        {
            ushort op = funcInfo.Opcode;

            DatumIndex current = _currentIndex;
            _currentIndex.Increment();
            DatumIndex next = _currentIndex;

            ScriptExpression call = new ScriptExpression(current, op, returnType, ScriptExpressionType.Group, 
                _randomAddress, next, lineNumber);
            OpenDatumAndAdd(call);

            ushort nameType = _opcodes.GetTypeInfo("function_name").Opcode;
            ScriptExpression funcName = new ScriptExpression(next, op, nameType, ScriptExpressionType.Expression, 
                _strings.Cache(funcInfo.Name), (uint)0, lineNumber);
            _currentIndex.Increment();
            OpenDatumAndAdd(funcName);
        }

        private void CreateScriptReference(string name, ushort op, ushort valType, short lineNumber)
        {
            DatumIndex current = _currentIndex;
            _currentIndex.Increment();
            DatumIndex next = _currentIndex;

            ScriptExpression scrRef = new ScriptExpression(current, op, valType, ScriptExpressionType.ScriptReference,
                _randomAddress, next, lineNumber);
            OpenDatumAndAdd(scrRef);

            ushort nameType = _opcodes.GetTypeInfo("function_name").Opcode;
            ScriptExpression funcName = new ScriptExpression(next, op, nameType, ScriptExpressionType.Expression, 
                _strings.Cache(name), (uint)0, lineNumber);
            _currentIndex.Increment();
            OpenDatumAndAdd(funcName);
        }
    }
}
