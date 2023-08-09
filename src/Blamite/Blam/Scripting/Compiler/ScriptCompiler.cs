using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Blamite.Blam.Scripting.Context;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Blamite.Util;
using Blamite.Blam.Scripting;
using System.IO;
using Blamite.Serialization;
using System.Text.RegularExpressions;

namespace Blamite.Blam.Scripting.Compiler
{
    public partial class ScriptCompiler : HS_Gen1BaseListener
    {
        // debug
        private readonly ScriptCompilerLogger _logger;
        private readonly bool _debug;

        // lookups
        private readonly ICacheFile _cacheFile;
        private readonly EngineDescription _buildInfo;
        private readonly OpcodeLookup _opcodes;
        private readonly ScriptingContextCollection _scriptingContext;
        private Dictionary<string, List<ScriptInfo>> _scriptLookup = new Dictionary<string, List<ScriptInfo>>();
        private Dictionary<string, GlobalInfo> _mapGlobalsLookup = new Dictionary<string, GlobalInfo>();
        private readonly Dictionary<string, ParameterInfo> _parameterLookup = new Dictionary<string, ParameterInfo>();

        // script tables
        private readonly StringTable _strings = new StringTable();
        private readonly List<ScriptExpression> _expressions = new List<ScriptExpression>();
        private readonly List<Script> _scripts = new List<Script>();
        private readonly List<ScriptGlobal> _globals = new List<ScriptGlobal>();
        private readonly List<ITag> _references = new List<ITag>();

        // utility
        private readonly List<int> _missingCarriageReturnPositions = new List<int>();
        private const int _globalPushIndex = -1;
        private DatumIndex _currentIndex;
        private readonly Stack<int> _openDatums = new Stack<int>();
        private readonly TypeStack _expectedTypes;

        // branching
        private ushort _nextGenBranchIndex = 0;
        private ushort _branchBoolIndex = 0;
        private readonly List<Branch> _generatedBranches = new List<Branch>();

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


        public ScriptCompiler(ICacheFile casheFile, EngineDescription buildInfo, OpcodeLookup opCodes, ScriptingContextCollection context, IProgress<int> progress, ScriptCompilerLogger logger, bool debug)
        {
            _buildInfo = buildInfo;
            _progress = progress;
            _cacheFile = casheFile;
            _scriptingContext = context;
            _opcodes = opCodes;
            _logger = logger;
            _debug = debug;
            _expectedTypes = new TypeStack(logger, _debug);

            ushort intialSalt = SaltGenerator.GetSalt("script node");
            _currentIndex = new DatumIndex(intialSalt, 0);
        }

        public ScriptData Result()
        {
            return _result;
        }

        /// <summary>
        /// Generate script and global lookups.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterHsc(HS_Gen1Parser.HscContext context)
        {
            // Generate the globals lookup.
            if (context.globalDeclaration() != null)
            {
                _mapGlobalsLookup = context.globalDeclaration().Select((g, index) => new GlobalInfo(g, (ushort)index)).ToDictionary(g => g.Name);

            }

            // Generate the script lookup and determine the index, which the next generated branch script will have.
            var declarations = context.scriptDeclaration();
            if (declarations != null)
            {
                _nextGenBranchIndex = (ushort)declarations.Length;
                _scriptLookup = new Dictionary<string, List<ScriptInfo>>();

                for(ushort i = 0; i < context.scriptDeclaration().Length; i++)
                {
                    var info = new ScriptInfo(declarations[i], i);
                    AddScriptToLookup(info);
                }
            }

            // The declaration count is used to calculate the current progress.
            _declarationCount = context.scriptDeclaration().Length + context.globalDeclaration().Length;

            // Find all missing carriage returns. Somehow antlr removes them under certain circumstances and messes up the text position indeces.
            string text = context.Start.InputStream.GetText(new Interval(0, context.Start.InputStream.Size));
            string otherText = context.GetText();
            bool same = text == otherText;
            var missingCarriageReturns = Regex.Matches(text, @"(?<!\r)\n");
            foreach (Match match in missingCarriageReturns)
            {
                _missingCarriageReturnPositions.Add(match.Index);
            }
        }

