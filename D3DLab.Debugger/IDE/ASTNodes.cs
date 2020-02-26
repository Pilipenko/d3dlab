using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace D3DLab.Debugger.IDE {
    class CommentsNode : AbstractNode {
        public LexerNode Lex;
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    class ListNode : List<AbstractNode> { }
    abstract class AbstractNode {
        public AbstractNode Parent;
        public abstract void Handle(IASTNodeVisitor  highlighter);
    }
    class Boundary {
        public LexerNode StartPointer;
        public LexerNode EndPointer;
    }

    
    abstract class NameNode : AbstractNode {
        public LexerNode NameLex;

        public override string ToString() {
            return NameLex.Value;
        }

        
    }
    class LiteralNode : NameNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    class NumbertNode : NameNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    class StringNode : NameNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    //example: foo(PSInput input[3])
    class ArrayNameNode : NameNode {
        public LexerNode IndexerLex;

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.AppendFormat("[{0}]", IndexerLex.Value);
            return sb.ToString();
        }
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    class SystemName : NameNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }


    //

    /*
    declarations: (just signature)
        char example1;
        void example3(float a);
    definitions: (implementation)
        void example3(float a) { .. }
    */
    abstract class DeclarationOrDefinition : AbstractNode {
        
    }

    #region definition
    abstract class DefinitionNode : DeclarationOrDefinition {
        public VarDeclaration VarDeclaration;
    }

    class DefinitionObjectNode : DefinitionNode {
        public ListNode Children = new ListNode();

        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }

    
    class VariableDefinitionWitSemanticName : DefinitionNode {
        // example: [InterpolationModifier] Type[RxC] MemberName [:Semantic];
        /// <summary>
        /// Optional parameter-usage information, used by the compiler to link shader inputs and outputs. There are several predefined semantics for vertex and pixel shaders. The compiler ignores semantics unless they are declared on a global variable, or a parameter passed into a shader.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb509647(v=vs.85).aspx
        /// </summary>
        public LexerNode SemanticNameLex;
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }

    class FunctionDeclarationNode : DefinitionObjectNode {
        public Boundary Boundary;
        public LexerNode SemanticNameLex;
        public ListNode Vars = new ListNode();
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append('(');
            Vars.ForEach(x => sb.Append(x.ToString()).Append(','));
            sb.Append(')');
            return sb.ToString();
        }
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }

    
    class StructNode : DefinitionObjectNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    class CBufferNode : DefinitionObjectNode {
        public AbstractNode Register;
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }


    #endregion

    #region Declaration

    abstract class DeclarationNode : DeclarationOrDefinition {
        public TypeNode TypeNode;
        public NameNode DeclarationName;

        public override string ToString() {
            var sb = new StringBuilder();
            if (TypeNode != null) {
                sb.Append(TypeNode.ToString()).Append(' ');
            }
            sb.Append(DeclarationName.ToString());
            return sb.ToString();
        }
    }
    class VarDeclaration : DeclarationNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }

    class IncludeDeclaration : DeclarationOrDefinition {
        public LexerNode Name;
        public StringNode Path;
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    
    #endregion

    abstract class TypeNode : AbstractNode {
        public LexerNode TypeLex;
        //inout
        public LexerNode ModificationLex; 

        public override string ToString() {
            var mod = ModificationLex != null ? ModificationLex.Value : string.Empty;
            return $"{mod} {TypeLex.Value}";
        }
        
    }
    class DatatypeNode : TypeNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    class PrimitiveType : TypeNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    // float2 float3 float4
    class ArrayType : TypeNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    class StringTypeNode : TypeNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    
    class GenericTypeNode : TypeNode {//TriangleStream<PSInput> 
        public LexerNode TTypeLex;

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.AppendFormat("<{0}>", TTypeLex.Value);
            return sb.ToString();
        }
        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }
    }
    //
    abstract class ExpressionNode : AbstractNode {
        
    }

    class BinaryExpression : ExpressionNode {
        public LexerNode OperatorLex;
        public AbstractNode Left;
        public AbstractNode Right;

        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(Left.ToString());
            if (Right.IsNotNull()) {
                sb.Append(OperatorLex.Value);
                sb.Append(Right.ToString());
            }
            return sb.ToString();
        }
    }

    class AssignmentVariableDefinition : BinaryExpression {

    }
    class FunctionCallNode : ExpressionNode {
        public AbstractNode Name;
        public ListNode Args = new ListNode();

        public override void Handle(IASTNodeVisitor  highlighter) {
            highlighter.Visit(this);
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(Name.ToString());
            sb.Append('(');
            Args.ForEach(x => sb.Append(',').Append(x.ToString()));
            sb.Append(')');
            return sb.ToString();
        }
    }

    //
    class StatementNode : AbstractNode {
        public override void Handle(IASTNodeVisitor  highlighter) {
            throw new NotImplementedException();
        }
    }
    class IfStatementNode : StatementNode { }
    class LoopStatementNode : StatementNode { }
    //
    class OtherSyntaxObject { }
}
