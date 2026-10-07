using Esprima.Utils;
using Mosaic.UI.Wpf.Scripting;

namespace Esprima.Ast
{
    public sealed class Identifier : Expression
    {
        public readonly string? Name;
        internal ScriptIdentifier ScriptIdentifier;

        public Identifier(string? name) : base(Nodes.Identifier)
        {
            Name = name;
            ScriptIdentifier = new ScriptIdentifier(Name);
        }

        public override NodeCollection ChildNodes => NodeCollection.Empty;        

        protected internal override void Accept(AstVisitor visitor)
        {
            visitor.VisitIdentifier(this);
        }
    }
}