        /// <summary>
        /// Output Debug Info
        /// </summary>
        /// <param name="context"></param>
        public override void ExitHsc(HS_Gen1Parser.HscContext context)
        {
            GenerateBranches();
            if(_debug)
            {
                DeclarationsToXML();
                ExpressionsToXML();
                StringsToFile();
            }
            _result = new ScriptData(_scripts, _globals, _references, _expressions, _strings);
        }

        /// <summary>
        /// Processes script declarations. Opens a datum. 
        /// Creates the script node and the initial "begin" expression.
        /// Generates the variable lookup. Pushes return types.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterScriptDeclaration(HS_Gen1Parser.ScriptDeclarationContext context)
        {
            if(_debug)
            {
                _logger.Script(context, CompilerContextAction.Enter);
            }

            // Create new script object and add it to the table.
            Script script = ScriptObjectCreation.GetScriptFromContext(context, _currentIndex, _opcodes);
            _scripts.Add(script);

            // Generate the parameter lookup.
            for(ushort i = 0; i < script.Parameters.Count; i++)
            {
                ScriptParameter parameter = script.Parameters[i];
                var info = new ParameterInfo(parameter.Name, _opcodes.GetTypeInfo(parameter.Type).Name, i);
                _parameterLookup.Add(info.Name, info);
            }

            string returnType = context.VALUETYPE().GetTextSanitized();
            int expressionCount = context.expression().Count();

            // The final expression must match the return type of this script.
            _expectedTypes.PushType(returnType);
            // The other expressions can be of any type.
            if (expressionCount > 1)
            {
                _expectedTypes.PushTypes("void", expressionCount - 1);
            }

            CreateInitialBegin(returnType, context.GetCorrectTextPosition(_missingCarriageReturnPositions));
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitScriptDeclaration(HS_Gen1Parser.ScriptDeclarationContext context)
        {
            if (_debug)
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
        public override void EnterGlobalDeclaration(HS_Gen1Parser.GlobalDeclarationContext context)
        {
            if (_debug)
            {
                _logger.Global(context, CompilerContextAction.Enter);
            }

            // Create a new Global and add it to the table.
            string valueType = context.VALUETYPE().GetTextSanitized();
            ScriptGlobal glo = new ScriptGlobal()
            {
                Name = context.ID().GetTextSanitized(),
                Type = (short)_opcodes.GetTypeInfo(valueType).Opcode,
                ExpressionIndex = _currentIndex,
            };

            _globals.Add(glo);

            if (_debug)
            {
                _logger.Information($"A global declaration was detected by the parser. The index {_globalPushIndex} will be opened.");
            }
            OpenDatum(_globalPushIndex);
            _expectedTypes.PushType(valueType);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitGlobalDeclaration(HS_Gen1Parser.GlobalDeclarationContext context)
        {
            if (_debug)
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
        public override void EnterCall(HS_Gen1Parser.CallContext context)
        {
            if (_debug)
            {
                _logger.Call(context, CompilerContextAction.Enter);
            }

            LinkDatum();

            // Retrieve information from the context.
            string name = context.callID().GetTextSanitized();
            string expectedType = _expectedTypes.PopType();
            int contextParameterCount = context.expression().Length;

            // Handle script references.
            if (IsScriptReference(expectedType, contextParameterCount, context))
            {
                return;
            }

            // Handle functions.
            var functions = _opcodes.GetFunctionInfo(name);

            if (functions == null)
                throw new CompilerException($"Tried to find opcode named '{name}' but could not. It may actually be a culled script if this is an unmodified map.", context);

            foreach (var func in functions)
            {
                if(!CheckParameterSanity(context, func))
                {
                    continue;
                }

                string returnType = DetermineReturnType(func, expectedType, context);

                // If a function, which satisfies the requirements, was found, perform the necessary push operations and create expression nodes.
                if (!(returnType is null))
                {
                    EqualityPush(func.ReturnType);
                    PushCallParameters(func, contextParameterCount, expectedType, context);
                    CreateFunctionCall(func, _opcodes.GetTypeInfo(returnType).Opcode, context.GetCorrectTextPosition(_missingCarriageReturnPositions), GetLineNumber(context));
                    return;
                }
            }

            string errorMessage = contextParameterCount == 1 ?
                    $"Failed to process the call {name} with 1 parameter." :
                    $"Failed to process the call {name} with {contextParameterCount} parameters.";
            throw new CompilerException(errorMessage + $" The expected return type was {expectedType}.", context);
        }

        /// <summary>
        /// Closes a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitCall(HS_Gen1Parser.CallContext context)
        {
            if (_debug)
            {
                _logger.Call(context, CompilerContextAction.Exit);
            }

            CloseDatum();
        }

        public override void EnterBranch(HS_Gen1Parser.BranchContext context)
        {
            if (_debug)
            {
                _logger.Branch(context, CompilerContextAction.Enter);
            }

            LinkDatum();
            _ = _expectedTypes.PopType();    // Just ignore the type for now.

            // Branch always has two parameters.
            if (context.expression().Count() != 2)
            {
                throw new CompilerException($"\"Branch\" accepts two arguments. The compiler found {context.expression().Count() }.", context);
            }

            _expectedTypes.PushTypes("boolean", TypeHelper.ScriptReference);
            FunctionInfo info = _opcodes.GetFunctionInfo("branch").First();
            CreateFunctionCall(info, _opcodes.GetTypeInfo("void").Opcode, context.GetCorrectTextPosition(_missingCarriageReturnPositions), GetLineNumber(context));

            _branchBoolIndex = _currentIndex.Index;
        }

        public override void ExitBranch(HS_Gen1Parser.BranchContext context)
        {
            if (_debug)
            {
                _logger.Branch(context, CompilerContextAction.Exit);
            }

            // Generate the script name.
            var scriptContext = GetParentContext(context, HS_Gen1Parser.RULE_scriptDeclaration) as HS_Gen1Parser.ScriptDeclarationContext;
            if(scriptContext is null)
            {
                throw new CompilerException("The compiler failed to retrieve the name of a script, from which \"branch\" was called.", context);
            }
            string fromScript = scriptContext.scriptID().GetTextSanitized();

            var parameters = context.expression();
            var callParameter = parameters[1].call();
            if (callParameter is null)
            {
                throw new CompilerException("A branch call's second argument must be a script call.", context);
            }
            string toScript = callParameter.callID().GetTextSanitized();
            string generatedName = fromScript + "_to_" + toScript;
            var condition = _expressions[_branchBoolIndex].Clone();
            var scriptReference = _expressions[condition.Next.Index].Clone();

            // Modify the original script reference. The opcode points to the generated script.
            _expressions[condition.Next.Index].Opcode = _nextGenBranchIndex;

            // Add the generated script to the lookup.
            ScriptInfo info = new ScriptInfo(generatedName, "static", "void", _nextGenBranchIndex);
            AddScriptToLookup(info);
            _nextGenBranchIndex++;
            _generatedBranches.Add(new Branch(generatedName, context.GetCorrectTextPosition(_missingCarriageReturnPositions), condition, scriptReference));

            CloseDatum();
        }

       

        public override void EnterCond(HS_Gen1Parser.CondContext context)
        {
            if (_debug)
            {
                _logger.Cond(context, CompilerContextAction.Enter);
            }

            // Tell the groups what type to expect.
            _condReturnType = _expectedTypes.PopType();

            // Push the index to the first group, so that we can open it later on.
            _condIndeces.Push(_expressions.Count);
        }

        public override void ExitCond(HS_Gen1Parser.CondContext context)
        {
            if (_debug)
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
                StringOffset = context.GetCorrectTextPosition(_missingCarriageReturnPositions),
                Value = new LongExpressionValue(0),
                LineNumber = 0
            };
            AddExpressionIncrement(expression);

            // Open the first group.
            int firstGroupIndex = _condIndeces.Pop();
            OpenDatum(firstGroupIndex);
        }

        public override void EnterCondGroup(HS_Gen1Parser.CondGroupContext context)
        {
            if (_debug)
            {
                _logger.CondGroup(context, CompilerContextAction.Enter);
            }

            // Link to the previous expression or cond group.
            LinkDatum();

            // push the types of the group members.
            _expectedTypes.PushTypes(_condReturnType, context.expression().Count() - 1);
            _expectedTypes.PushType("boolean");

            var parentCond = context.Parent as HS_Gen1Parser.CondContext;
            var ifInfo = _opcodes.GetFunctionInfo("if")[0];
            var compilerIf = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = ifInfo.Opcode,
                ReturnType = _opcodes.GetTypeInfo(_condReturnType).Opcode,
                Type = ScriptExpressionType.Group,
                Next = DatumIndex.Null,
                StringOffset = context.GetCorrectTextPosition(_missingCarriageReturnPositions),
                Value = new LongExpressionValue(_currentIndex.Next),
                LineNumber = 0
            };

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
                Value = new LongExpressionValue(0),
                LineNumber = GetLineNumber(context)
            };
            OpenDatumAddExpressionIncrement(compilerIfName);

            // Push the index to the condition so that we can modify it later on.
            _condIndeces.Push(_expressions.Count);
        }

        public override void ExitCondGroup(HS_Gen1Parser.CondGroupContext context)
        {
            if (_debug)
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
                StringOffset = context.GetCorrectTextPosition(_missingCarriageReturnPositions),
                Value = new LongExpressionValue(_currentIndex.Next),
                LineNumber = 0
            };
            OpenDatumAddExpressionIncrement(compilerBeginCall);

            var compilerBeginName = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = beginInfo.Opcode,
                ReturnType = _opcodes.GetTypeInfo("function_name").Opcode,
                Type = ScriptExpressionType.Expression,
                Next = valueExpressionDatum,
                StringOffset = _strings.Cache(beginInfo.Name),
                Value = new LongExpressionValue(0),
                LineNumber = 0
            };
            AddExpressionIncrement(compilerBeginName);
        }

