using System;
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
        // debug
        public Boolean PrintDebugInfo = true;
        private Logger _logger;

        // lookups
        private ICacheFile _cashefile;
        private OpcodeLookup _opcodes;
        private ScriptContext _scriptContext;
        private Dictionary<int, UnitSeatMapping> _seatMappings;
        private List<ScriptDeclInfo> _scriptLookup = new List<ScriptDeclInfo>();
        private List<GlobalDeclInfo> _globalLookup = new List<GlobalDeclInfo>();
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

        // branching
        private ushort _branchBoolIndex = 0;
        private Dictionary<string, ExpressionBase[]> _genBranches = new Dictionary<string, ExpressionBase[]>();

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



        public ScriptCompiler(ICacheFile casheFile, ScriptContext context, OpcodeLookup opCodes, Dictionary<int, UnitSeatMapping> seatMappings, IProgress<int> progress, Logger logger)
        {
            _progress = progress;
            _cashefile = casheFile;
            _scriptContext = context;
            _opcodes = opCodes;
            _seatMappings = seatMappings;
            _logger = logger;
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
                _globalLookup = context.gloDecl().Select(g => new GlobalDeclInfo(g)).ToList();

            if (context.scriDecl() != null)
                _scriptLookup = context.scriDecl().Select(s => new ScriptDeclInfo(s)).ToList();

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

            var retType = context.retType().GetText();
            var exp_count = context.gloRef().Count() + context.call().Count() + context.branch().Count();

            // if the function has a return type, the last call or global reference must match it.
            if (retType == "void")
            {
                PushTypes("ANY");
            }
            else
            {
                PushTypes(retType);
            }
            // push the remaining parameters
            for (int i = 0; i < exp_count - 1; i++)
                PushTypes("ANY");

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
            glo.ExpressionIndex = new DatumIndex(_currentSalt, _currentExpressionIndex);
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
            Int32 contextParamCount = context.expr().Count();

            // handle script references
            if (IsScriptReference(context, expectedType, contextParamCount))
            {
                return;
            }

            // handle calls
            ScriptFunctionInfo info = RetrieveFunctionInfo(name, contextParamCount, context.Start.Line);
            // equality
            EqualityPush(info.ReturnType);
            PushCallParameters(info, context, contextParamCount, expectedType);

            // Create Function Call Expression
            FunctionCall funcCall = new FunctionCall();
            funcCall.Salt = _currentSalt;
            funcCall.OpCode = info.Opcode;
            funcCall.LineNumber = (short)context.Start.Line;

            // Calculate return type
            if(info.ReturnType == expectedType) // default case - matching types
            {
                funcCall.ValueType = _opcodes.GetTypeInfo(expectedType).Opcode;
            }
            else if(expectedType == "ANY")  // ANY
            {
                if(info.ReturnType == "passthrough" || info.Group == "SleepUntil")    // convert passthrough and sleep_until to void if the compiler doesn't expect any particular return type
                {
                    funcCall.ValueType = _opcodes.GetTypeInfo("void").Opcode;
                }
                else
                {
                    funcCall.ValueType = _opcodes.GetTypeInfo(info.ReturnType).Opcode;
                }
            }
            else if(expectedType == "NUMBER" && Casting.IsNumType(info.ReturnType))     // NUMBER
            {
                funcCall.ValueType = _opcodes.GetTypeInfo(info.ReturnType).Opcode;
            }
            else if(info.ReturnType == "passthrough")   // passthrough
            {
                funcCall.ValueType = _opcodes.GetTypeInfo(expectedType).Opcode;
            }
            else if (Casting.CanBeCasted(info.ReturnType, expectedType, expectedType ,_opcodes))     // casting
            {
                if (expectedType == "object_list")   // special cases
                {
                    funcCall.ValueType = _opcodes.GetTypeInfo("object_list").Opcode;
                }
                else
                {
                    funcCall.ValueType = _opcodes.GetTypeInfo(expectedType).Opcode;
                }
            }
            else
            {
                throw new CompilerException($"The compiler expected a function with the return type \"{expectedType}\" while processing \"{name}\"." +
                    $" It encountered \"{info.ReturnType}\".",context);
            }
            
            IncrementDatum();
            funcCall.Value = new DatumIndex(_currentSalt, _currentExpressionIndex);
            OpenDatumAndAdd(funcCall);
            AddFunctionName(name, funcCall.OpCode, funcCall.LineNumber);
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

            string expectedType = PopType();    // just ignore the type for now

            // branch excepts two parameters
            if (context.expr().Count() != 2)
            {
                throw new CompilerException("A branch statement had an unexpected number of parameters.", context);
            }

            PushTypes("boolean", "ANY");

            ScriptFunctionInfo info = _opcodes.GetFunctionInfo("branch").First();
            UInt16 op = info.Opcode;

            // create the branch function call and its name expression
            FunctionCall call = new FunctionCall(_currentSalt, op, _opcodes.GetTypeInfo("void").Opcode, (short)context.Start.Line);
            IncrementDatum();
            call.Value = new DatumIndex(_currentSalt, _currentExpressionIndex);
            OpenDatumAndAdd(call);

            AddFunctionName("branch", op, (short)context.Start.Line);

            _branchBoolIndex = _currentExpressionIndex;
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

            ExpressionBase[] expressions = new ExpressionBase[2];
            // grab boolean expression
            var bol = _expressions[_branchBoolIndex].Clone();
            expressions[0] = bol;
            // grab script ref
            var sr = _expressions[_expressions.Count - 2].Clone();
            expressions[1] = sr;
            // modify the original script ref. the opcode points to the generated script
            int genScriptIndex = _scriptLookup.Count;
            _expressions[_expressions.Count - 2].OpCode = (ushort)genScriptIndex;
            // add the generated script to the lookup
            ScriptDeclInfo decl = new ScriptDeclInfo(genName, "static", "void");
            _scriptLookup.Add(decl);

            _genBranches.Add(genName, expressions);
            CloseDatum();
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
                UInt16 opc = _opcodes.GetTypeInfo(valType).Opcode;
                Expression32 exp = new Expression32(_currentSalt, opc, opc, _strings.Cache(txt), (short)context.Start.Line);
                IncrementDatum();
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

        private bool IsGlobalReference(BS_ReachParser.LitContext context, string expReturnType)
        {
            //todo: implement casting?

            string text = context.GetText();

            // check if this literal is an engine global
            GlobalInfo engineGlobal = _opcodes.GetGlobalInfo(text);
            if (engineGlobal != null)
            {
                string retType = engineGlobal.ReturnType;
                ushort opc = GetGlobalOpCode(context, retType);
                var exp = new EngineGlobalReference(_currentSalt, opc, _opcodes.GetTypeInfo(retType).Opcode, _strings.Cache(text), engineGlobal.MaskedOpcode, (short)context.Start.Line);
                IncrementDatum();
                OpenDatumAndAdd(exp);
                return true;
            }
            else
            {
                int index = _globalLookup.FindIndex(glo => glo.Name == text);

                // not found
                if (index == -1)
                {
                    if (expReturnType == "GLOBALREFERENCE")
                        throw new ArgumentException($"GLOBALREFERENCE: No matching global could be found. Name: \"{text}\". Line: {context.Start.Line}");
                    else
                        return false;
                }
                else
                {
                    string retType;

                    // calculate return type
                    if(Casting.IsFlexType(expReturnType) || expReturnType == _globalLookup[index].ValueType)    // default case
                    {
                        retType = _globalLookup[index].ValueType;
                    }
                    else if (Casting.CanBeCasted(_globalLookup[index].ValueType, expReturnType, expReturnType, _opcodes))        // casting
                    {
                        retType = expReturnType;
                    }
                    else
                    {
                        throw new CompilerException($"The global \"{text}\" can't be casted from \"{_globalLookup[index]}\" to \"{expReturnType}\".", context);
                    }

                    ushort opc = GetGlobalOpCode(context, retType);
                    var exp = new GlobalReference(_currentSalt, opc, _opcodes.GetTypeInfo(retType).Opcode, _strings.Cache(text), index, (short)context.Start.Line);
                    IncrementDatum();
                    OpenDatumAndAdd(exp);
                    return true;
                }
            }
        }

        private ushort GetGlobalOpCode(BS_ReachParser.LitContext context, string retType)
        {
            ushort opcode = _opcodes.GetTypeInfo(retType).Opcode;
            var grandparent = context.Parent.Parent as BS_ReachParser.CallContext;

            // "set" and (In)Equality functions are special
            if (grandparent != null)
            {
                string funcName = grandparent.funcID().GetText();
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

                        PushTypes(retType); // the next parameter must have the same return type as this global
                        _set = false;
                    }
                }
            }

            // equality
            EqualityPush(retType);

            return opcode;
        }

        private bool IsScriptReference(BS_ReachParser.CallContext context, string expectedReturnType, Int32 expectedParamCount)
        {
            string name = context.funcID().GetText();

            int index = -1;

            // try to find a matching script
            if (expectedReturnType == "NUMBER")
            {
                index = _scriptLookup.FindIndex(s => s.Name == name && s.Parameters.Count == expectedParamCount && Casting.IsNumType(s.ReturnType));
            }
            else
            {
                index = _scriptLookup.FindIndex(s => s.Name == name && s.Parameters.Count == expectedParamCount);
            }

            // a matching script wasn't found...not a script reference
            if (index == -1)
                return false;

            ScriptDeclInfo info = _scriptLookup[index];
            string retType;

            // check if the script satisfies the return type requirement
            if(expectedReturnType != "ANY" && expectedReturnType != info.ReturnType)
            {
                // NUMBER
                if(expectedReturnType == "NUMBER" && Casting.IsNumType(info.ReturnType))
                {
                    retType = info.ReturnType;
                }
                // casting
                else if(Casting.CanBeCasted(info.ReturnType, expectedReturnType, expectedReturnType, _opcodes))
                {
                    retType = expectedReturnType;
                }
                // the script didn't satisfy the requirement
                else
                {
                    return false;
                }
            }
            // ANY and matching types
            else
            {
                retType = info.ReturnType;
            }

            // check for equality functions
            EqualityPush(retType);

            // handle parameters. Push them to the stack
            if (expectedParamCount > 0)
            {
                string[] types = info.Parameters.Select(p=> p.ValueType).ToArray();
                PushTypes(types);
            }

            // create Script Reference node
            ushort valType = _opcodes.GetTypeInfo(retType).Opcode;
            ScriptReference scrRef = new ScriptReference(_currentSalt, (ushort)index, valType, (short)context.Start.Line);

            IncrementDatum();
            scrRef.Value = new DatumIndex(_currentSalt, _currentExpressionIndex);     // close Value: Index to name                                                                                     
            OpenDatumAndAdd(scrRef);                                                  // open next expression Datum

            AddFunctionName(name, scrRef.OpCode, scrRef.LineNumber);
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
                    throw new CompilerException($"The variable  \"{name}\" can't be casted from \"{_variables[index]}\" to \"{expectedReturnType}\".", context);
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
            ScriptVariableReference exp = new ScriptVariableReference(_currentSalt, opcode, valop, _strings.Cache(name), (short)context.Start.Line);
            exp.Value = index;

            IncrementDatum();
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
            scr.RootExpressionIndex = new DatumIndex(_currentSalt, _currentExpressionIndex);
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
                        PushTypes(expectedReturnType);  // the last evaluated expression
                        PushTypes("ANY", contextParameterCount -1);
                        break;

                    case "BeginCount":
                        PushTypes(expectedReturnType, contextParameterCount - 1);
                        PushTypes("NUMBER");
                        break;

                    case "If":
                        PushTypes("ANY", contextParameterCount - 1);
                        PushTypes("boolean");
                        break;

                    case "Cond":
                        for (int i = 0; i < (contextParameterCount / 2); i++)
                        {
                            PushTypes("boolean", "ANY");
                        }
                        break;

                    case "Set":
                            PushTypes("GLOBALREFERENCE");
                            _set = true;
                            break;

                    case "Logical":
                        PushTypes("boolean", contextParameterCount);
                        break;

                    case "Arithmetic":
                        PushTypes("NUMBER", contextParameterCount);
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
                            PushTypes("short", "NUMBER");
                        }
                        else if(contextParameterCount == 2)
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
                        throw new CompilerException("A branch call was not regognized by the parser.", context);

                    case "ObjectCast":
                        PushTypes("object");
                        break;
                        #endregion
                }
            }
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
                scr.RootExpressionIndex = new DatumIndex(_currentSalt, _currentExpressionIndex);
                _scripts.Add(scr);

                // create the begin call
                ScriptFunctionInfo beginInfo = _opcodes.GetFunctionInfo("begin").First();
                FunctionCall begin = new FunctionCall(_currentSalt, beginInfo.Opcode, _opcodes.GetTypeInfo("void").Opcode, 0);
                begin.NextExpression = DatumIndex.Null;
                IncrementDatum();
                begin.Value = new DatumIndex(_currentSalt, _currentExpressionIndex);
                _expressions.Add(begin);

                // create the begin name
                Expression32 beginName = new Expression32(_currentSalt, beginInfo.Opcode, _opcodes.GetTypeInfo("function_name").Opcode, _strings.Cache("begin"), 0);
                beginName.Value = 0;
                IncrementDatum();
                beginName.NextExpression = new DatumIndex(_currentSalt, _currentExpressionIndex);
                _expressions.Add(beginName);

                // create the sleep_until call
                ScriptFunctionInfo sleepInfo = _opcodes.GetFunctionInfo("sleep_until").First();
                FunctionCall sleepCall = new FunctionCall(_currentSalt, sleepInfo.Opcode, _opcodes.GetTypeInfo("void").Opcode, 0);
                // link to the script reference
                ushort srIndex = (ushort)(_currentSalt + 3);
                ushort srSalt = IndexToSalt(srIndex);
                sleepCall.NextExpression = new DatumIndex(srSalt, srIndex);
                IncrementDatum();
                sleepCall.Value = new DatumIndex(_currentSalt, _currentExpressionIndex);
                _expressions.Add(sleepCall);

                // create the sleep_until name
                Expression32 sleepName = new Expression32(_currentSalt, sleepInfo.Opcode, _opcodes.GetTypeInfo("function_name").Opcode, _strings.Cache("sleep_until"), 0);
                sleepName.Value = 0;
                IncrementDatum();
                sleepName.NextExpression = new DatumIndex(_currentSalt, _currentExpressionIndex);
                _expressions.Add(sleepName);

                // adjust the boolean expression
                ExpressionBase bo = branch.Value[0];
                bo.Salt = _currentSalt;
                bo.NextExpression = DatumIndex.Null;
                IncrementDatum();
                _expressions.Add(bo);

                // adjust the script reference
                ExpressionBase sr = branch.Value[1];
                sr.Salt = _currentSalt;
                sr.NextExpression = DatumIndex.Null;
                IncrementDatum();
                _expressions.Add(sr);
            }
        }
    }
}
