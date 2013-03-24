%namespace Blamite.Blam.Scripting.Analysis
%using System.Diagnostics;
%scannertype LispScriptScanner
%scanbasetype LispScriptScanBase
%tokentype LispScriptTokens
%option stack unicode

eol (\n|\r\n?)
name [A-Za-z0-9!#_<>=\+\-\*\\]+
open \(
close \)
quote \"
digit [0-9]
float \-?{digit}*\.?{digit}+
comment ;
comma ,
whitespace [ \t\r\n]

%x COMMENT
%x AFTEROPEN

%%

<AFTEROPEN> {
    global  BEGIN(INITIAL); return (int)LispScriptTokens.GLOBAL;
    script  BEGIN(INITIAL); return (int)LispScriptTokens.SCRIPT;
}

<INITIAL,AFTEROPEN> {
    {whitespace}*        /* ignore */
    {open}               BEGIN(AFTEROPEN); return (int)'(';
    {close}              BEGIN(INITIAL); return (int)')';
    {comma}              BEGIN(INITIAL); return (int)',';
    true                 BEGIN(INITIAL); yylval.BooleanValue = true; return (int)LispScriptTokens.BOOLEAN;
    false                BEGIN(INITIAL); yylval.BooleanValue = false; return (int)LispScriptTokens.BOOLEAN;
    none                 BEGIN(INITIAL); return (int)LispScriptTokens.NONE;
    {float}              BEGIN(INITIAL); yylval.FloatValue = float.Parse(yytext); return (int)LispScriptTokens.FLOAT;
    {quote}[^"]*{quote}  BEGIN(INITIAL); yylval.StringValue = yytext.Substring(1, yytext.Length - 2); return (int)LispScriptTokens.STRING;
    {comment}            BEGIN(INITIAL); yy_push_state(COMMENT);
    {name}               BEGIN(INITIAL); yylval.StringValue = yytext; return (int)LispScriptTokens.NAME;
}

<COMMENT> {
	{eol}  yy_pop_state();
}

%%