        /// <summary>
        /// Processes regular expressions, script variables and global references. Links to a datum. Opens a datum.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterLiteral(HS_Gen1Parser.LiteralContext context)
        {
            if (_debug)
            {
                _logger.Literal(context, CompilerContextAction.Enter);
            }

            LinkDatum();

            string text = context.GetTextSanitized();
            string expectedType = _expectedTypes.PopType();

            // Handle "none" expressions. The Value field of ai_line expressions stores their string offset.
            // Therefore the Value is not -1 if the expression is "none".
            if(IsNone(text) && expectedType != "ai_line" && expectedType != "string")
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
                    Value = new LongExpressionValue(0xFFFFFFFF),
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

            throw new CompilerException($"Failed to process the expression \"{text.Trim('"')}\". A \"{expectedType}\" expression was expected.", context);
        }

        private bool IsGlobalsReference(string expectedType, ParserRuleContext context)
        {
            string text = context.GetTextSanitized();
            GlobalInfo globalInfo = _opcodes.GetGlobalInfo(text);
            IExpressionValue value;

            // Engine global.
            if(globalInfo != null)
            {
                //ushort[] arr = { 0xFFFF, globalInfo.MaskedOpcode };
                value = new LongExpressionValue(0xFFFF, globalInfo.MaskedOpcode);
            }
            // Map global.
            else if(_mapGlobalsLookup.ContainsKey(text))
            {
                globalInfo = _mapGlobalsLookup[text];
                value = new LongExpressionValue(globalInfo.Opcode);
            }
            // Not a global...
            else if (expectedType == TypeHelper.GlobalsReference)
            {
                throw new CompilerException($"A global reference was expected, but no global with the name \"{text}\" could be found.", context);
            }
            else
            {
                return false;
            }

            string returnType = DetermineReturnType(globalInfo, expectedType, context);
            ushort returnTypeOpcode = _opcodes.GetTypeInfo(returnType).Opcode;
            ushort opcode = GetGlobalOpCode(returnTypeOpcode, context);
            var expression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = opcode,
                ReturnType = returnTypeOpcode,
                Type = ScriptExpressionType.GlobalsReference,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(text),
                Value = value,
                LineNumber = GetLineNumber(context)
            };
            OpenDatumAddExpressionIncrement(expression);

            return true;
        }

