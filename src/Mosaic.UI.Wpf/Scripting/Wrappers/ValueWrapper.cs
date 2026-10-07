using Expression = Esprima.Ast.Expression;
using Esprima.Ast;
using Esprima.Utils;

namespace Mosaic.UI.Wpf.Scripting
{
    internal sealed class ValueWrapper : Expression
    {
        public override NodeCollection ChildNodes => NodeCollection.Empty;

        public readonly object Value;

        public ValueWrapper(object value) : base(Nodes.ValueWrapper)
        {
            Value = value;
        }

        protected internal override void Accept(AstVisitor visitor)
        {
        }
    }
}
