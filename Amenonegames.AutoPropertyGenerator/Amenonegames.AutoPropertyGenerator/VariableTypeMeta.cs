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


using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Amenonegames.AutoPropertyGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amenonegames.SourceGenerator;

class UnionMeta
{
    public string SubTypeTag { get; set; }
    public INamedTypeSymbol SubTypeSymbol { get; set; }
    public string FullTypeName { get; }

    public UnionMeta(string subTypeTag, INamedTypeSymbol subTypeSymbol)
    {
        SubTypeTag = subTypeTag;
        SubTypeSymbol = subTypeSymbol;
        FullTypeName = subTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}

class VariableTypeMeta
{
    public VariableDeclaratorSyntax Syntax { get; }
    public IFieldSymbol Symbol { get; }
    public ImmutableArray<AttributeData> AttributeDatas { get; }
    public string TypeName { get; }
    public string FullTypeName { get; }
    //public IReadOnlyList<IMethodSymbol> Constructors { get; }
    public AXS AXSArgument { get; }
    public ITypeSymbol? SourceType { get; }
    public ITypeSymbol? TargetType { get; }
    
    public ClassDeclarationSyntax? ClassSyntax;
    public INamedTypeSymbol ClassSymbol { get; }
    
    //public IEnumerable<SyntaxToken> VariableSyntax => GetVariableSyntax(Syntax);
    ReferenceSymbols references;

    public VariableTypeMeta(
        Compilation? compilation,
        VariableDeclaratorSyntax syntax,
        IFieldSymbol symbol,
        ImmutableArray<AttributeData> attr,
        ReferenceSymbols references)
    {
        Syntax = syntax;
        Symbol = symbol;
        
        if (compilation != null) SourceType = GetVariableTypeSymbol(Syntax, compilation);
        TargetType = SourceType;
        AXSArgument = AXS.PublicGet;
        
        this.references = references;
        
        ClassSyntax = GetContainingClassSyntax(syntax);
        ClassSymbol = Symbol.ContainingType;
        
        TypeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        FullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        AttributeDatas = attr;

        foreach (var attributeData in AttributeDatas)
        {
            foreach (var arg in attributeData.ConstructorArguments)
            {
                if (SymbolEqualityComparer.Default.Equals(arg.Type, references.AXSAttribute))
                {
                    AXSArgument = arg.Value != null
                        ? (AXS)arg.Value
                        : AXS.PublicGet;
                    continue;
                }
                
                if (SymbolEqualityComparer.Default.Equals(arg.Type, references.TypeAttribute))
                {
                    TargetType = arg.Value as ITypeSymbol;
                    continue;
                }
            }
        }


        // Constructors = symbol.InstanceConstructors
        //     .Where(x => !x.IsImplicitlyDeclared) // remove empty ctor(struct always generate it), record's clone ctor
        //     .ToArray();


    }
    
    IEnumerable<SyntaxToken> GetVariableSyntax(FieldDeclarationSyntax fieldDeclaration)
    {
        var variableNames = new List< SyntaxToken>();
        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            variableNames.Add(variable.Identifier);
        }

        return variableNames;
    }
    
    ITypeSymbol GetFieldType(FieldDeclarationSyntax fieldDeclarationSyntax, Compilation compilation)
    {
        var semanticModel = compilation.GetSemanticModel(fieldDeclarationSyntax.SyntaxTree);
        var typeSyntax = fieldDeclarationSyntax.Declaration.Type;
        var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;

        return typeSymbol;
    }
    
    TypeSyntax GetVariableType(VariableDeclaratorSyntax variableDeclarator)
    {
        var parentDeclaration = variableDeclarator.Parent as VariableDeclarationSyntax;
        return parentDeclaration?.Type;
    }

    // セマンティックモデルを使用して型シンボルを取得（オプショナル）
    ITypeSymbol GetVariableTypeSymbol(VariableDeclaratorSyntax variableDeclarator, Compilation compilation)
    {
        var semanticModel = compilation.GetSemanticModel(variableDeclarator.SyntaxTree);
        var typeSyntax = GetVariableType(variableDeclarator);
        return typeSyntax != null ? semanticModel.GetTypeInfo(typeSyntax).Type : null;
    }

    /// <summary>
    /// TypeDeclarationSyntaxがfieldDeclarationであることを前提にする
    /// </summary>
    /// <param name="variableDeclaratorSyntax"></param>
    /// <returns></returns>
    ClassDeclarationSyntax? GetContainingClassSyntax(VariableDeclaratorSyntax variableDeclaratorSyntax)
    {
        var parent = variableDeclaratorSyntax.Parent;
        while (parent != null)
        {
            if (parent is ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration;
            }
            parent = parent.Parent;
        }

        return null;
    }
    
    
}
