//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.7
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from C:/Users/gqqnbig/IdeaProjects/untitled/src\Paradox.g4 by ANTLR 4.7

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using Antlr4.Runtime.Misc;
using IParseTreeListener = Antlr4.Runtime.Tree.IParseTreeListener;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete listener for a parse tree produced by
/// <see cref="ParadoxParser"/>.
/// </summary>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.7")]
[System.CLSCompliant(false)]
public interface IParadoxListener : IParseTreeListener {
	/// <summary>
	/// Enter a parse tree produced by <see cref="ParadoxParser.paradox"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterParadox([NotNull] ParadoxParser.ParadoxContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="ParadoxParser.paradox"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitParadox([NotNull] ParadoxParser.ParadoxContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="ParadoxParser.kvPair"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterKvPair([NotNull] ParadoxParser.KvPairContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="ParadoxParser.kvPair"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitKvPair([NotNull] ParadoxParser.KvPairContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="ParadoxParser.scope"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterScope([NotNull] ParadoxParser.ScopeContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="ParadoxParser.scope"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitScope([NotNull] ParadoxParser.ScopeContext context);
	/// <summary>
	/// Enter a parse tree produced by <see cref="ParadoxParser.atom"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void EnterAtom([NotNull] ParadoxParser.AtomContext context);
	/// <summary>
	/// Exit a parse tree produced by <see cref="ParadoxParser.atom"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	void ExitAtom([NotNull] ParadoxParser.AtomContext context);
}