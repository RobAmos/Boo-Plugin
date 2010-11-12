﻿using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Compiler.TypeSystem.Internal;
using Microsoft.VisualStudio.Package;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.Ast;

namespace Hill30.BooProject.LanguageService
{
    public class BooDeclarations : Declarations
    {
        public class Declaration
        {
            public string DisplayText { get; set; }
            public string Description { get; set; }
            public int ImageIndex { get; set; }
            public int Count { get; set; }
        }

        private readonly SortedList<string, Declaration> list = new SortedList<string, Declaration>();

        public BooDeclarations()
        {
        }

        public BooDeclarations(Node context, IType varType, bool instance)
        {
            if (varType != null)
                foreach (var member in varType.GetMembers())
                {
                    switch (member.EntityType)
                    {
                        case EntityType.Method:
                            FormatMethod(context, (IMethod)member, instance);
                            break;
                        case EntityType.Property:
                            FormatProperty(context, (IProperty)member, instance);
                            break;
                        case EntityType.Event:
                            FormatEvent(context, (IEvent)member, instance);
                            break;
                        case EntityType.Field:
                            FormatField(context, (IField)member, instance);
                            break;
                    }
                }
            //for (var i = 1000; i < 1300; i += 6)
            //    list.Add("a" + i, new Declaration {DisplayText="a" + i, ImageIndex = i - 1000});
        }

        private static bool IsPrivate(Node context, IType type)
        {
            if (context == null)
                return false;
            if (context.NodeType == NodeType.ClassDefinition && TypeSystemServices.GetType(context) == type)
                    return true;
            return IsPrivate(context.ParentNode, type);
        }

        private void FormatField(Node context, IField field, bool instance)
        {
            if (field.IsStatic == instance)
                return;
            if (field.IsInternal && !(field is InternalField))
                return;
            if (field.IsPrivate && !IsPrivate(context, field.Type))
                    return;


            var name = field.Name;
            var description = name + " as " + field.Type;

            list.Add(name,
                new Declaration
                {
                    DisplayText = name,
                    Description = description,
                    ImageIndex = GetIconForNode(NodeType.Field, field.IsPublic, field.IsInternal, field.IsProtected, field.IsPrivate)
                });
        }

        private void FormatEvent(Node context, IEvent @event, bool instance)
        {
            if (@event.IsStatic == instance)
                return;

            var name = @event.Name;
            var description = name + " as " + @event.Type;

            list.Add(name,
                new Declaration
                {
                    DisplayText = name,
                    Description = description,
                    // Hmm... if it is not public - is it protected? or internal? let us make it private
                    ImageIndex = GetIconForNode(NodeType.Event, @event.IsPublic, /* @event.IsInternal */ false, /*@event.IsProtected*/ false, !@event.IsPublic)
                });
        }

        private void FormatProperty(Node context, IProperty property, bool instance)
        {
            if (property.IsStatic == instance)
                return;
            if (property.IsInternal && !(property is InternalProperty))
                return;
            if (property.IsPrivate && !IsPrivate(context, property.Type))
                return;

            var name = property.Name;
            var description = name + " as " + property.Type;

            if (property.IsExtension)
                description = "(extension) " + description;

            list.Add(name, 
                new Declaration
                    {
                        DisplayText = name,
                        Description = description,
                        ImageIndex = GetIconForNode(NodeType.Property, property.IsPublic, property.IsInternal, property.IsProtected, property.IsPrivate)
                    });
        }

        private void FormatMethod(Node context, IMethod method, bool instance)
        {
            if (method.IsAbstract)
                return;
            if (method.IsSpecialName)
                return;
            if (method.IsStatic == instance)
                return;
            if (method.IsInternal && !(method is InternalMethod))
                return;
            if (method.IsPrivate && !IsPrivate(context, method.ReturnType))
                return;


            var name = method.Name;
            Declaration declaration;
            if (list.TryGetValue(name, out declaration))
            {
                declaration.Count++;
                return;
            }

            var description = name + " as " + method.ReturnType;
            if (method.IsExtension)
                description = "(extension) " + description;

            list.Add(name, new Declaration 
                { 
                    DisplayText = name, 
                    Description = description, 
                    ImageIndex = GetIconForNode(NodeType.Method, method.IsPublic, method.IsInternal, method.IsProtected, method.IsPrivate)
                });
        }

        public override int GetCount()
        {
            return list.Count();
        }

        public override string GetDescription(int index)
        {
            if (list.Values[index].Count == 0)
                return list.Values[index].Description;
            return list.Values[index].Description + " (+" + list.Values[index].Count + " overload(s))";
        }

        public override string GetDisplayText(int index)
        {
            return list.Values[index].DisplayText;
        }

        public override int GetGlyph(int index)
        {
            return list.Values[index].ImageIndex;
        }

        public override string GetName(int index)
        {
            return list.Keys[index];
        }

        public static int GetIconForNode(TypeMember node)
        {
            return GetIconForNode(node.NodeType, node.IsPublic, node.IsInternal, node.IsProtected, node.IsPrivate);
        }

        public static int GetIconForNode(NodeType type, bool isPublic, bool isInternal, bool isProtected, bool isPrivate)
        {
            var result = int.MinValue;
            switch (type)
            {
                case NodeType.Module:
                case NodeType.ClassDefinition:
                    result = CLASS_ICONS;
                    break;
                case NodeType.EnumDefinition:
                    result = ENUM_ICONS;
                    break;
                case NodeType.StructDefinition:
                    result = STRUCT_ICONS;
                    break;
                case NodeType.InterfaceDefinition:
                    result = INTERFACE_ICONS;
                    break;

                case NodeType.EnumMember:
                    result = ENUM_MEMBER_ICONS;
                    break;
                case NodeType.Method:
                case NodeType.Constructor:
                case NodeType.Destructor:
                    result = METHOD_ICONS;
                    break;
                case NodeType.Property:
                    result = PROPERTY_ICONS;
                    break;
                case NodeType.Field:
                    result = FIELD_ICONS;
                    break;
                case NodeType.Event:
                    result = EVENT_ICONS;
                    break;
            }

            if (isPublic)
                result += ICON_PUBLIC;
            if (isPrivate)
                result += ICON_PRIVATE;
            if (isInternal)
                result += ICON_INTERNAL;
            else if (isProtected) // if it is internal protected, only the internal icon is shown
                result += ICON_PROTECTED;
            return result;
        }

        // ReSharper disable InconsistentNaming

        const int CLASS_ICONS = 0;
        const int CONST_ICONS = 6;
        const int DELEGATE_ICONS = 12;
        const int ENUM_ICONS = 18;
        const int ENUM_MEMBER_ICONS = 24;
        const int EVENT_ICONS = 30;
        const int FIELD_ICONS = 42;
        const int INTERFACE_ICONS = 48;
        const int METHOD_ICONS = 72;
        const int PROPERTY_ICONS = 102;
        const int STRUCT_ICONS = 108;

        const int ICON_PUBLIC = 0;
        const int ICON_INTERNAL = 1;
        const int ICON_DIAMOND = 2;
        const int ICON_PROTECTED = 3;
        const int ICON_PRIVATE = 4;
        const int ICON_REFERENCE = 5;

        // ReSharper restore InconsistentNaming

    }
}
