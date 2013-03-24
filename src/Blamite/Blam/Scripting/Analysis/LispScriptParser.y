%namespace Blamite.Blam.Scripting.Analysis
%using System.Diagnostics;
%parsertype LispScriptParser
%scanbasetype LispScriptScanBase
%tokentype LispScriptTokens

%union {
    public float FloatValue;
    public string StringValue;
    public bool BooleanValue;
    public IScriptNode Node;
    public List<IScriptNode> NodeList;
    public ScriptDefinitionParam Parameter;
    public List<ScriptDefinitionParam> ParamList;
}

%start declarations

%token GLOBAL, SCRIPT
%token <StringValue> NAME
%token <FloatValue> FLOAT
%token <StringValue> STRING
%token <BooleanValue> BOOLEAN
%token NONE

%type <StringValue> scriptname
%type <Node> declaration, globaldecl, scriptdecl
%type <Node> expression, constant, functioncall, variable
%type <NodeList> expressions
%type <Parameter> parameter
%type <ParamList> parameters

%%

declarations : /* empty */
             | declarations declaration
                 { Nodes.Add($2); }
             ;

declaration : globaldecl
            | scriptdecl
            ;

globaldecl : '(' GLOBAL NAME NAME expression ')'
               { $$ = new GlobalDefinitionNode($4, $3, $5); }
           ;

scriptname : NAME
           | FLOAT
               /* WHY IS THIS LEGAL WHY WHY WHY */
               { $$ = $1.ToString(); }
           ;

scriptdecl : '(' SCRIPT NAME NAME scriptname expressions ')'
               {
                   var def = new ScriptDefinitionNode($5, $3, $4);
                   def.Nodes.AddRange($6);
                   $$ = def;
               }
           | '(' SCRIPT NAME NAME '(' scriptname '(' parameters ')' ')' expressions ')'
               {
                   var def = new ScriptDefinitionNode($6, $3, $4);
                   def.Parameters.AddRange($8);
                   def.Nodes.AddRange($11);
                   $$ = def;
               }
           ;

parameters : parameter
               { $$ = new List<ScriptDefinitionParam>(); $$.Add($1); }
           | parameters ',' parameter
               { $$ = $1; $$.Add($3); }
           ;

parameter : NAME NAME
              { $$ = new ScriptDefinitionParam($2, $1); }
          ;

expressions : expression
                { $$ = new List<IScriptNode>(); $$.Add($1); }
            | expressions expression
                { $$ = $1; $$.Add($2); }
            ;

expression : constant
           | functioncall
           | variable
           ;

constant : FLOAT
             { $$ = new ConstantNode($1); }
         | STRING
             { $$ = new ConstantNode($1); }
         | BOOLEAN
             { $$ = new ConstantNode($1); }
         | NONE
             { $$ = new ConstantNode(); }
         ;

functioncall : '(' scriptname ')'
                 { $$ = new FunctionCallNode($2); }
             | '(' scriptname expressions ')'
                 {
                     var call = new FunctionCallNode($2);
                     call.Arguments.AddRange($3);
                     $$ = call;
                 }
             ;

variable : NAME
             { $$ = new VariableReferenceNode($1); }
         ;

%%

public LispScriptParser(LispScriptScanner sc)
	: base(sc)
{
    Nodes = new List<IScriptNode>();
}

public List<IScriptNode> Nodes { get; private set; }