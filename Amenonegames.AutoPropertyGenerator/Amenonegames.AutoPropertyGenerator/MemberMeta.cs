//this code copied from VYaml.SourceGenerator in https://github.com/hadashiA/VYaml?tab=MIT-1-ov-file

// Copyright (c) 2022 hadashiA
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//     

using System;
using System.Linq;
using Amenonegames.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amenonegames.SourceGenerator;

class MemberMeta
{
    public ISymbol Symbol { get; }
    public string Name { get; }
    public string FullTypeName { get; }
    public ITypeSymbol MemberType { get; }
    public bool IsField { get; }
    public bool IsProperty { get; }
    public bool IsSettable { get; }
    public int Order { get; }
    public bool HasExplicitOrder { get; }
    public bool HasKeyNameAlias { get; }
    public string KeyName { get; }

    public bool IsConstructorParameter { get; set; }
    public bool HasExplicitDefaultValueFromConstructor { get; set; }
    public object? ExplicitDefaultValueFromConstructor { get; set; }

    public byte[] KeyNameUtf8Bytes => keyNameUtf8Bytes ??= System.Text.Encoding.UTF8.GetBytes(KeyName);
    byte[]? keyNameUtf8Bytes;

    public MemberMeta(ISymbol symbol, ReferenceSymbols references, NamingConvention namingConvention, int sequentialOrder)
    {
        Symbol = symbol;
        Name = symbol.Name;
        Order = sequentialOrder;
        KeyName = KeyNameMutator.Mutate(Name, namingConvention);

        var memberAttribute = symbol.GetAttribute(references.YamlMemberAttribute);
        if (memberAttribute != null)
        {
            if (memberAttribute.ConstructorArguments.Length > 0 &&
                memberAttribute.ConstructorArguments[0].Value is string aliasValue)
            {
                HasKeyNameAlias = true;
                KeyName = aliasValue;
            }

            var orderProp = memberAttribute.NamedArguments.FirstOrDefault(x => x.Key == "Order");
            if (orderProp.Key != "Order" && orderProp.Value.Value is { } explicitOrder)
            {
                HasExplicitOrder = true;
                Order = (int)explicitOrder;
            }
        }

        if (symbol is IFieldSymbol f)
        {
            IsProperty = false;
            IsField = true;
            IsSettable = !f.IsReadOnly; // readonly field can not set.
            MemberType = f.Type;

        }
        else if (symbol is IPropertySymbol p)
        {
            IsProperty = true;
            IsField = false;
            IsSettable = !p.IsReadOnly;
            MemberType = p.Type;
        }
        else
        {
            throw new Exception("member is not field or property.");
        }
        FullTypeName = MemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public Location GetLocation(TypeDeclarationSyntax fallback)
    {
        var location = Symbol.Locations.FirstOrDefault() ?? fallback.Identifier.GetLocation();
        return location;
    }
}
