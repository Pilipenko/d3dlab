using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Ext;

namespace D3DLab.Debugger.IDE {
    //https://github.com/lukebuehler/NRefactory-Completion-Sample
    //https://github.com/icsharpcode/NRefactory
    //https://www.codeproject.com/Articles/42490/Using-AvalonEdit-WPF-Text-Editor

    public class ShaderDevelopmentEnvironment {
        public FlowDocument ShaderDocument { get; private set; }
        SemanticAnalyzer Semantic;

        SyntaxHighlighter highlighter;
        readonly ShaderSyntaxInfo syntax;
        readonly List<LexerNode> highlightedLexers;
        public ShaderDevelopmentEnvironment() {
            syntax = new ShaderSyntaxInfo();
            ShaderDocument = new FlowDocument();
            ShaderDocument.FontSize = 13;
            highlightedLexers = new List<LexerNode>();

        }

        public void Read(string shadertext) {
            Semantic = new SemanticAnalyzer();
            highlighter = new SyntaxHighlighter(syntax);
            highlightedLexers.Clear();
            var ast = new SyntaxParser(Semantic, syntax);
            try {
                //shadertext = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "IDE", "TestShaders", "Test.fx"));

                shadertext = shadertext.Replace(Environment.NewLine, "\n");
                ShaderDocument.Blocks.Clear();
                ShaderDocument.Blocks.Add(new Paragraph(new Run(shadertext)));
                //
                var la = new LexicalAnalyzer(syntax);
                la.Parse(ShaderDocument.ContentStart, shadertext);
                var lexer = la.Lexers;
                //
                ast.Parse(lexer);
            }catch (SyntaxException se) {
                highlighter.HighlightException(se);
            } catch (Exception ex) {
                ex.ToString();
            }

            ast.Prog.Handle(highlighter);
            //highlighter.ApplyComments(ast.Comments);
        }


        public IEnumerable<string> GetProperiesOfType(string variableName, TextRange range) {
            var scope = Semantic.GetScope(range);
            if (scope == null) {
                return new string[0];
            }
            return Semantic.GetProperiesOfTypeByVariableName(variableName, scope);
        }

        public IEnumerable<string> GetVariablesOfScope(string words, TextRange range) {
            var scope = Semantic.GetScope(range);
            if (scope == null) {
                return new string[0];
            }
            return Semantic.GetVariableOfScopeByWorlds(words, scope);
        }

        public IEnumerable<string> GetShaderKeywords(string words) {
            return syntax.AllKeys.Where(x => x.StartsWith(words));
        }

        public IEnumerable<string> GetGlobalTypes(string words) {
            return Semantic.GetGlobalTypesByWorlds(words);
        }

        public void HighlightRelations(string variableName, TextRange range) {
            UnHighlightAll();

            if (string.IsNullOrWhiteSpace(variableName)) {
                return;
            }
            var scope = Semantic.GetScope(range);

            if (scope.IsNull()) {
                return;
            }

            if (scope.TryGetLexerRelations(variableName, out var relations)) {
                relations.ForEach(highlighter.HighlightSelection);
                highlightedLexers.AddRange(relations);
            }
        }

        public void UnHighlightAll() {
            highlightedLexers.ForEach(highlighter.UnHighlightSelection);
            highlightedLexers.Clear();
        }

        public bool IsKeyword(string text) {
            return syntax.AllKeys.Any(x=>x==text);
        }
    }


    class SyntaxParser : AbstractSyntaxTree {
        public List<LexerNode> Comments { get; }
        public DefinitionObjectNode Prog { get; }

        readonly SemanticAnalyzer semantic;
        readonly ShaderSyntaxInfo syntax;
        public SyntaxParser(SemanticAnalyzer analizer, ShaderSyntaxInfo syntax) {
            this.semantic = analizer;
            this.syntax = syntax;
            Comments = new List<LexerNode>();
            Prog = new DefinitionObjectNode();
        }

        public void Parse(Queue<LexerNode> lexer) {
            var vis = new Visitor();
            while (lexer.Any()) {
                Parse(lexer, Prog);
                //Prog.Children.Add(node);
            }
        }

        #region second try

