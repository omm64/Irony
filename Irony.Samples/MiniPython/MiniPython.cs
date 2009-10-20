#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using Irony.Ast;

namespace Irony.Samples.MiniPython {
  // The grammar for a very small subset of Python. This is work in progress, 
  // I will be adding more features as we go along, bringing it closer to real python.
  // Current version: expressions, assignments, indented code blocks, function defs, function calls
  // Full support for Python line joining rules: line continuation symbol "\", automatic line joining when 
  //  line ends in the middle of expression, with unbalanced parenthesis
  // Python is important test case for Irony as an indentation-sensitive language.

  [Language("MiniPython", "0.5", "Subset of Python")]
  public class MiniPythonGrammar : Irony.Parsing.Grammar {
    public MiniPythonGrammar() {

      // 1. Terminals
      var number = TerminalFactory.CreatePythonNumber("number");
      var identifier = TerminalFactory.CreatePythonIdentifier("identifier");
      var comment = new CommentTerminal("comment", "#", "\n", "\r");
      //comment must to be added to NonGrammarTerminals list; it is not used directly in grammar rules,
      // so we add it to this list to let Scanner know that it is also a valid terminal. 
      base.NonGrammarTerminals.Add(comment);
      var comma = ToTerm(",");
      var colon = ToTerm(":");

      // 2. Non-terminals
      var Expr = new NonTerminal("Expr");
      var Term = new NonTerminal("Term");
      var BinExpr = new NonTerminal("BinExpr", typeof(BinExprNode));
      var ParExpr = new NonTerminal("ParExpr");
      var UnExpr = new NonTerminal("UnExpr", typeof(UnExprNode));
      var UnOp = new NonTerminal("UnOp");
      var BinOp = new NonTerminal("BinOp", "operator");
      var AssignmentStmt = new NonTerminal("AssignmentStmt", typeof(AssigmentNode));
      var Stmt = new NonTerminal("Stmt");
      var ExtStmt = new NonTerminal("ExtStmt");
      var Block = new NonTerminal("Block", typeof(BlockNode));
      var StmtList = new NonTerminal("StmtList", typeof(StatementListNode));

      var ParamList = new NonTerminal("ParamList", typeof(ParamListNode));
      var ArgList = new NonTerminal("ArgList", typeof(ExpressionListNode));
      var FunctionDef = new NonTerminal("FunctionDef", typeof(FunctionDefNode));
      var FunctionCall = new NonTerminal("FunctionCall", typeof(FunctionCallNode));


      // 3. BNF rules
      Expr.Rule = Term | UnExpr | BinExpr;
      Term.Rule = number | ParExpr | identifier | FunctionCall;
      ParExpr.Rule = "(" + Expr + ")";
      UnExpr.Rule = UnOp + Term;
      UnOp.Rule = ToTerm("+") | "-";
      BinExpr.Rule = Expr + BinOp + Expr;
      BinOp.Rule = ToTerm("+") | "-" | "*" | "/" | "**";
      AssignmentStmt.Rule = identifier + "=" + Expr;
      Stmt.Rule = AssignmentStmt | Expr | Empty;
      //Eos is End-Of-Statement token produced by CodeOutlineFilter
      ExtStmt.Rule = Stmt + Eos | Block | FunctionDef;
      Block.Rule = Indent + StmtList + Dedent;
      StmtList.Rule = MakeStarRule(StmtList, ExtStmt);

      ParamList.Rule = MakeStarRule(ParamList, comma, identifier);
      ArgList.Rule = MakeStarRule(ArgList, comma, Expr);
      FunctionDef.Rule = "def" + identifier + "(" + ParamList + ")" + colon + Eos + Block;
      FunctionCall.Rule = identifier + "(" + ArgList + ")";

      this.Root = StmtList;       // Set grammar root

      // 4. Token filter
      //we need to add continuation symbol to NonGrammarTerminals because it is not used anywhere in grammar
      var lineContinuationTerm = ToTerm(@"\");
      NonGrammarTerminals.Add(lineContinuationTerm);
      var outlineFilter = new CodeOutlineFilter(OutlineOptions.ProduceIndents | OutlineOptions.CheckBraces, lineContinuationTerm);
      TokenFilters.Add(outlineFilter);

      // 5. Operators precedence
      RegisterOperators(1, "+", "-");
      RegisterOperators(2, "*", "/");
      RegisterOperators(3, Associativity.Right, "**");

      // 6. Miscellaneous: punctuation, braces, transient nodes
      RegisterPunctuation("(", ")", ":", "else");
      RegisterBracePair("(", ")");
      MarkTransient(Term, Expr, Stmt, ExtStmt, UnOp, BinOp, ExtStmt, ParExpr);

      //automatically add NewLine before EOF so that our BNF rules work correctly when there's no final line break in source
      this.LanguageFlags = LanguageFlags.CreateAst | LanguageFlags.CanRunSample; // | LanguageFlags.NewLineBeforeEOF;

    }
  }
}//namespace

