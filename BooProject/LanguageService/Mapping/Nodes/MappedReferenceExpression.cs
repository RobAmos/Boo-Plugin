﻿//
//   Copyright © 2010 Michael Feingold
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Internal;

namespace Hill30.BooProject.LanguageService.Mapping.Nodes
{
    public class MappedReferenceExpression : MappedNode
    {
        private readonly NodeMap nodeMap;
        private string quickInfoTip;
        private MappedNode declarationNode;
        private IType varType;

        public MappedReferenceExpression(NodeMap nodeMap, BufferMap bufferMap, ReferenceExpression node)
            : base(bufferMap, node, node.Name.Length)
        {
            this.nodeMap = nodeMap;
        }

        public MappedReferenceExpression(NodeMap nodeMap, BufferMap bufferMap, SelfLiteralExpression node)
            : base(bufferMap, node, "self".Length)
        {
            this.nodeMap = nodeMap;
        }

        public override MappedNodeType Type
        {
            get { return MappedNodeType.VraiableReference; }
        }

        public override string QuickInfoTip
        {
            get { return quickInfoTip; }
        }

        protected override void ResolveImpl()
        {
            switch (Node.NodeType)
            {
                case NodeType.SelfLiteralExpression:
                    var classDefinition = Node;
                    while (classDefinition.ParentNode != null)
                        if (classDefinition.NodeType != NodeType.ClassDefinition)
                            classDefinition = classDefinition.ParentNode;
                        else
                        {
                            varType = TypeSystemServices.GetType(classDefinition);
                            break;
                        }
                    break;

                case NodeType.MemberReferenceExpression:
                case NodeType.ReferenceExpression:
                    var expression = (ReferenceExpression)Node;
                    if (expression.ExpressionType == null || expression.ExpressionType.EntityType == EntityType.Error)
                        break;
                    var entity = TypeSystemServices.GetEntity(expression);
                    var prefix = "";
                    if (entity is InternalParameter)
                    {
                        prefix = "(parameter) ";
                        varType = TypeSystemServices.GetType(expression);
                        declarationNode = nodeMap.GetMappedNode(((InternalParameter)entity).Parameter);
                    }
                    if (entity is InternalLocal)
                    {
                        prefix = "(local variable) ";
                        varType = ((InternalLocal)entity).Type;
                        declarationNode = nodeMap.GetMappedNode(((InternalLocal)entity).Local);
                    }
                    if (entity is InternalField)
                    {
                        varType = TypeSystemServices.GetType(Node);
                        declarationNode = nodeMap.GetMappedNode(((InternalField)entity).Field);
                    }
                    if (entity is InternalMethod)
                    {
                        var declaration = ((InternalMethod) entity).Method;
                        declarationNode = nodeMap.GetMappedNode(declaration);
                        varType = TypeSystemServices.GetType(declaration.ReturnType);
                    }
                    if (entity is InternalProperty)
                    {
                        varType = TypeSystemServices.GetType(Node);
                        declarationNode = nodeMap.GetMappedNode(((InternalProperty)entity).Property);
                    }
                    if (entity is InternalEvent)
                    {
                        varType = TypeSystemServices.GetType(Node);
                        declarationNode = nodeMap.GetMappedNode(((InternalEvent)entity).Event);
                    }
                    quickInfoTip = prefix + expression.Name + " as " + expression.ExpressionType.FullName;
                    break;
                default:
                    break;
            }
        }

        protected internal override MappedNode DeclarationNode { get { return declarationNode; } }

        public override BooDeclarations Declarations
        {
            get
            {
                return new BooDeclarations(Node, varType, true);
            }
        }

        internal override void Record(RecordingStage stage, List<MappedNode> list)
        {
            switch (stage)
            {
                case RecordingStage.Completed:
                    var macro = list.Where(
                        node => (node.Node is MacroStatement &&
                                    ((MacroStatement)node.Node).Name == ((ReferenceExpression)Node).Name)
                        ).FirstOrDefault();
                    if (macro != null)
                        list.Remove(macro);
                    break;
                default:
                    break;
            }
            base.Record(stage, list);
        }
    }
}