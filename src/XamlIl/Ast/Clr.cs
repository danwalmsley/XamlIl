﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Transform;
using XamlIl.TypeSystem;
using Visitor = XamlIl.Ast.XamlIlAstVisitorDelegate;

namespace XamlIl.Ast
{
    public class XamlIlAstClrTypeReference : XamlIlAstNode, IXamlIlAstTypeReference
    {
        public IXamlIlType Type { get; }

        public XamlIlAstClrTypeReference(IXamlIlLineInfo lineInfo, IXamlIlType type) : base(lineInfo)
        {
            Type = type;
        }

        public override string ToString() => Type.GetFqn();
    }

    public class XamlIlAstClrPropertyReference : XamlIlAstNode, IXamlIlAstPropertyReference
    {
        public IXamlIlProperty Property { get; set; }

        public XamlIlAstClrPropertyReference(IXamlIlLineInfo lineInfo, IXamlIlProperty property) : base(lineInfo)
        {
            Property = property;
        }

        public override string ToString() => Property.PropertyType.GetFqn() + "." + Property.Name;
    }

    public class XamlIlPropertyAssignmentNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlProperty Property { get; set; }
        public IXamlIlAstValueNode Value { get; set; }

        public XamlIlPropertyAssignmentNode(IXamlIlLineInfo lineInfo,
            IXamlIlProperty property, IXamlIlAstValueNode value)
            : base(lineInfo)
        {
            Property = property;
            Value = value;
        }
    }
    
    public class XamlIlPropertyValueManipulationNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlProperty Property { get; set; }
        public IXamlIlAstManipulationNode Manipulation { get; set; }
        public XamlIlPropertyValueManipulationNode(IXamlIlLineInfo lineInfo, 
            IXamlIlProperty property, IXamlIlAstManipulationNode manipulation) 
            : base(lineInfo)
        {
            Property = property;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            Manipulation = (IXamlIlAstManipulationNode) Manipulation.Visit(visitor);
        }
    }

    public abstract class XamlIlInstanceMethodCallBaseNode : XamlIlAstNode
    {
        public IXamlIlMethod Method { get; set; }
        public List<IXamlIlAstValueNode> Arguments { get; set; }
        public XamlIlInstanceMethodCallBaseNode(IXamlIlLineInfo lineInfo, 
            IXamlIlMethod method, IEnumerable<IXamlIlAstValueNode> args) 
            : base(lineInfo)
        {
            Method = method;
            Arguments = args?.ToList() ?? new List<IXamlIlAstValueNode>();
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            VisitList(Arguments, visitor);
        }
    }
    
    public class XamlIlInstanceNoReturnMethodCallNode : XamlIlInstanceMethodCallBaseNode, IXamlIlAstManipulationNode
    {
        public XamlIlInstanceNoReturnMethodCallNode(IXamlIlLineInfo lineInfo, IXamlIlMethod method, IEnumerable<IXamlIlAstValueNode> args)
            : base(lineInfo, method, args)
        {
        }
    }
    
    public class XamlIlStaticReturnMethodCallNode : XamlIlInstanceMethodCallBaseNode, IXamlIlAstValueNode
    {
        public XamlIlStaticReturnMethodCallNode(IXamlIlLineInfo lineInfo, IXamlIlMethod method, IEnumerable<IXamlIlAstValueNode> args)
            : base(lineInfo, method, args)
        {
            Type = new XamlIlAstClrTypeReference(lineInfo, method.ReturnType);
        }

        public IXamlIlAstTypeReference Type { get; }
    }

    public class XamlIlManipulationGroupNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public List<IXamlIlAstManipulationNode> Children { get; set; } = new List<IXamlIlAstManipulationNode>();
        public XamlIlManipulationGroupNode(IXamlIlLineInfo lineInfo) : base(lineInfo)
        {
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor) => VisitList(Children, visitor);
    }

    public class XamlIlValueWithManipulationNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public IXamlIlAstValueNode Value { get; set; }
        public IXamlIlAstManipulationNode Manipulation { get; set; }
        public IXamlIlAstTypeReference Type => Value.Type;
        
        public XamlIlValueWithManipulationNode(IXamlIlLineInfo lineInfo,
            IXamlIlAstValueNode value,
            IXamlIlAstManipulationNode manipulation) : base(lineInfo)
        {
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            Value = (IXamlIlAstValueNode) Value?.Visit(visitor);
            Manipulation = (IXamlIlAstManipulationNode) Manipulation?.Visit(visitor);
        }
    }

    public class XamlIlAstNewClrObjectNode : XamlIlAstNode, IXamlIlAstValueNode
    {
        public XamlIlAstNewClrObjectNode(IXamlIlLineInfo lineInfo,
            IXamlIlAstTypeReference type,
            List<IXamlIlAstValueNode> arguments) : base(lineInfo)
        {
            Type = type;
            Arguments = arguments;
        }

        public IXamlIlAstTypeReference Type { get; set; }
        public List<IXamlIlAstValueNode> Arguments { get; set; } = new List<IXamlIlAstValueNode>();

        public override void VisitChildren(Visitor visitor)
        {
            Type = (IXamlIlAstTypeReference) Type.Visit(visitor);
            VisitList(Arguments, visitor);
        }
    }

    public class XamlIlMarkupExtensionNode : XamlIlAstNode, IXamlIlAstManipulationNode
    {
        public IXamlIlAstValueNode Value { get; set; }
        public IXamlIlProperty Property { get; set; }
        public IXamlIlMethod ProvideValue { get; }
        public IXamlIlMethod Manipulation { get; set; }

        public XamlIlMarkupExtensionNode(IXamlIlLineInfo lineInfo, IXamlIlProperty property, IXamlIlMethod provideValue,
            IXamlIlAstValueNode value, IXamlIlMethod manipulation) : base(lineInfo)
        {
            Property = property;
            ProvideValue = provideValue;
            Value = value;
            Manipulation = manipulation;
        }

        public override void VisitChildren(XamlIlAstVisitorDelegate visitor)
        {
            Value = (IXamlIlAstValueNode) Value.Visit(visitor);
        }
    }
   
}