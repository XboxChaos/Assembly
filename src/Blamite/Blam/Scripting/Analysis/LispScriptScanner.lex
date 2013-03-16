%namespace Blamite.Blam.Scripting.Analysis
%using System.Diagnostics;
%scannertype LispScriptScanner
%scanbasetype LispScriptScanBase
%tokentype LispScriptTokens
%option stack noparser unicode

eol (\n|\r\n?)
keyword [A-Za-z0-9!#_<>=\+\-\*\\]+
open \(
close \)
quote \"
digit [0-9]
float {digit}*\.?{digit}+
comment ;
comma ,
whitespace [ \t\r\n]

%x COMMENT
%%

{whitespace}*     /* ignore */
{open}            Output.WriteLine("(");
{close}           Output.WriteLine(")");
{comma}           Output.WriteLine("SEPARATOR");
true              Output.WriteLine("TRUE");
false             Output.WriteLine("FALSE");
global            Output.WriteLine("GLOBAL");
object            Output.WriteLine("OBJECT");
script            Output.WriteLine("SCRIPT");
{float}           Output.WriteLine("FLOAT {0}", yytext);
{quote}.*{quote}  Output.WriteLine("STRING \"{0}\"", yytext.Substring(1, yytext.Length - 2));
{comment}         yy_push_state(COMMENT);
{keyword}         Output.WriteLine("KEYWORD {0}", yytext);

<COMMENT> {
	{eol}  yy_pop_state();
}

%%
	public StreamWriter Output { get; set; }