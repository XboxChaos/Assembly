// Generated from c:\Users\Johann\Source\Repos\SnipeStyle\Assembly\src\Blamite\Blam\Scripting\ANTLR\BS_Reach.g4 by ANTLR 4.7.1
import org.antlr.v4.runtime.atn.*;
import org.antlr.v4.runtime.dfa.DFA;
import org.antlr.v4.runtime.*;
import org.antlr.v4.runtime.misc.*;
import org.antlr.v4.runtime.tree.*;
import java.util.List;
import java.util.Iterator;
import java.util.ArrayList;

@SuppressWarnings({"all", "warnings", "unchecked", "unused", "cast"})
public class BS_ReachParser extends Parser {
	static { RuntimeMetaData.checkVersion("4.7.1", RuntimeMetaData.VERSION); }

	protected static final DFA[] _decisionToDFA;
	protected static final PredictionContextCache _sharedContextCache =
		new PredictionContextCache();
	public static final int
		T__0=1, T__1=2, T__2=3, T__3=4, T__4=5, T__5=6, T__6=7, T__7=8, T__8=9, 
		T__9=10, T__10=11, T__11=12, T__12=13, T__13=14, T__14=15, T__15=16, T__16=17, 
		T__17=18, T__18=19, BOOLIT=20, VALUETYPE=21, DAMAGEREGION=22, MODELSTATE=23, 
		ID=24, STRING=25, FLOAT=26, INT=27, LP=28, RP=29, COMMENT=30, WS=31;
	public static final int
		RULE_hsc = 0, RULE_gloDecl = 1, RULE_scriDecl = 2, RULE_scriptParams = 3, 
		RULE_call = 4, RULE_funcID = 5, RULE_retType = 6, RULE_expr = 7, RULE_scriptType = 8;
	public static final String[] ruleNames = {
		"hsc", "gloDecl", "scriDecl", "scriptParams", "call", "funcID", "retType", 
		"expr", "scriptType"
	};

	private static final String[] _LITERAL_NAMES = {
		null, "'global'", "'script'", "','", "'!='", "'>='", "'<='", "'*'", "'+'", 
		"'<'", "'-'", "'='", "'>'", "'void'", "'startup'", "'dormant'", "'continuous'", 
		"'static'", "'command_script'", "'stub'", null, null, null, null, null, 
		null, null, null, "'('", "')'"
	};
	private static final String[] _SYMBOLIC_NAMES = {
		null, null, null, null, null, null, null, null, null, null, null, null, 
		null, null, null, null, null, null, null, null, "BOOLIT", "VALUETYPE", 
		"DAMAGEREGION", "MODELSTATE", "ID", "STRING", "FLOAT", "INT", "LP", "RP", 
		"COMMENT", "WS"
	};
	public static final Vocabulary VOCABULARY = new VocabularyImpl(_LITERAL_NAMES, _SYMBOLIC_NAMES);

	/**
	 * @deprecated Use {@link #VOCABULARY} instead.
	 */
	@Deprecated
	public static final String[] tokenNames;
	static {
		tokenNames = new String[_SYMBOLIC_NAMES.length];
		for (int i = 0; i < tokenNames.length; i++) {
			tokenNames[i] = VOCABULARY.getLiteralName(i);
			if (tokenNames[i] == null) {
				tokenNames[i] = VOCABULARY.getSymbolicName(i);
			}

			if (tokenNames[i] == null) {
				tokenNames[i] = "<INVALID>";
			}
		}
	}

	@Override
	@Deprecated
	public String[] getTokenNames() {
		return tokenNames;
	}

	@Override

	public Vocabulary getVocabulary() {
		return VOCABULARY;
	}

	@Override
	public String getGrammarFileName() { return "BS_Reach.g4"; }

	@Override
	public String[] getRuleNames() { return ruleNames; }

	@Override
	public String getSerializedATN() { return _serializedATN; }

	@Override
	public ATN getATN() { return _ATN; }