        void Parse(Queue<LexerNode> lexers, DefinitionObjectNode parent) {
            var next = lexers.Peek();
            switch (next.Token) {
                case LexerTokens.Operator:
                    switch (next.Value) {
                        case ShaderSyntaxInfo.Semicolon:
                            lexers.Dequeue();//skip lex 
                            return;

                    }
                    break;
                case LexerTokens.Comment:
                    parent.Children.Add(new CommentsNode() { Lex = lexers.Dequeue() });
                    return;
                case LexerTokens.Punctuation:
                    switch (next.Value) {
                        case "#":
                            // ==== examples ==== //
                            // #include "./Shaders/Common.fx"
                            // ==== ++++++++ ==== //
                            lexers.Dequeue();//for '#'
                            var id = new IncludeDeclaration {
                                Name = lexers.Dequeue(),//get key
                                Path = new StringNode { NameLex = lexers.Dequeue() }
                            };
                            parent.Children.Add(id);
                            return;
                        case "["://attrinuter: [maxvertexcount(18)]
                            var body = new ListNode();
                            DelimitedBy(lexers, DelimeterParams.Attribute, body, lex => { while (lex.Any()) { lex.Dequeue(); } return null; });
                            return;
                    }
                    break;
            }
            //specific keys to parse first
            switch (next.Value) {
                case ShaderSyntaxInfo.StructKey:
                    var var = VarDeclarationParse(lexers);
                    var strn = new StructNode { VarDeclaration = var, Parent = parent };
                    using (semantic.RegisterStruct(var.DeclarationName.NameLex, strn)) {
                        parent.Children.Add(strn);

                        DelimitedBy(lexers, DelimeterParams.Sequences, strn.Children, lex => VarDeclarationWithSemanticNamesParse(lex, strn));
                        if (lexers.Peek().Value == ShaderSyntaxInfo.Semicolon) {
                            //in some cases ';' is end of declaration 
                            lexers.Dequeue();
                        }
                    }
                    return;
                case ShaderSyntaxInfo.CbufferKey:
                    var cbn = new CBufferNode { VarDeclaration = VarDeclarationParse(lexers), Parent = parent };
                    lexers.Dequeue().ThrowIfNot(ShaderSyntaxInfo.Colon);
                    parent.Children.Add(cbn);
                    cbn.Register = Parse(lexers, new FunctionCallNode { Parent = cbn, Name = new SystemName { NameLex = lexers.Dequeue() } });
                    DelimitedBy(lexers, DelimeterParams.Sequences, cbn.Children, lex => Parse(lex, cbn));
                    return;
            }
            //parse any variable
            var variable = VarDeclarationParse(lexers);
            if (lexers.Peek().Value == ShaderSyntaxInfo.Semicolon) {//end of declaration or definition
                                                                    //float4 illumDiffuse;
                lexers.Dequeue();//skip Semicolon                                                         
                variable.Parent = parent;
                parent.Children.Add(variable);
                return;
            }

            //
            next = lexers.Peek();
            switch (next.Token) {
                case LexerTokens.Punctuation:
                    // ==== examples ==== //
                    //int foo(InputT input, ...) { }
                    var fdn = new FunctionDeclarationNode {
                        Parent = parent,
                        VarDeclaration = variable
                    };

                    parent.Children.Add(fdn);

                    DelimitedBy(lexers, DelimeterParams.Function, fdn.Vars,
                              lex => {
                                  //var v = VarDeclarationParse(lex);
                                  var v = VarDeclarationWithSemanticNamesParse(lex, parent);
                                  lex.ThrowIfAny();
                                  return v;
                              });
                    using (semantic.RegisterFunction(variable.DeclarationName.NameLex, fdn)) {
                        //maybe have semantic name
                        var semanticNameMaybe = lexers.Peek();
                        if (semanticNameMaybe.Token == LexerTokens.Operator && semanticNameMaybe.Value == ":") {
                            lexers.Dequeue();//skip operator
                            fdn.SemanticNameLex = lexers.Dequeue();
                        }
                        var boundary = DelimitedBy(lexers, DelimeterParams.Sequences, fdn.Children,
                            lex => FunctionParse(lex, fdn));
                        fdn.Boundary = boundary;
                    }
                    
                    return;
                case LexerTokens.Operator:
                    parent.Children.Add(AssignmentParse(lexers, new AssignmentVariableDefinition() { Left = variable }));
                    break;
            }
        }