        private ushort GetGlobalOpCode(ushort globalOpcode, RuleContext context)
        {
            RuleContext grandparent = GetParentContext(context, HS_Gen1Parser.RULE_call);
            ushort opcode = globalOpcode;

            // "set" and (in)equality functions are special.
            if (grandparent is HS_Gen1Parser.CallContext call)
            {
                string funcName = call.callID().GetTextSanitized();
                List<FunctionInfo> funcInfo = _opcodes.GetFunctionInfo(funcName);

                if (funcInfo != null)
                {
                    if (funcInfo[0].Group == "Equality" || funcInfo[0].Group == "Inequality")
                    {
                        opcode = GetEqualityArgumentOP(globalOpcode);
                    }
                    else if (_set && funcInfo[0].Group == "Set")
                    {
                        opcode = 0xFFFF;

                        if (_debug)
                        {
                            _logger.Information("The Global belongs to a set call. It's value type will be pushed to the stack.");
                        }
                        // The next parameter must have the same return type as this global
                        _expectedTypes.PushType(_opcodes.GetTypeInfo(globalOpcode).Name);
                        _set = false;
                    }
                }
            }
            EqualityPush(_opcodes.GetTypeInfo(globalOpcode).Name);

            return opcode;
        }

        private ushort GetEqualityArgumentOP(ushort normalOpcode)
        {
            // The opccode of global references and parameter references as part of equality checks is 0, unless the first argument of the comparison is a call.
            // Find the last generated equality function name.
            for(int i = _expressions.Count -1; i >= 0; i--)
            {
                if(_expressions[i].Type == ScriptExpressionType.Expression 
                    && _expressions[i].ReturnType == _opcodes.GetTypeInfo("function_name").Opcode
                    && _expressions[i - 1].Type == ScriptExpressionType.Group)
                {
                    var funcInfo = _opcodes.GetFunctionInfo(_expressions[i].Opcode);
                    if (funcInfo.Group == "Equality" || funcInfo.Group == "Inequality")
                    {
                        if(i == _expressions.Count - 1)
                        {
                            return 0;
                        }                     
                        else if(_expressions[i + 1].Type != ScriptExpressionType.Expression)
                        {
                            return normalOpcode;
                        }

                        return 0;
                    }
                }
            }

            return 0;
        }

