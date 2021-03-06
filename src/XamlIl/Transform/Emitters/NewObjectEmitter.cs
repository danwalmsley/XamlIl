using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class NewObjectEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlAstNewClrObjectNode n))
                return null;
            var type = n.Type.GetClrType();

            var argTypes = n.Arguments.Select(a => a.Type.GetClrType()).ToList();
            var ctor = type.FindConstructor(argTypes);
            if (ctor == null)
                throw new XamlIlLoadException(
                    $"Unable to find public constructor for type {type.GetFqn()}({string.Join(", ", argTypes.Select(at => at.GetFqn()))})",
                    n);

            for (var c = 0; c < n.Arguments.Count; c++)
            {
                var ctorArg = n.Arguments[c];
                context.Emit(ctorArg, codeGen, ctor.Parameters[c]);
            }

            var gen = codeGen.Generator
                .Emit(OpCodes.Newobj, ctor);
            
            
            return XamlIlNodeEmitResult.Type(type);
        }
    }
}