        AbstractNode VarDeclarationWithSemanticNamesParse(Queue<LexerNode> lexers, DefinitionObjectNode parent) {
            var variable = VarDeclarationParse(lexers);
            if (lexers.Any()) {
                var op = lexers.Peek();
                switch (op.Value) {
                    case ShaderSyntaxInfo.Colon: // ==== examples ==== 
                                                 //float4 c	: COLOR0;    
                                                 // ==== ++++++++ ==== //    
                        var svd = new VariableDefinitionWitSemanticName { Parent = parent };
                        svd.VarDeclaration = variable;
                        using (lexers.Boundary(ShaderSyntaxInfo.Colon, ShaderSyntaxInfo.Semicolon)) {
                            svd.SemanticNameLex = lexers.Dequeue();
                        }
                        svd.Parent = parent;
                        return svd;
                }
            }
            variable.Parent = parent;
            return variable;
        }
        VarDeclaration Parse(Queue<LexerNode> lexers, CBufferNode parent) {
            var variable = VarDeclarationParse(lexers);
            variable.Parent = parent;
            return variable;
        }
        FunctionCallNode Parse(Queue<LexerNode> lexers, FunctionCallNode parent) {
            DelimitedBy(lexers, DelimeterParams.Function, parent.Args,
                                lex => {
                                    var v = ExpressionParse(lex);
                                    lex.ThrowIfAny();
                                    return v;
                                });
            return parent;
        }
        Queue<LexerNode> SeparateExpression(Queue<LexerNode> lexers, out LexerNode op) {
            var left = new Queue<LexerNode>();
            op = null;//custom.t = float2(dx / abs(dx), dy / abs(dy));
            var bracket = 0;
            while (lexers.Any()) {
                var lex = lexers.Dequeue();
                switch (lex.Token) {
                    case LexerTokens.Punctuation:
                        switch (lex.Value) {
                            case ShaderSyntaxInfo.StartFuncDeclaration:
                                bracket++;
                                break;
                            case ")":
                                bracket--;
                                break;
                        }
                        break;
                    case LexerTokens.Operator:
                        if (bracket == 0) {
                            op = lex;
                            return left;
                        }
                        break;
                }
                left.Enqueue(lex);
            }
            return left;
        }

        BinaryExpression BinaryExpressionParse(Queue<LexerNode> lexers, BinaryExpression parent) {
            var left = SeparateExpression(lexers, out var op);
            //LexerNode op = null;//custom.t = float2(dx / abs(dx), dy / abs(dy));


            var leftExpr = ExpressionParse(left);

            var right = lexers;

            if (op == null || op.Value == ShaderSyntaxInfo.Semicolon) {
                parent.Right = leftExpr;
                return parent;
            }

            op.ThrowIfNot(LexerTokens.Operator);

            var expr = new BinaryExpression {
                Left = leftExpr,
                OperatorLex = op
            };

            parent.Right = BinaryExpressionParse(right, expr);

            return parent;
        }

        AbstractNode FunctionParse(Queue<LexerNode> lexers, FunctionDeclarationNode parent) {
            var variable = VarDeclarationParse(lexers);

            if (!lexers.Any()) {
                variable.Parent = parent;
                return variable;
            }
            var next = lexers.Peek();
            switch (next.Token) {
                case LexerTokens.Operator:
                    var expr = AssignmentParse(lexers, new AssignmentVariableDefinition() { Left = variable });
                    expr.Parent = parent;
                    return expr;
                case LexerTokens.Punctuation:
                    switch (next.Value) {
                        case ShaderSyntaxInfo.StartFuncDeclaration:
                            var call = Parse(lexers, new FunctionCallNode { Name = variable });
                            if (!lexers.Any()) {
                                return call;
                            }
                            next = lexers.Peek();
                            next.ThrowIfNot(LexerTokens.Operator);
                            return BinaryExpressionParse(lexers, new BinaryExpression { Left = call, OperatorLex = lexers.Dequeue() });
                    }
                    var rs = ExpressionParse(lexers);
                    rs.Parent = parent;
                    return rs;
            }
            variable.Parent = parent;
            return variable;
        }


        BinaryExpression AssignmentParse(Queue<LexerNode> lexers, AssignmentVariableDefinition parent) {
            var equally = lexers.Dequeue();

            equally.ThrowIfNot("=");

            parent.OperatorLex = equally;

            var tillSemicolon = DelimitedTill(lexers, BySemicolonPredicate);

            tillSemicolon.ThrowIfEmpty(equally);

            return BinaryExpressionParse(tillSemicolon, parent);
        }
        AbstractNode ExpressionParse(Queue<LexerNode> lexers) {
            AbstractNode name = ParseName(lexers);

            if (!lexers.Any()) {
                return name;
            }

            var next = lexers.Peek();

            switch (next.Token) {

                case LexerTokens.Punctuation:
                    switch (next.Value) {
                        case ShaderSyntaxInfo.StartFuncDeclaration:
                            var call = Parse(lexers, new FunctionCallNode { Name = name });
                            if (!lexers.Any()) {
                                return call;
                            }
                            next = lexers.Peek();
                            next.ThrowIfNot(LexerTokens.Operator);
                            return BinaryExpressionParse(lexers, new BinaryExpression { Left = call, OperatorLex = lexers.Dequeue() });
                    }
                    break;
                case LexerTokens.Operator:
                    return BinaryExpressionParse(lexers, new BinaryExpression { Left = name, OperatorLex = lexers.Dequeue() });
            }

            return name;
        }
        VarDeclaration VarDeclarationParse(Queue<LexerNode> lexers) {
            VarDeclaration node = new VarDeclaration();
            var typeNode = IsKeyword(lexers.Peek()) ? TypeParse(lexers) : null;
            var name = ParseName(lexers);

            node.TypeNode = typeNode;
            node.DeclarationName = name;

            return node;
        }
        NameNode ParseName(Queue<LexerNode> lexers) {
            var name = lexers.Dequeue();

            if (name.Token == LexerTokens.Number) {
                return new NumbertNode { NameLex = name };
            }

            if (name.Token == LexerTokens.String) {
                return new StringNode { NameLex = name };
            }

            if (lexers.Any()) { // specific variable 
                var next = lexers.Peek();
                switch (next.Token) {
                    case LexerTokens.Punctuation:
                        switch (next.Value) {
                            case ShaderSyntaxInfo.StartArrayDeclaration:
                                //input[3]
                                using (lexers.Boundary(syntax.VarArrayBoundary[0], syntax.VarArrayBoundary[1])) {
                                    var num = lexers.Dequeue();
                                    return new ArrayNameNode {
                                        NameLex = name,
                                        IndexerLex = num
                                    };
                                }
                        }
                        break;
                }
            }



            return syntax.AllKeys.Is(name.Value) ? (NameNode)new SystemName { NameLex = name } : new LiteralNode { NameLex = name };
        }