        private string DetermineReturnType(IScriptingConstantInfo info, string expectedType, ParserRuleContext context)
        {
            string calculatedType = expectedType switch
            {
                "ANY" when info.ReturnType == "passthrough" => "void",
                "ANY" => info.ReturnType,
                // Cast globals in arithmetic functions to real.
                "NUMBER" when info is GlobalInfo && TypeHelper.IsNumType(info.ReturnType) => "real",
                "NUMBER" when TypeHelper.IsNumType(info.ReturnType) => info.ReturnType,
                "NUMBER" when !TypeHelper.IsNumType(info.ReturnType) => "",
                "void" => "void",
                "GLOBALREFERENCE" when info is GlobalInfo => info.ReturnType,
                "SCRIPTREFERENCE" when info is ScriptInfo => info.ReturnType,
                _ when expectedType == info.ReturnType => expectedType,
                _ when info.ReturnType == "passthrough" => expectedType,
                _ when TypeHelper.CanBeCasted(info.ReturnType, expectedType, _opcodes) => expectedType,
                _ => null
            };

            return calculatedType;
        }

        private bool IsScriptReference(string expectedReturnType, int expectedParameterCount, HS_Gen1Parser.CallContext context)
        {
            string key = context.callID().GetTextSanitized();

            if(!_scriptLookup.ContainsKey(key))
            {
                if (expectedReturnType == TypeHelper.ScriptReference)
                {
                    throw new CompilerException($"The compiler expected a Script Reference but a Script with the name \"{key}\" could not be found. " +
                        "Please check your script declarations and your spelling.", context);
                }
                else
                {
                    return false;
                }
            }

            // Try to find a script, which satisfies all conditions.
            foreach(ScriptInfo info in _scriptLookup[key])
            {
                if(info.Parameters.Count != expectedParameterCount)
                {
                    continue;
                }

                string returnType = DetermineReturnType(info, expectedReturnType, context);

                if(!(returnType is null))
                {
                    // Check for equality functions.
                    EqualityPush(returnType);

                    // Push the parameters to the type stack.
                    if (expectedParameterCount > 0)
                    {
                        string[] parameterTypes = info.Parameters.Select(p => p.ReturnType).ToArray();
                        _expectedTypes.PushTypes(parameterTypes);
                    }

                    // Create a script reference node.
                    CreateScriptReference(info, _opcodes.GetTypeInfo(returnType).Opcode, context.GetCorrectTextPosition(_missingCarriageReturnPositions), GetLineNumber(context));
                    return true;
                }
            }

            return false;
        }

