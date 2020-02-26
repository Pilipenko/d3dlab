using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace D3DLab.Debugger.IDE {

    class GloalObject { // fuction / cbuffer / struct / property
        public LexerNode Name;

        public virtual void Analyze() { }

        public Dictionary<string, VariableDefinitionWitSemanticName> Properties;
        public List<string> Methods;
        public GloalObject() {
            Properties = new Dictionary<string, VariableDefinitionWitSemanticName>();
            Methods = new List<string>();
        }
    }
    class StructGloalObject : GloalObject {
        public StructNode Node;

        public override void Analyze() {
            if (Properties.Any()) {
                return;
            }
            foreach (var pr in Node.Children) {
                if (pr is VariableDefinitionWitSemanticName vari) {
                    Properties.Add(vari.VarDeclaration.DeclarationName.NameLex.Value, vari);//vari.DeclarationName
                } else {
                    Console.WriteLine(pr.ToString());
                }
            }
        }
    }
    class FuncGloalObject : GloalObject, IASTNodeVisitor {
        public FunctionDeclarationNode Node;
        // key - variable name
        // value - variable type -> global types of primitive types
        public readonly Dictionary<string, string> VariableTypesMapper;
        readonly Dictionary<string, List<LexerNode>> funcOrVarNameLexerMapper;

        public FuncGloalObject() {
            VariableTypesMapper = new Dictionary<string, string>();
            funcOrVarNameLexerMapper = new Dictionary<string, List<LexerNode>>();
        }
        public override void Analyze() {
            if (VariableTypesMapper.Any()) {
                return;
            }
            foreach (var v in Node.Vars) {
                v.Handle(this);
            }
            try {
                foreach (var pr in Node.Children) {
                    pr.Handle(this);
                }
            }catch (Exception ex) {
                ex.ToString();
            }
        }

        public bool TryGetLexerRelations(string name, out IEnumerable<LexerNode> lexers) {
            var res =  funcOrVarNameLexerMapper.TryGetValue(name, out var list);
            lexers = list?.Select(x=> new LexerNode(x.Token,name,x.StartPointer));
            return res;
        }

        #region visitor

        public void UpdateVariables(LexerNode typeLex, LexerNode nameLex) {
            var name = nameLex.Value;
            UpdateVariables(nameLex);
            var type = typeLex.Value;
            if (!VariableTypesMapper.ContainsKey(name)) {
                VariableTypesMapper.Add(name, type);
            }
        }
        public void UpdateVariables(LexerNode nameLex) {
            var names = nameLex.Value.Split('.');
            var offset = 0;
            for (int i = 0; i < names.Length; i++) {
                var name = names[i];
                if (!funcOrVarNameLexerMapper.TryGetValue(name, out var list)) {
                    list = new List<LexerNode>();
                    funcOrVarNameLexerMapper.Add(name, list);
                }
                list.Add(new LexerNode(nameLex.Token, name, nameLex.StartPointer.GetPositionAtOffset(offset)));
                offset += name.Length + 1;//+1 is dot element
            }
        }

        public void Visit(CommentsNode node) {
            
        }

        public void Visit(IncludeDeclaration includeDeclaration) {
            
        }

        public void Visit(FunctionDeclarationNode definitionObjectNode) {
            
        }

        public void Visit(VarDeclaration vardec) {
            var nameLex = vardec.DeclarationName.NameLex;
            if (vardec.TypeNode.IsNotNull()) {
                var typeLex = vardec.TypeNode.TypeLex;
                UpdateVariables(typeLex, nameLex);
            } else {
                UpdateVariables(nameLex);
            }
        }

        public void Visit(DefinitionObjectNode definitionObjectNode) {
            
        }

        public void Visit(VariableDefinitionWitSemanticName definitionObjectNode) {
            
        }

        public void Visit(CBufferNode definitionObjectNode) {
            
        }

        public void Visit(StructNode definitionObjectNode) {
            
        }

        public void Visit(NameNode sn) {
            UpdateVariables(sn.NameLex);
        }

        public void Visit(ArrayNameNode nameNode) {

        }

        public void Visit(StringNode definitionObjectNode) {
            
        }

        public void Visit(NumbertNode definitionObjectNode) {
            
        }

        public void Visit(SystemName sn) {
            UpdateVariables(sn.NameLex);
        }

        public void Visit(GenericTypeNode typeNode) {
            
        }

        public void Visit(DatatypeNode typeNode) {
            
        }

        public void Visit(PrimitiveType typeNode) {
            
        }

        public void Visit(ArrayType typeNode) {
            
        }

        public void Visit(StringTypeNode typeNode) {
            
        }

        public void Visit(BinaryExpression binaryExpression) {
            binaryExpression.Left.Handle(this);
            binaryExpression.Right.Handle(this);
        }

        public void Visit(FunctionCallNode functionCallNode) {
            functionCallNode.Name.Handle(this);
            functionCallNode.Args.ForEach(x => x.Handle(this));
        }

        #endregion
    }

    public class SemanticAnalyzer {
        //1 top-level semantic analyzer
        //2 statement-and-expression semantic analyzer

        public HashSet<string> NewKeys { get { return new HashSet<string>(global.Keys); } }

        private readonly Dictionary<string, GloalObject> global = new Dictionary<string, GloalObject>();
        //key - variable name
        //value - type name
        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();
        private readonly Dictionary<string, FuncGloalObject> scopes = new Dictionary<string, FuncGloalObject>();

        private ScopeRegistration currentRegistration;

        public SemanticAnalyzer() {
            currentRegistration = new EmptyRegistration();
        }

        public bool CanRegisterGlobalFunction() {
            return currentRegistration.IsFinished;
        }

        internal IDisposable RegisterStruct(LexerNode lexer, StructNode node) {
            if (!CanRegisterGlobalFunction()) {
                throw new SemanticException($"Unavailable struct declaration '{lexer.Value}' inside another function '{currentRegistration.Name}'");
            }
            var str = new StructGloalObject { Name = lexer, Node = node };
            global.Add(lexer.Value, str);
            currentRegistration = new ScopeRegistration(lexer.Value, str);
            return currentRegistration;
        }
        internal IDisposable RegisterFunction(LexerNode lexer, FunctionDeclarationNode node) {
            if (!CanRegisterGlobalFunction()) {
                throw new SemanticException($"Unavailable function declaration '{lexer.Value}' inside another function '{currentRegistration.Name}'");
            }
            var f = new FuncGloalObject { Name = lexer, Node = node };
            global.Add(lexer.Value, f);
            scopes.Add(lexer.Value, f);

            currentRegistration = new ScopeRegistration(lexer.Value, f);
            return currentRegistration;
        }


        internal FuncGloalObject GetScope(TextRange range) {
            FuncGloalObject currentScope = null;
            try {
                foreach (var scope in scopes) {
                    var box = scope.Value.Node.Boundary;
                    var start = scope.Value.Node.VarDeclaration.DeclarationName.NameLex.StartPointer;
                    var fromStartToCurent = start.GetOffsetToPosition(range.Start);
                    var fromCurrenToEnd = range.Start.GetOffsetToPosition(box.EndPointer.StartPointer);
                    if (fromStartToCurent > 0 && fromCurrenToEnd > 0) {
                        currentScope = scope.Value;
                        break;
                    }
                }
            }catch(Exception ex) {
                Console.WriteLine(ex);
            }
            return currentScope;
        }

        internal IEnumerable<string> GetProperiesOfTypeByVariableName(string variableName, FuncGloalObject currentScope) {
            if (!currentScope.VariableTypesMapper.ContainsKey(variableName)) {
                return new string[0];
            }

            var type = currentScope.VariableTypesMapper[variableName];

            if (global.TryGetValue(type, out var obj)) {
                //obj.Analyze();
                return obj.Properties.Keys.Union(obj.Methods);
            }

            return new string[0];
        }
        internal IEnumerable<string> GetVariableOfScopeByWorlds(string worlds, FuncGloalObject currentScope) {
            var valiables = currentScope.VariableTypesMapper.Keys.Where(x => x.StartsWith(worlds));

            return valiables;
        }
        internal IEnumerable<string> GetGlobalTypesByWorlds(string worlds) {
            var valiables = global.Keys.Where(x => x.StartsWith(worlds));

            return valiables;
        }

        class ScopeRegistration : IDisposable {
            public bool IsFinished { get; protected set; }
            readonly GloalObject func;

            public string Name { get; }

            public ScopeRegistration(string name, GloalObject func) {
                Name = name;
                this.func = func;
            }
            public void Dispose() {
                func.Analyze();
                IsFinished = true;
            }
        }
        class EmptyRegistration : ScopeRegistration {
            public EmptyRegistration() : base(null, null) {
                IsFinished = true;
            }
        }
    }
}
