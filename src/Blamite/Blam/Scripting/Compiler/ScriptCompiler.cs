using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : BS_ReachBaseListener
    {
        // debug
        private readonly ScriptCompilerLogger _logger;

        // lookups
        private readonly ICacheFile _cacheFile;
        private readonly OpcodeLookup _opcodes;
        private readonly ScriptContext _scriptContext;
        private readonly Dictionary<string, UnitSeatMapping> _seatMappings;
        // TODO: Change the script lookup from dictionary to lookup.
        private Dictionary<string, ScriptInfo> _scriptLookup = new Dictionary<string, ScriptInfo>();
        private Dictionary<string, GlobalInfo> _mapGlobalsLookup = new Dictionary<string, GlobalInfo>();
        private readonly Dictionary<string, ParameterInfo> _parameterLookup = new Dictionary<string, ParameterInfo>();

        // script tables
        private readonly StringTable _strings = new StringTable();
        private readonly List<ScriptExpression> _expressions = new List<ScriptExpression>();
        private readonly List<Script> _scripts = new List<Script>();
        private readonly List<ScriptGlobal> _globals = new List<ScriptGlobal>();
        private readonly List<ITag> _references = new List<ITag>();

        // utility
        private const uint _randomAddress = 0xCDCDCDCD;  // used for expressions where the string address doesn't point to the string table
        private const int _globalPushIndex = -1;
        private DatumIndex _currentIndex;
        private readonly Stack<int> _openDatums = new Stack<int>();
        private readonly Stack<string> _expectedTypes = new Stack<string>();

        // branching
        private ushort _branchBoolIndex = 0;
        private readonly Dictionary<string, ScriptExpression[]> _generatedBranches = new Dictionary<string, ScriptExpression[]>();

        // cond
        private string _condReturnType;
        private readonly Stack<int> _condIndeces = new Stack<int>();

        // equality
        private bool _equality = false;

        // set
        private bool _set = false;

        // progress
        private readonly IProgress<int> _progress;
        private int _declarationCount = 0;
        private int _processedDeclarations = 0;

        // returned data
        private ScriptData _result = null;


        public ScriptCompiler(ICacheFile casheFile, ScriptContext context, OpcodeLookup opCodes, Dictionary<string, UnitSeatMapping> seatMappings, IProgress<int> progress, ScriptCompilerLogger logger)
        {
            _progress = progress;
            _cacheFile = casheFile;
            _scriptContext = context;
            _opcodes = opCodes;
            _seatMappings = seatMappings;
            _logger = logger;

            ushort intialSalt = SaltGenerator.GetSalt("script node");
            _currentIndex = new DatumIndex(intialSalt, 0);
        }

        public ScriptData Result()
        {
            return _result;
        }

        public bool OutputDebugInfo { get; set; } = false;

        /// <summary>
        /// Generate script and global lookups.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterHsc(BS_ReachParser.HscContext context)
        {
            // Generate the globals lookup.
            if (context.globalDeclaration() != null)
            {
                _mapGlobalsLookup = context.globalDeclaration().Select((g, index) => new GlobalInfo(g, (ushort)index)).ToDictionary(g => g.Name);

            }

            // Generate the script lookup.
            if (context.scriptDeclaration() != null)
            {
                _scriptLookup = context.scriptDeclaration().Select((s, index) => new ScriptInfo(s, (ushort)index)).ToDictionary(s => s.Name + "_" + s.Parameters.Count);

            }

            // The declarations count is used to calculate the current progress.
            _declarationCount = context.scriptDeclaration().Count() + context.globalDeclaration().Count();
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
        public override void EnterScriptDeclaration(BS_ReachParser.ScriptDeclarationContext context)
        {
            if(OutputDebugInfo)
            {
                _logger.Script(context, CompilerContextAction.Enter);
            }

            // Create new script object and add it to the table.
            Script script = GetScriptFromContext(context);
            _scripts.Add(script);

            // Generate the parameter lookup.
            for(ushort i = 0; i < script.Parameters.Count; i++)
            {
                ScriptParameter parameter = script.Parameters[i];
                var info = new ParameterInfo(parameter.Name, _opcodes.GetTypeInfo(parameter.Type).Name, i);
                _parameterLookup.Add(info.Name, info);
            }

            string returnType = context.VALUETYPE().GetText();
            int expressionCount = context.globalsReference().Count() + context.call().Count() + context.branch().Count() + context.cond().Count();

            // The final expression must match the return type of this script.
            PushTypes(returnType);
            // The other expressions can be of any type.
            if (expressionCount > 1)
            {
                PushTypes("void", expressionCount - 1);
            }

            CreateInitialBegin(returnType);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitScriptDeclaration(BS_ReachParser.ScriptDeclarationContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Script(context, CompilerContextAction.Exit);
            }

            _parameterLookup.Clear();
            CloseDatum();
            ReportProgress();
        }

        /// <summary>
        /// Processes global declarations. Opens a datum. Creates the global node. Pushes the value type.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterGlobalDeclaration(BS_ReachParser.GlobalDeclarationContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Global(context, CompilerContextAction.Enter);
            }

            // Create a new Global and add it to the table.
            string valueType = context.VALUETYPE().GetText();
            ScriptGlobal glo = new ScriptGlobal()
            {
                Name = context.ID().GetText(),
                Type = (short)_opcodes.GetTypeInfo(valueType).Opcode,
                ExpressionIndex = _currentIndex,
            };

            _globals.Add(glo);

            if (OutputDebugInfo)
            {
                _logger.Information($"A global declaration was detected by the parser. The index {_globalPushIndex} will be opened.");
            }
            OpenDatum(_globalPushIndex);
            PushTypes(valueType);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitGlobalDeclaration(BS_ReachParser.GlobalDeclarationContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Global(context, CompilerContextAction.Exit);
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
            if (OutputDebugInfo)
            {
                _logger.Call(context, CompilerContextAction.Enter);
            }

            LinkDatum();

            // Retrieve information from the context.
            string name = context.functionID().GetText();
            string expectedType = PopType();
            int contextParameterCount = context.expression().Count();

            // Handle script references.
            if (IsScriptReference(expectedType, contextParameterCount, context))
            {
                return;
            }

            FunctionInfo info = RetrieveFunctionInfo(name, contextParameterCount, GetLineNumber(context));
            ushort returnTypeOpcode = DetermineReturnTypeOpcode(info, expectedType, context);
            EqualityPush(info.ReturnType);
            PushCallParameters(info, contextParameterCount, expectedType, context);
            CreateFunctionCall(info, returnTypeOpcode, GetLineNumber(context));
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitCall(BS_ReachParser.CallContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Call(context, CompilerContextAction.Exit);
            }

            CloseDatum();
        }

        public override void EnterBranch(BS_ReachParser.BranchContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Branch(context, CompilerContextAction.Enter);
            }

            LinkDatum();
            _ = PopType();    // Just ignore the type for now.

            // branch excepts two parameters
            if (context.expression().Count() != 2)
            {
                throw new CompilerException("A branch call had an unexpected number of parameters.", context);
            }

            PushTypes("boolean", "SCRIPTREFERENCE");
            FunctionInfo info = _opcodes.GetFunctionInfo("branch").First();
            CreateFunctionCall(info, _opcodes.GetTypeInfo("void").Opcode, GetLineNumber(context));

            _branchBoolIndex = _currentIndex.Index;
        }

        public override void ExitBranch(BS_ReachParser.BranchContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Branch(context, CompilerContextAction.Exit);
            }

            // Generate the script name.
            BS_ReachParser.ScriptDeclarationContext scriptContext = GetParentScriptContext(context);
            string fromScript = scriptContext.scriptID().GetText();

            var parameters = context.expression();
            if (parameters[1].call() == null)
            {
                throw new CompilerException("A branch statements second parameter was not a script.", context);
            }
            var param = parameters[1].call();
            string toScript = param.functionID().GetText();
            string generatedName = fromScript + "_to_" + toScript;

            ScriptExpression[] expressions = new ScriptExpression[2];
            var condition = _expressions[_branchBoolIndex].Clone();
            var scriptReference = _expressions[condition.Next.Index].Clone();
            expressions[0] = condition;
            expressions[1] = scriptReference;
            // Modify the original script reference. The opcode points to the generated script.
            _expressions[condition.Next.Index].Opcode = (ushort)_scriptLookup.Count;

            // Add the generated script to the lookup.
            ScriptInfo info = new ScriptInfo(generatedName, "static", "void", (ushort)_scriptLookup.Count);
            string infoKey = info.Name + "_" + 0;
            _scriptLookup[infoKey] = info;
            _generatedBranches[generatedName] = expressions;

            CloseDatum();
        }

        public override void EnterCond(BS_ReachParser.CondContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Cond(context, CompilerContextAction.Enter);
            }

            // Tell the groups what type to expect.
            _condReturnType = PopType();

            // Is this still necessary?
            //if (_condReturnType == "ANY")
            //    _condReturnType = "void";

            // Push the index to the first group, so that we can open it later on.
            _condIndeces.Push(_expressions.Count);
        }

        public override void ExitCond(BS_ReachParser.CondContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Cond(context, CompilerContextAction.Exit);
            }

            // Link to the compiler generated begin call of the last open cond group.
            LinkDatum();

            // Add the final expression of the cond construct. Not sure why the official Blam Script compiler adds these.
            ushort typeOpcode = _opcodes.GetTypeInfo(_condReturnType).Opcode;
            var expression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = typeOpcode,
                ReturnType = typeOpcode,
                Type = ScriptExpressionType.Expression,
                Next = DatumIndex.Null,
                StringOffset = _randomAddress,
                Value = 0,
                LineNumber = 0
            };
            AddExpressionIncrement(expression);

            // Open the first group.
            int firstGroupIndex = _condIndeces.Pop();
            OpenDatum(firstGroupIndex);
        }

        public override void EnterCondGroup(BS_ReachParser.CondGroupContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.CondGroup(context, CompilerContextAction.Enter);
            }

            // Link to the previous expression or cond group.
            LinkDatum();

            // push the types of the group members.
            PushTypes(_condReturnType, context.expression().Count() - 1);
            PushTypes("boolean");

            var ifInfo = _opcodes.GetFunctionInfo("if")[0];
            var compilerIf = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = ifInfo.Opcode,
                ReturnType = _opcodes.GetTypeInfo(_condReturnType).Opcode,
                Type = ScriptExpressionType.Group,
                Next = DatumIndex.Null,
                StringOffset = _randomAddress,
                LineNumber = 0
            };
            compilerIf.SetValue(_currentIndex.Next);

            // Keep the if call closed. We will open the initial one later on.
            AddExpressionIncrement(compilerIf);

            var compilerIfName = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = ifInfo.Opcode,
                ReturnType = _opcodes.GetTypeInfo("function_name").Opcode,
                Type = ScriptExpressionType.Expression,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(ifInfo.Name),
                Value = 0,
                LineNumber = GetLineNumber(context)
            };
            OpenDatumAddExpressionIncrement(compilerIfName);

            // Push the index to the condition so that we can modify it later on.
            _condIndeces.Push(_expressions.Count);
        }

        public override void ExitCondGroup(BS_ReachParser.CondGroupContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.CondGroup(context, CompilerContextAction.Exit);
            }

            // Close the final value expression.
            CloseDatum();

            int index = _condIndeces.Pop();
            var beginInfo = _opcodes.GetFunctionInfo("begin")[0];

            // Grab the index to value expression of the cond group. Modify the condition expression afterwards.
            // Next has to point to the begin call, which is added by the compiler.
            var valueExpressionDatum = _expressions[index].Next;
            _expressions[index].Next = _currentIndex;

            var compilerBeginCall = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = beginInfo.Opcode,
                ReturnType = _opcodes.GetTypeInfo(_condReturnType).Opcode,
                Type = ScriptExpressionType.Group,
                Next = DatumIndex.Null,
                StringOffset = _randomAddress,
                LineNumber = 0
            };
            compilerBeginCall.SetValue(_currentIndex.Next);
            OpenDatumAddExpressionIncrement(compilerBeginCall);

            var compilerBeginName = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = beginInfo.Opcode,
                ReturnType = _opcodes.GetTypeInfo("function_name").Opcode,
                Type = ScriptExpressionType.Expression,
                Next = valueExpressionDatum,
                StringOffset = _strings.Cache(beginInfo.Name),
                Value = 0,
                LineNumber = 0
            };
            AddExpressionIncrement(compilerBeginName);
        }

        public override void EnterGlobalsReference(BS_ReachParser.GlobalsReferenceContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.GlobalReference(context, CompilerContextAction.Enter);
            }

            LinkDatum();
            string expectedType = PopType();

            if (!IsGlobalsReference(expectedType, context))
            {
                throw new CompilerException("The parser detected a Globals Reference, but the expression doesn't seem to be one.", context);
            }
        }

        /// <summary>
        /// Processes regular expressions, script variables and global references. Links to a datum. Opens a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterLiteral(BS_ReachParser.LiteralContext context)
        {
            if (OutputDebugInfo)
            {
                _logger.Literal(context, CompilerContextAction.Enter);
            }

            LinkDatum();

            string text = context.GetText();
            string expectedType = PopType();

            // Handle "none" expressions. The Value field of ai_line expressions stores their string offset.
            // Therefore the Value is not -1 if the expression is "none".
            if(text == "none" && expectedType != "ai_line")
            {                
                ushort opcode = _opcodes.GetTypeInfo(expectedType).Opcode;
                var expression = new ScriptExpression
                {
                    Index = _currentIndex,
                    Opcode = opcode,
                    ReturnType = opcode,
                    Type = ScriptExpressionType.Expression,
                    Next = DatumIndex.Null,
                    StringOffset = _strings.Cache(text),
                    Value = 0xFFFFFFFF,
                    LineNumber = GetLineNumber(context)
                };
                OpenDatumAddExpressionIncrement(expression);
                return;
            }

            // handle script variable references
            if (IsScriptParameter(expectedType, context))
                return;

            // handle global references
            if (IsGlobalsReference(expectedType, context))
                return;
            
            // handle regular expressions
            if (ProcessLiteral(expectedType, context))
                return;

            throw new CompilerException($"Failed to process \"{text}\".", context);
        }

        private bool IsGlobalsReference(string expectedType, ParserRuleContext context)
        {
            string text = context.GetText();
            GlobalInfo globalInfo = _opcodes.GetGlobalInfo(text);
            object value;

            // Engine global.
            if(globalInfo != null)
            {
                ushort[] arr = { 0xFFFF, globalInfo.MaskedOpcode };
                value = arr;
            }
            // Map global.
            else if(_mapGlobalsLookup.ContainsKey(text))
            {
                globalInfo = _mapGlobalsLookup[text];
                value = (uint)globalInfo.Opcode;
            }
            // Not a global...
            else if (expectedType == "GLOBALREFERENCE")
            {
                throw new CompilerException($"GLOBALREFERENCE: No matching global could be found.", context);
            }
            else
            {
                return false;
            }

            ushort returnTypeOpcode = DetermineReturnTypeOpcode(globalInfo, expectedType, context);
            ushort opcode = GetGlobalOpCode(returnTypeOpcode, context);
            var expression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = opcode,
                ReturnType = returnTypeOpcode,
                Type = ScriptExpressionType.GlobalsReference,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(text),
                LineNumber = GetLineNumber(context)
            };
            expression.SetValue(value);
            OpenDatumAddExpressionIncrement(expression);

            return true;
        }

        private ushort GetGlobalOpCode(ushort expectedTypeOpcode, RuleContext context)
        {
            RuleContext grandparent = GetParentContext(context, BS_ReachParser.RULE_call);
            ushort opcode = expectedTypeOpcode;

            // "set" and (in)equality functions are special.
            if (grandparent is BS_ReachParser.CallContext call)
            {
                string funcName = call.functionID().GetText();
                List<FunctionInfo> funcInfo = _opcodes.GetFunctionInfo(funcName);

                if (funcInfo != null)
                {
                    if ((funcInfo[0].Group == "Equality" || funcInfo[0].Group == "Inequality") && _equality)
                    {
                        opcode = 0;
                    }
                    else if (_set && funcInfo[0].Group == "Set")
                    {
                        opcode = 0xFFFF;

                        if (OutputDebugInfo)
                        {
                            _logger.Information("The Global belongs to a set call. It's value type will be pushed to the stack.");
                        }
                        // The next parameter must have the same return type as this global
                        PushTypes(_opcodes.GetTypeInfo(expectedTypeOpcode).Name);
                        _set = false;
                    }
                }
            }
            EqualityPush(_opcodes.GetTypeInfo(expectedTypeOpcode).Name);

            return opcode;
        }

        private ushort DetermineReturnTypeOpcode(IScriptingConstantInfo info, string expectedType, ParserRuleContext context)
        {
            string calculatedType = expectedType switch
            {
                "ANY" when info.ReturnType == "passthrough" => "void",
                "ANY" => info.ReturnType,
                // Cast globals in arithmetic functions to real.
                "NUMBER" when info is GlobalInfo && Casting.IsNumType(info.ReturnType) => "real",
                "NUMBER" when Casting.IsNumType(info.ReturnType) => info.ReturnType,
                "NUMBER" when !Casting.IsNumType(info.ReturnType) => "",
                "void" => "void",
                "GLOBALREFERENCE" when info is GlobalInfo => info.ReturnType,
                "SCRIPTREFERENCE" when info is ScriptInfo => info.ReturnType,
                _ when expectedType == info.ReturnType => expectedType,
                _ when info.ReturnType == "passthrough" => expectedType,
                _ when Casting.CanBeCasted(info.ReturnType, expectedType, _opcodes) => expectedType,
                _ => ""
            };

            if (calculatedType == "")
            {
                throw new CompilerException($"The compiler failed to calculate the return type of an expression. It expected the return type \"{expectedType}\" while processing \"{info.Name}\"." +
                    $" It encountered the return type \"{info.ReturnType}\".", context);
            }

            return _opcodes.GetTypeInfo(calculatedType).Opcode;
        }

        private bool IsScriptReference(string expectedReturnType, int expectedParameterCount, BS_ReachParser.CallContext context)
        {
            string key = context.functionID().GetText() + "_" + expectedParameterCount;
            if(!_scriptLookup.TryGetValue(key, out ScriptInfo info))
            {
                if (expectedReturnType == "SCRIPTREFERENCE")
                    throw new CompilerException("The compiler expected a Script Reference but was unable to find a matching one. " +
                        "Please check your script declarations and your spelling.", context);
                else
                    return false;
            }

            ushort returnTypeOpcode = DetermineReturnTypeOpcode(info, expectedReturnType, context);

            // Check for equality functions.
            EqualityPush(_opcodes.GetTypeInfo(returnTypeOpcode).Name);

            // Push the parameters to the type stack.
            if (expectedParameterCount > 0)
            {
                string[] parameterTypes = info.Parameters.Select(p=> p.ReturnType).ToArray();
                PushTypes(parameterTypes);
            }

            // Create a script reference node.
            CreateScriptReference(info, returnTypeOpcode, GetLineNumber(context));

            return true;
        }

        private bool IsScriptParameter(string expectedReturnType, ParserRuleContext context)
        {
            // This script doesn't have parameters.
            if (_parameterLookup.Count == 0)
            {
                return false;
            }

            string text = context.GetText().Trim('"');

            // The script doesn't have a parameter with this name.
            if (!_parameterLookup.TryGetValue(text, out ParameterInfo info))
            {
                return false;
            }

            //// Casting is not required.
            //if(Casting.IsFlexType(expectedReturnType) || expectedReturnType == info.ReturnType)
            //{
            //    returnType = info.ReturnType;
            //}
            //// Casting.
            //else
            //{
            //    if(Casting.CanBeCasted(info.ReturnType, expectedReturnType, _opcodes))
            //    {
            //        returnType = expectedReturnType;
            //    }
            //    else
            //    {
            //        throw new CompilerException($"The parameter  \"{text}\" can't be casted from \"{info.ReturnType}\" to \"{expectedReturnType}\".", context);
            //    }
            //}

            ushort returnTypeOpcode = DetermineReturnTypeOpcode(info, expectedReturnType, context);
            ushort opcode = returnTypeOpcode;

            // (In)Equality functions are special
            if(context.Parent.Parent is BS_ReachParser.CallContext grandparent)
            {
                string funcName = grandparent.functionID().GetText();
                var funcInfo = _opcodes.GetFunctionInfo(funcName);
                if(funcInfo != null)
                {
                    if ((funcInfo[0].Group == "Equality" || funcInfo[0].Group == "Inequality") && _equality)
                    {
                        opcode = 0;
                    }
                }
            }

            var expression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = opcode,
                ReturnType = returnTypeOpcode,
                Type = ScriptExpressionType.ParameterReference,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(info.Name),
                Value = info.Opcode,
                LineNumber = GetLineNumber(context)
            };
            EqualityPush(info.ReturnType);
            OpenDatumAddExpressionIncrement(expression);

            return true;
        }

        private Script GetScriptFromContext(BS_ReachParser.ScriptDeclarationContext context)
        {
            // Create a new Script.
            Script script = new Script
            {
                Name = context.scriptID().GetText(),
                ExecutionType = (short)_opcodes.GetScriptTypeOpcode(context.SCRIPTTYPE().GetText()),
                ReturnType = (short)_opcodes.GetTypeInfo(context.VALUETYPE().GetText()).Opcode,
                RootExpressionIndex = _currentIndex
            };
            // Handle scripts with parameters.
            var parameterContext = context.scriptParameters();
            if (parameterContext != null)
            {
                var parameters = parameterContext.parameterGroup();
                for(ushort i = 0; i < parameters.Length; i++)
                {
                    string name = parameters[i].ID().GetText();
                    string valueType = parameters[i].VALUETYPE().GetText();

                    // Add the parameter to the script object.
                    ScriptParameter parameter = new ScriptParameter
                    {
                        Name = name,
                        Type = _opcodes.GetTypeInfo(valueType).Opcode
                    };
                    script.Parameters.Add(parameter);

                    //// Add the parameter to the parameter lookup.
                    //var info = new ParameterInfo(name, valueType, i);
                    //_parameterLookup[info.Name] = info;
                }
            }
            return script;
        }

        /// <summary>
        /// Calculates the value types for the parameters of a function and pushes them to the stack.
        /// </summary>
        /// <param name="info">The function's ScriptFunctionInfo</param>
        /// <param name="context">The call contect</param>
        /// <param name="actualParameterCount">The number of parameters which was extracted from the context</param>
        private void PushCallParameters(FunctionInfo info, int contextParameterCount, string expectedReturnType, BS_ReachParser.CallContext context)
        {
            // Handle parameters.
            int expectedParamCount = info.ParameterTypes.Count();
            if (expectedParamCount > 0)
            {
                if (contextParameterCount != expectedParamCount)
                {
                    throw new CompilerException($"Failed to push function parameters for function \"{context.functionID().GetText()}\". Mismatched counts. Expected: \"{expectedParamCount}\" Encountered: \"{contextParameterCount}\".", context);

                }
                PushTypes(info.ParameterTypes);
            }
            // TODO: Throw exceptions if a wrong number of parameters is detected.
            #region special functions
            else
            {
                switch (info.Group)
                {
                    case "Begin":
                        if (info.Name.Contains("random"))
                        {
                            PushTypes(expectedReturnType, contextParameterCount);

                        }
                        else
                        {
                            // the last evaluated expression.
                            PushTypes(expectedReturnType);
                            PushTypes("void", contextParameterCount - 1);
                        }
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
                        PushTypes("real", contextParameterCount);
                        break;

                    case "Equality":
                    case "Inequality":
                        PushTypes("ANY");
                        _equality = true;
                        break;

                    case "Sleep":
                        if (contextParameterCount == 2)
                        {
                            PushTypes("script");
                        }
                        PushTypes("short");
                        break;

                    case "SleepForever":
                        if (contextParameterCount == 1)
                        {
                            PushTypes("script");
                        }
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
            foreach(var branch in _generatedBranches)
            {
                if(OutputDebugInfo)
                {
                    _logger.Information($"Generating Branch: {branch.Key}");
                }

                ushort voidOpcode = _opcodes.GetTypeInfo("void").Opcode;

                // create script entry
                Script script = new Script
                {
                    Name = branch.Key,
                    ReturnType = (short)voidOpcode,
                    ExecutionType = (short)_opcodes.GetScriptTypeOpcode("static"),
                    RootExpressionIndex = _currentIndex,
                };
                _scripts.Add(script);

                CreateInitialBegin("void");
                LinkDatum();

                //// create the begin call
                //ScriptFunctionInfo beginInfo = _opcodes.GetFunctionInfo("begin").First();
                //var begin = new ScriptExpression(_currentIndex, beginInfo.Opcode, _opcodes.GetTypeInfo("void").Opcode,
                //    ScriptExpressionType.Group, _randomAddress, _currentIndex.Next(), 0);
                //_currentIndex.Increment();
                //_expressions.Add(begin);

                //// create the begin name
                //var beginName = new ScriptExpression(_currentIndex, beginInfo.Opcode, _opcodes.GetTypeInfo("function_name").Opcode,
                //    ScriptExpressionType.Expression, _currentIndex.Next(), _strings.Cache("begin"), (uint)0, 0);
                //_currentIndex.Increment();
                //_expressions.Add(beginName);

                // create the sleep_until call
                FunctionInfo sleepInfo = _opcodes.GetFunctionInfo("sleep_until").First();
                CreateFunctionCall(sleepInfo, voidOpcode, 0);

                //var sleepCall = new ScriptExpression(_currentIndex, sleepInfo.Opcode, _opcodes.GetTypeInfo("void").Opcode,
                //    ScriptExpressionType.Group, _randomAddress, _currentIndex.Next(), 0);

                //// link to the script reference
                //ushort srIndex = (ushort)(_currentIndex.Index + 3);
                //ushort srSalt = IndexToSalt(srIndex);
                //sleepCall.Next = new DatumIndex(srSalt, srIndex);
                //_currentIndex.Increment();
                //_expressions.Add(sleepCall);

                //// create the sleep_until name
                //var sleepName = new ScriptExpression(_currentIndex, sleepInfo.Opcode, _opcodes.GetTypeInfo("function_name").Opcode,
                //    ScriptExpressionType.Expression, _currentIndex.Next(), _strings.Cache("sleep_until"), (uint)0, 0);
                //_currentIndex.Increment();
                //_expressions.Add(sleepName);

                // Adjust the condition expression and add it.
                LinkDatum();
                ScriptExpression condition = branch.Value[0];
                condition.Index = _currentIndex;
                condition.Next = DatumIndex.Null;
                AddExpressionIncrement(condition);

                // Adjust the script reference and add it.
                LinkDatum();
                ScriptExpression scriptReference = branch.Value[1];
                scriptReference.Index = _currentIndex;
                scriptReference.Next = DatumIndex.Null;
                AddExpressionIncrement(scriptReference);
            }
        }

        private void CreateInitialBegin(string returnType)
        {
            FunctionInfo info = _opcodes.GetFunctionInfo("begin").First();

            // Create the begin call.
            var beginCall = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = _opcodes.GetTypeInfo(returnType).Opcode,
                Type = ScriptExpressionType.Group,
                Next = DatumIndex.Null,
                StringOffset = _randomAddress,
                LineNumber = 0
            };
            beginCall.SetValue(_currentIndex.Next);
            AddExpressionIncrement(beginCall);

            // Create the function name.
            var beginName = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = _opcodes.GetTypeInfo("function_name").Opcode,
                Type = ScriptExpressionType.Expression,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(info.Name),
                Value = 0,
                LineNumber = 0
            };
            OpenDatumAddExpressionIncrement(beginName);
        }

        private void CreateFunctionCall(FunctionInfo info, ushort returnTypeOpcode, short lineNumber)
        {
            var callExpression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = returnTypeOpcode,
                Type = ScriptExpressionType.Group,
                Next = DatumIndex.Null,
                StringOffset = _randomAddress,
                LineNumber = lineNumber
            };
            callExpression.SetValue(_currentIndex.Next);
            OpenDatumAddExpressionIncrement(callExpression);

            var nameExpression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = _opcodes.GetTypeInfo("function_name").Opcode,
                Type = ScriptExpressionType.Expression,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(info.Name),
                Value = 0,
                LineNumber = lineNumber
            };
            OpenDatumAddExpressionIncrement(nameExpression);
        }

        private void CreateScriptReference(ScriptInfo info, ushort returnTypeOpcode, short lineNumber)
        {
            var scriptReference = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = returnTypeOpcode,
                Type = ScriptExpressionType.ScriptReference,
                Next = DatumIndex.Null,
                StringOffset = _randomAddress,
                LineNumber = lineNumber
            };
            scriptReference.SetValue(_currentIndex.Next);
            OpenDatumAddExpressionIncrement(scriptReference);

            var nameExpression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = _opcodes.GetTypeInfo("function_name").Opcode,
                Type = ScriptExpressionType.Expression,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(info.Name),
                Value = 0,
                LineNumber = lineNumber
            };
            OpenDatumAddExpressionIncrement(nameExpression);
        }
    }
}