	public BS_ReachParser(TokenStream input) {
		super(input);
		_interp = new ParserATNSimulator(this,_ATN,_decisionToDFA,_sharedContextCache);
	}
	public static class HscContext extends ParserRuleContext {
		public List<GloDeclContext> gloDecl() {
			return getRuleContexts(GloDeclContext.class);
		}
		public GloDeclContext gloDecl(int i) {
			return getRuleContext(GloDeclContext.class,i);
		}
		public List<ScriDeclContext> scriDecl() {
			return getRuleContexts(ScriDeclContext.class);
		}
		public ScriDeclContext scriDecl(int i) {
			return getRuleContext(ScriDeclContext.class,i);
		}
		public HscContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_hsc; }
	}

	public final HscContext hsc() throws RecognitionException {
		HscContext _localctx = new HscContext(_ctx, getState());
		enterRule(_localctx, 0, RULE_hsc);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(20); 
			_errHandler.sync(this);
			_la = _input.LA(1);
			do {
				{
				setState(20);
				_errHandler.sync(this);
				switch ( getInterpreter().adaptivePredict(_input,0,_ctx) ) {
				case 1:
					{
					setState(18);
					gloDecl();
					}
					break;
				case 2:
					{
					setState(19);
					scriDecl();
					}
					break;
				}
				}
				setState(22); 
				_errHandler.sync(this);
				_la = _input.LA(1);
			} while ( _la==LP );
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class GloDeclContext extends ParserRuleContext {
		public TerminalNode LP() { return getToken(BS_ReachParser.LP, 0); }
		public TerminalNode VALUETYPE() { return getToken(BS_ReachParser.VALUETYPE, 0); }
		public TerminalNode ID() { return getToken(BS_ReachParser.ID, 0); }
		public ExprContext expr() {
			return getRuleContext(ExprContext.class,0);
		}
		public TerminalNode RP() { return getToken(BS_ReachParser.RP, 0); }
		public GloDeclContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_gloDecl; }
	}

	public final GloDeclContext gloDecl() throws RecognitionException {
		GloDeclContext _localctx = new GloDeclContext(_ctx, getState());
		enterRule(_localctx, 2, RULE_gloDecl);
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(24);
			match(LP);
			setState(25);
			match(T__0);
			setState(26);
			match(VALUETYPE);
			setState(27);
			match(ID);
			setState(28);
			expr();
			setState(29);
			match(RP);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ScriDeclContext extends ParserRuleContext {
		public TerminalNode LP() { return getToken(BS_ReachParser.LP, 0); }
		public ScriptTypeContext scriptType() {
			return getRuleContext(ScriptTypeContext.class,0);
		}
		public RetTypeContext retType() {
			return getRuleContext(RetTypeContext.class,0);
		}
		public TerminalNode ID() { return getToken(BS_ReachParser.ID, 0); }
		public TerminalNode RP() { return getToken(BS_ReachParser.RP, 0); }
		public ScriptParamsContext scriptParams() {
			return getRuleContext(ScriptParamsContext.class,0);
		}
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public ScriDeclContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_scriDecl; }
	}

	public final ScriDeclContext scriDecl() throws RecognitionException {
		ScriDeclContext _localctx = new ScriDeclContext(_ctx, getState());
		enterRule(_localctx, 4, RULE_scriDecl);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(31);
			match(LP);
			setState(32);
			match(T__1);
			setState(33);
			scriptType();
			setState(34);
			retType();
			setState(35);
			match(ID);
			setState(37);
			_errHandler.sync(this);
			switch ( getInterpreter().adaptivePredict(_input,2,_ctx) ) {
			case 1:
				{
				setState(36);
				scriptParams();
				}
				break;
			}
			setState(40); 
			_errHandler.sync(this);
			_la = _input.LA(1);
			do {
				{
				{
				setState(39);
				expr();
				}
				}
				setState(42); 
				_errHandler.sync(this);
				_la = _input.LA(1);
			} while ( (((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << BOOLIT) | (1L << VALUETYPE) | (1L << DAMAGEREGION) | (1L << MODELSTATE) | (1L << ID) | (1L << STRING) | (1L << FLOAT) | (1L << INT) | (1L << LP))) != 0) );
			setState(44);
			match(RP);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ScriptParamsContext extends ParserRuleContext {
		public TerminalNode LP() { return getToken(BS_ReachParser.LP, 0); }
		public List<TerminalNode> VALUETYPE() { return getTokens(BS_ReachParser.VALUETYPE); }
		public TerminalNode VALUETYPE(int i) {
			return getToken(BS_ReachParser.VALUETYPE, i);
		}
		public List<TerminalNode> ID() { return getTokens(BS_ReachParser.ID); }
		public TerminalNode ID(int i) {
			return getToken(BS_ReachParser.ID, i);
		}
		public TerminalNode RP() { return getToken(BS_ReachParser.RP, 0); }
		public ScriptParamsContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_scriptParams; }
	}

	public final ScriptParamsContext scriptParams() throws RecognitionException {
		ScriptParamsContext _localctx = new ScriptParamsContext(_ctx, getState());
		enterRule(_localctx, 6, RULE_scriptParams);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(46);
			match(LP);
			setState(47);
			match(VALUETYPE);
			setState(48);
			match(ID);
			setState(54);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while (_la==T__2) {
				{
				{
				setState(49);
				match(T__2);
				setState(50);
				match(VALUETYPE);
				setState(51);
				match(ID);
				}
				}
				setState(56);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(57);
			match(RP);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class CallContext extends ParserRuleContext {
		public TerminalNode LP() { return getToken(BS_ReachParser.LP, 0); }
		public FuncIDContext funcID() {
			return getRuleContext(FuncIDContext.class,0);
		}
		public TerminalNode RP() { return getToken(BS_ReachParser.RP, 0); }
		public List<ExprContext> expr() {
			return getRuleContexts(ExprContext.class);
		}
		public ExprContext expr(int i) {
			return getRuleContext(ExprContext.class,i);
		}
		public CallContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_call; }
	}

	public final CallContext call() throws RecognitionException {
		CallContext _localctx = new CallContext(_ctx, getState());
		enterRule(_localctx, 8, RULE_call);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(59);
			match(LP);
			setState(60);
			funcID();
			setState(64);
			_errHandler.sync(this);
			_la = _input.LA(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << BOOLIT) | (1L << VALUETYPE) | (1L << DAMAGEREGION) | (1L << MODELSTATE) | (1L << ID) | (1L << STRING) | (1L << FLOAT) | (1L << INT) | (1L << LP))) != 0)) {
				{
				{
				setState(61);
				expr();
				}
				}
				setState(66);
				_errHandler.sync(this);
				_la = _input.LA(1);
			}
			setState(67);
			match(RP);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class FuncIDContext extends ParserRuleContext {
		public TerminalNode ID() { return getToken(BS_ReachParser.ID, 0); }
		public FuncIDContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_funcID; }
	}

	public final FuncIDContext funcID() throws RecognitionException {
		FuncIDContext _localctx = new FuncIDContext(_ctx, getState());
		enterRule(_localctx, 10, RULE_funcID);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(69);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__3) | (1L << T__4) | (1L << T__5) | (1L << T__6) | (1L << T__7) | (1L << T__8) | (1L << T__9) | (1L << T__10) | (1L << T__11) | (1L << ID))) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class RetTypeContext extends ParserRuleContext {
		public TerminalNode VALUETYPE() { return getToken(BS_ReachParser.VALUETYPE, 0); }
		public RetTypeContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_retType; }
	}

	public final RetTypeContext retType() throws RecognitionException {
		RetTypeContext _localctx = new RetTypeContext(_ctx, getState());
		enterRule(_localctx, 12, RULE_retType);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(71);
			_la = _input.LA(1);
			if ( !(_la==T__12 || _la==VALUETYPE) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ExprContext extends ParserRuleContext {
		public CallContext call() {
			return getRuleContext(CallContext.class,0);
		}
		public TerminalNode INT() { return getToken(BS_ReachParser.INT, 0); }
		public TerminalNode FLOAT() { return getToken(BS_ReachParser.FLOAT, 0); }
		public TerminalNode STRING() { return getToken(BS_ReachParser.STRING, 0); }
		public TerminalNode DAMAGEREGION() { return getToken(BS_ReachParser.DAMAGEREGION, 0); }
		public TerminalNode MODELSTATE() { return getToken(BS_ReachParser.MODELSTATE, 0); }
		public TerminalNode BOOLIT() { return getToken(BS_ReachParser.BOOLIT, 0); }
		public TerminalNode ID() { return getToken(BS_ReachParser.ID, 0); }
		public TerminalNode VALUETYPE() { return getToken(BS_ReachParser.VALUETYPE, 0); }
		public ExprContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_expr; }
	}

	public final ExprContext expr() throws RecognitionException {
		ExprContext _localctx = new ExprContext(_ctx, getState());
		enterRule(_localctx, 14, RULE_expr);
		try {
			setState(82);
			_errHandler.sync(this);
			switch (_input.LA(1)) {
			case LP:
				enterOuterAlt(_localctx, 1);
				{
				setState(73);
				call();
				}
				break;
			case INT:
				enterOuterAlt(_localctx, 2);
				{
				setState(74);
				match(INT);
				}
				break;
			case FLOAT:
				enterOuterAlt(_localctx, 3);
				{
				setState(75);
				match(FLOAT);
				}
				break;
			case STRING:
				enterOuterAlt(_localctx, 4);
				{
				setState(76);
				match(STRING);
				}
				break;
			case DAMAGEREGION:
				enterOuterAlt(_localctx, 5);
				{
				setState(77);
				match(DAMAGEREGION);
				}
				break;
			case MODELSTATE:
				enterOuterAlt(_localctx, 6);
				{
				setState(78);
				match(MODELSTATE);
				}
				break;
			case BOOLIT:
				enterOuterAlt(_localctx, 7);
				{
				setState(79);
				match(BOOLIT);
				}
				break;
			case ID:
				enterOuterAlt(_localctx, 8);
				{
				setState(80);
				match(ID);
				}
				break;
			case VALUETYPE:
				enterOuterAlt(_localctx, 9);
				{
				setState(81);
				match(VALUETYPE);
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static class ScriptTypeContext extends ParserRuleContext {
		public ScriptTypeContext(ParserRuleContext parent, int invokingState) {
			super(parent, invokingState);
		}
		@Override public int getRuleIndex() { return RULE_scriptType; }
	}

	public final ScriptTypeContext scriptType() throws RecognitionException {
		ScriptTypeContext _localctx = new ScriptTypeContext(_ctx, getState());
		enterRule(_localctx, 16, RULE_scriptType);
		int _la;
		try {
			enterOuterAlt(_localctx, 1);
			{
			setState(84);
			_la = _input.LA(1);
			if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << T__13) | (1L << T__14) | (1L << T__15) | (1L << T__16) | (1L << T__17) | (1L << T__18))) != 0)) ) {
			_errHandler.recoverInline(this);
			}
			else {
				if ( _input.LA(1)==Token.EOF ) matchedEOF = true;
				_errHandler.reportMatch(this);
				consume();
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.reportError(this, re);
			_errHandler.recover(this, re);
		}
		finally {
			exitRule();
		}
		return _localctx;
	}

	public static final String _serializedATN =
		"\3\u608b\ua72a\u8133\ub9ed\u417c\u3be7\u7786\u5964\3!Y\4\2\t\2\4\3\t\3"+
		"\4\4\t\4\4\5\t\5\4\6\t\6\4\7\t\7\4\b\t\b\4\t\t\t\4\n\t\n\3\2\3\2\6\2\27"+
		"\n\2\r\2\16\2\30\3\3\3\3\3\3\3\3\3\3\3\3\3\3\3\4\3\4\3\4\3\4\3\4\3\4\5"+
		"\4(\n\4\3\4\6\4+\n\4\r\4\16\4,\3\4\3\4\3\5\3\5\3\5\3\5\3\5\3\5\7\5\67"+
		"\n\5\f\5\16\5:\13\5\3\5\3\5\3\6\3\6\3\6\7\6A\n\6\f\6\16\6D\13\6\3\6\3"+
		"\6\3\7\3\7\3\b\3\b\3\t\3\t\3\t\3\t\3\t\3\t\3\t\3\t\3\t\5\tU\n\t\3\n\3"+
		"\n\3\n\2\2\13\2\4\6\b\n\f\16\20\22\2\5\4\2\6\16\32\32\4\2\17\17\27\27"+
		"\3\2\20\25\2]\2\26\3\2\2\2\4\32\3\2\2\2\6!\3\2\2\2\b\60\3\2\2\2\n=\3\2"+
		"\2\2\fG\3\2\2\2\16I\3\2\2\2\20T\3\2\2\2\22V\3\2\2\2\24\27\5\4\3\2\25\27"+
		"\5\6\4\2\26\24\3\2\2\2\26\25\3\2\2\2\27\30\3\2\2\2\30\26\3\2\2\2\30\31"+
		"\3\2\2\2\31\3\3\2\2\2\32\33\7\36\2\2\33\34\7\3\2\2\34\35\7\27\2\2\35\36"+
		"\7\32\2\2\36\37\5\20\t\2\37 \7\37\2\2 \5\3\2\2\2!\"\7\36\2\2\"#\7\4\2"+
		"\2#$\5\22\n\2$%\5\16\b\2%\'\7\32\2\2&(\5\b\5\2\'&\3\2\2\2\'(\3\2\2\2("+
		"*\3\2\2\2)+\5\20\t\2*)\3\2\2\2+,\3\2\2\2,*\3\2\2\2,-\3\2\2\2-.\3\2\2\2"+
		"./\7\37\2\2/\7\3\2\2\2\60\61\7\36\2\2\61\62\7\27\2\2\628\7\32\2\2\63\64"+
		"\7\5\2\2\64\65\7\27\2\2\65\67\7\32\2\2\66\63\3\2\2\2\67:\3\2\2\28\66\3"+
		"\2\2\289\3\2\2\29;\3\2\2\2:8\3\2\2\2;<\7\37\2\2<\t\3\2\2\2=>\7\36\2\2"+
		">B\5\f\7\2?A\5\20\t\2@?\3\2\2\2AD\3\2\2\2B@\3\2\2\2BC\3\2\2\2CE\3\2\2"+
		"\2DB\3\2\2\2EF\7\37\2\2F\13\3\2\2\2GH\t\2\2\2H\r\3\2\2\2IJ\t\3\2\2J\17"+
		"\3\2\2\2KU\5\n\6\2LU\7\35\2\2MU\7\34\2\2NU\7\33\2\2OU\7\30\2\2PU\7\31"+
		"\2\2QU\7\26\2\2RU\7\32\2\2SU\7\27\2\2TK\3\2\2\2TL\3\2\2\2TM\3\2\2\2TN"+
		"\3\2\2\2TO\3\2\2\2TP\3\2\2\2TQ\3\2\2\2TR\3\2\2\2TS\3\2\2\2U\21\3\2\2\2"+
		"VW\t\4\2\2W\23\3\2\2\2\t\26\30\',8BT";
	public static final ATN _ATN =
		new ATNDeserializer().deserialize(_serializedATN.toCharArray());
	static {
		_decisionToDFA = new DFA[_ATN.getNumberOfDecisions()];
		for (int i = 0; i < _ATN.getNumberOfDecisions(); i++) {
			_decisionToDFA[i] = new DFA(_ATN.getDecisionState(i), i);
		}
	}
}