        private bool IsScriptParameter(string expectedReturnType, ParserRuleContext context)
        {
            // This script doesn't have parameters.
            if (_parameterLookup.Count == 0)
            {
                return false;
            }

            string text = context.GetTextSanitized();

            // The script doesn't have a parameter with this name.
            if (!_parameterLookup.TryGetValue(text, out ParameterInfo info))
            {
                return false;
            }

            string returnType = DetermineReturnType(info, expectedReturnType, context);

            if(returnType is null)
            {
                throw new CompilerException($"Failed to determine the return type of the parameter {text}.", context);
            }

            ushort returnTypeOpcode = _opcodes.GetTypeInfo(returnType).Opcode;
            ushort opcode = returnTypeOpcode;

            // (In)Equality functions are special
            if(context.Parent.Parent is HS_Gen1Parser.CallContext grandparent)
            {
                string funcName = grandparent.callID().GetTextSanitized();
                var funcInfo = _opcodes.GetFunctionInfo(funcName);
                if(funcInfo != null)
                {
                    if (funcInfo[0].Group == "Equality" || funcInfo[0].Group == "Inequality")
                    {
                        opcode = GetEqualityArgumentOP(returnTypeOpcode);
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
                Value = new LongExpressionValue(info.Opcode),
                LineNumber = GetLineNumber(context)
            };
            EqualityPush(info.ReturnType);
            OpenDatumAddExpressionIncrement(expression);

            return true;
        }

        /// <summary>
        /// Calculates the value types for the parameters of a function and pushes them to the stack.
        /// </summary>
        /// <param name="info">The function's ScriptFunctionInfo</param>
        /// <param name="context">The call contect</param>
        /// <param name="actualParameterCount">The number of parameters which was extracted from the context</param>
        private void PushCallParameters(FunctionInfo info, int contextParameterCount, string expectedReturnType, HS_Gen1Parser.CallContext context)
        {
            int expectedParamCount = info.ParameterTypes.Count();

            // Handle regular functions.
            if (expectedParamCount > 0)
            {
                if (contextParameterCount != expectedParamCount)
                {
                    throw new CompilerException($"The function \"{context.callID().GetTextSanitized()}\" has an unexpected number of arguments. Expected: \"{expectedParamCount}\" Encountered: \"{contextParameterCount}\".", context);

                }
                _expectedTypes.PushTypes(info.ParameterTypes);
            }
            // TODO: Throw exceptions if a wrong number of parameters is detected.
            // Handle special functions.
            #region special functions
            else
            {
                bool validNumberOfArgs = false;

                switch (info.Group)
                {
                    // Any number of arguments.
                    case "Begin":
                        validNumberOfArgs = true;
                        if (info.Name.Contains("random"))
                        {
                            _expectedTypes.PushTypes(expectedReturnType, contextParameterCount);

                        }
                        else
                        {
                            // the last evaluated expression.
                            _expectedTypes.PushType(expectedReturnType);
                            _expectedTypes.PushTypes("void", contextParameterCount - 1);
                        }
                        break;

                    // Any number of arguments.
                    case "BeginCount":
                        validNumberOfArgs = true;
                        _expectedTypes.PushTypes(expectedReturnType, contextParameterCount - 1);
                        _expectedTypes.PushType("long");
                        break;

                    // 2 or 3 arguments?
                    case "If":
                        validNumberOfArgs = contextParameterCount == 2 || contextParameterCount == 3;
                        if(expectedReturnType == TypeHelper.Any)
                        {
                            _expectedTypes.PushTypes("void", contextParameterCount - 1);
                        }
                        else
                        {
                            _expectedTypes.PushTypes(expectedReturnType, contextParameterCount - 1);
                        }
                        _expectedTypes.PushType("boolean");
                        break;

                    // Cond has it's own parser rule and should be handled elsewhere.
                    case "Cond":
                        throw new CompilerException("A cond call was not recognized by the parser.", context);

                    // Two arguments.
                    case "Set":
                        validNumberOfArgs = contextParameterCount == 2;
                        _expectedTypes.PushType(TypeHelper.GlobalsReference);
                        // The second parameter will be pushed once we have determined the return type of the global.
                        _set = true;
                        break;

                    // Any number of arguments.
                    case "Logical":
                        validNumberOfArgs = contextParameterCount >= 1;
                        _expectedTypes.PushTypes("boolean", contextParameterCount);
                        break;

                    // Depends on the function. Some accept only two arguments.
                    case "Arithmetic":
                        validNumberOfArgs = contextParameterCount >= 1;
                        _expectedTypes.PushTypes("real", contextParameterCount);
                        break;

                    // TODO: Change inequality to only accept NUMBER or maybe real arguments?
                    // Two arguments.
                    case "Equality":
                    case "Inequality":
                        validNumberOfArgs = contextParameterCount == 2;
                        _expectedTypes.PushType("ANY");
                        _equality = true;
                        break;
                    
                    // One or two arguments.
                    case "Sleep":
                        validNumberOfArgs = contextParameterCount == 1 || contextParameterCount == 2;
                        if (contextParameterCount == 2)
                        {
                            _expectedTypes.PushType("script");
                        }
                        _expectedTypes.PushType("short");
                        break;
                    
                    // Zero or one argument(s).
                    case "SleepForever":
                        validNumberOfArgs = contextParameterCount == 0 || contextParameterCount == 1;
                        if (contextParameterCount == 1)
                        {
                            _expectedTypes.PushType("script");
                        }
                        break;

                    // One, two or three arguments.
                    case "SleepUntil":
                        validNumberOfArgs = contextParameterCount >= 1 || contextParameterCount <= 3;
                        if (contextParameterCount == 3)
                        {
                            _expectedTypes.PushTypes("short", "long");
                        }
                        else if(contextParameterCount == 2)
                        {
                            _expectedTypes.PushType("short");
                        }
                        _expectedTypes.PushType("boolean");
                        break;

                    // Probably two arguments.
                    case "SleepUntilGameTicks":
                        validNumberOfArgs = contextParameterCount == 2;
                        if (contextParameterCount != 2)
                        {
                            throw new CompilerException("The Compiler encountered a \"sleep_until_game_ticks\" call, which didn't have exactly two arguments.", context);
                        }
                        _expectedTypes.PushTypes("boolean", "short");
                        break;

                    // Probably one argument.
                    case "CinematicSleep":
                        validNumberOfArgs = contextParameterCount == 1;
                        if (contextParameterCount != 1)
                        {
                            throw new CompilerException("The Compiler encountered a \"sleep_until_game_ticks\" call, which didn't have exactly one argument.", context);
                        }
                        _expectedTypes.PushType("short");
                        break;

                    // One argument.
                    case "Wake":
                        validNumberOfArgs = contextParameterCount == 1;
                        _expectedTypes.PushType("script");
                        break;

                    // One argument.
                    case "Inspect":
                        validNumberOfArgs = contextParameterCount == 1;
                        _expectedTypes.PushType("ANY");
                        break;

                    // Branch has it's own parser rule and should be handled elsewhere.
                    case "Branch":
                        throw new CompilerException("A branch call was not identified by the parser.", context);
                    
                    // One argument.
                    case "ObjectCast":
                        validNumberOfArgs = contextParameterCount == 1;
                        _expectedTypes.PushType("object");
                        break;

                    // What is this?
                    case null:
                        return;

                    default:
                        throw new CompilerException($"Unimplemented function group: \"{info.Group}\".", context);
                }
                if(!validNumberOfArgs)
                {
                    throw new CompilerException($"The special function \"{info.Name}\" has an invalid number of arguments: \"{contextParameterCount}\".", context);
                }
            }
            #endregion
        }

        private void GenerateBranches()
        {
            foreach(var branch in _generatedBranches)
            {
                if(_debug)
                {
                    _logger.Information($"Generating Branch: {branch.BranchName}");
                }

                ushort voidOpcode = _opcodes.GetTypeInfo("void").Opcode;

                // create script entry
                Script script = new Script
                {
                    Name = branch.BranchName,
                    ReturnType = (short)voidOpcode,
                    ExecutionType = (short)_opcodes.GetScriptTypeOpcode("static"),
                    RootExpressionIndex = _currentIndex,
                };
                _scripts.Add(script);

                CreateInitialBegin("void", branch.TextPosition);
                LinkDatum();

                // create the sleep_until call
                FunctionInfo sleepInfo = _opcodes.GetFunctionInfo("sleep_until").First();
                CreateFunctionCall(sleepInfo, voidOpcode, branch.TextPosition, 0);

                // Adjust the condition expression and add it.
                LinkDatum();
                ScriptExpression condition = branch.Condition;
                condition.Index = _currentIndex;
                condition.Next = DatumIndex.Null;
                AddExpressionIncrement(condition);

                // Adjust the script reference and add it.
                LinkDatum();
                ScriptExpression scriptReference = branch.TargetScript;
                scriptReference.Index = _currentIndex;
                scriptReference.Next = DatumIndex.Null;
                AddExpressionIncrement(scriptReference);
            }
        }

        private void CreateInitialBegin(string returnType, uint textPosition)
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
                StringOffset = textPosition,
                Value = new LongExpressionValue(_currentIndex.Next),
                LineNumber = 0
            };
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
                Value = new LongExpressionValue(0),
                LineNumber = 0
            };
            OpenDatumAddExpressionIncrement(beginName);
        }

        private void CreateFunctionCall(FunctionInfo info, ushort returnTypeOpcode, uint textPosition, short lineNumber)
        {
            var callExpression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = returnTypeOpcode,
                Type = ScriptExpressionType.Group,
                Next = DatumIndex.Null,
                StringOffset = textPosition,
                Value = new LongExpressionValue(_currentIndex.Next),
                LineNumber = lineNumber
            };
            OpenDatumAddExpressionIncrement(callExpression);

            var nameExpression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = _opcodes.GetTypeInfo("function_name").Opcode,
                Type = ScriptExpressionType.Expression,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(info.Name),
                Value = new LongExpressionValue(0),
                LineNumber = lineNumber
            };
            OpenDatumAddExpressionIncrement(nameExpression);
        }

        private void CreateScriptReference(ScriptInfo info, ushort returnTypeOpcode, uint textPosition, short lineNumber)
        {
            var scriptReference = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = returnTypeOpcode,
                Type = ScriptExpressionType.ScriptReference,
                Next = DatumIndex.Null,
                StringOffset = textPosition,
                Value = new LongExpressionValue(_currentIndex.Next),
                LineNumber = lineNumber
            };
            OpenDatumAddExpressionIncrement(scriptReference);

            var nameExpression = new ScriptExpression
            {
                Index = _currentIndex,
                Opcode = info.Opcode,
                ReturnType = _opcodes.GetTypeInfo("function_name").Opcode,
                Type = ScriptExpressionType.Expression,
                Next = DatumIndex.Null,
                StringOffset = _strings.Cache(info.Name),
                Value = new LongExpressionValue(0),
                LineNumber = lineNumber
            };
            OpenDatumAddExpressionIncrement(nameExpression);
        }
    }
}