        bool IsKeyword(LexerNode next) {
            var isKey = false;
            switch (next.Token) {
                case LexerTokens.Keyword:
                    // ==== examples ==== //
                    //struct GSInputLS {} 
                    //cbuffer ProjectionBuffer {}     
                    // ==== ++++++++ ==== //
                    isKey = true;
                    break;
                case LexerTokens.Identifier:
                    // ==== examples ==== //
                    //GSInputLS VShaderLines(VSInputLS input) {
                    // ==== ++++++++ ==== //
                    if (semantic.NewKeys.Contains(next.Value)) {
                        isKey = true;
                    }
                    break;
            }
            return isKey;
        }

        TypeNode TypeParse(Queue<LexerNode> lexers) {
            TypeNode typen = new DatatypeNode();

            var lex = lexers.Dequeue();
            if (syntax.Modifications.Is(lex.Value)) {
                typen.ModificationLex = lex;
                lex = lexers.Dequeue();
            }

            if (syntax.Array.Contains(lex.Value)) {
                typen = new ArrayType { ModificationLex = typen.ModificationLex };
            } else if (syntax.Primitive.Contains(lex.Value)) {
                typen = new PrimitiveType { ModificationLex = typen.ModificationLex };
            } else if (syntax.Generics.Is(lex.Value)) {
                //seaching generic type T
                using (lexers.Boundary("<", ">")) {
                    var mod = typen.ModificationLex;
                    typen = new GenericTypeNode() {
                        ModificationLex = mod,
                        TTypeLex = lexers.Dequeue(),
                    };
                }
            }
            typen.TypeLex = lex;
            return typen;
        }

        #endregion

        #region firts try

        #endregion

        protected override void OnIgnored(LexerNode lex) {
            Comments.Add(lex);
        }


    }

    class BoundaryValidator : IDisposable {
        readonly Queue<LexerNode> lexers;
        readonly string end;

        public BoundaryValidator(Queue<LexerNode> lexers, string start, string end) {
            this.end = end;
            this.lexers = lexers;
            lexers.Dequeue().ThrowIfNot(start);
        }

        public void Dispose() {
            if (lexers.Any()) {
                lexers.Dequeue().ThrowIfNot(end);
            }
        }
    }
    static class LexerQueueEx {
        public static BoundaryValidator Boundary(this Queue<LexerNode> lexers, string start, string end) {
            return new BoundaryValidator(lexers, start, end);
        }
    }
    static class SyntaxEx {
        public static void ThrowIfNot(this LexerNode node, string _case) {
            if (node.Value != _case) {
                throw new SyntaxException(node);
            }
        }
        public static void ThrowIfNot(this LexerNode node, LexerTokens token) {
            if (node.Token != token) {
                throw new SyntaxException(node);
            }
        }
        public static void ThrowSyntaxException(this LexerNode node) {
            throw new SyntaxException(node);
        }
        public static void ThrowIfAny(this IEnumerable<LexerNode> lexers) {
            if (lexers.Any()) {
                throw new SyntaxException(lexers.First());
            }
        }
        public static void ThrowIfEmpty(this IEnumerable<LexerNode> lexers, LexerNode node) {
            if (!lexers.Any()) {
                throw new SyntaxException(node);
            }
        }
    }



    public class SemanticException : Exception {
        public SemanticException(string text) : base(text) { }
    }
}
