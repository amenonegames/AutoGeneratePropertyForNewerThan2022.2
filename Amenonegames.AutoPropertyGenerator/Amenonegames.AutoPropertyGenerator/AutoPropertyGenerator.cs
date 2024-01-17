using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Amenonegames.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Text;


namespace Amenonegames.AutoPropertyGenerator
{
    [Generator]
    public class AutoPropertyGenerator : IIncrementalGenerator
    {
        
        public void Initialize(IncrementalGeneratorInitializationContext  context)
        {
            context.RegisterPostInitializationOutput(x => SetDefaultAttribute(x));
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName
                (
                    context,
                    "AutoProperty.AutoPropAttribute",
                    static (node, cancellation) => node is FieldDeclarationSyntax,
                    static (cont, cancellation) => cont
                )
                .Combine(context.CompilationProvider);

            context.RegisterSourceOutput(
                context.CompilationProvider.Combine(provider.Collect()),
                (sourceProductionContext, t) =>
                {
                    var (compilation, list) = t;
                    var references = ReferenceSymbols.Create(compilation);
                    if (references is null)
                    {
                        return;
                    }
                    
                    var codeWriter = new CodeWriter();

                    var typeMetaList = new List<FieldTypeMeta>();
                    
                    foreach (var (x,y) in list)
                    {
                        typeMetaList.Add
                        (
                            new FieldTypeMeta(y,
                            (FieldDeclarationSyntax)x.TargetNode,
                            (INamedTypeSymbol)x.TargetSymbol,
                            x.Attributes,
                            references)
                        );
                    }

                    var classGrouped = typeMetaList.GroupBy( x  => x.ClassSymbol);

                    foreach (var classed in classGrouped)
                    {
                        if (TryEmit(classed, codeWriter, references, sourceProductionContext))
                        {
                            var className = classed.Key.Name;
                            sourceProductionContext.AddSource($"{className}.g.cs", codeWriter.ToString());
                        }
                        codeWriter.Clear();
                    }
                    
                }); //context.RegisterForSyntaxNotifications( () => new SyntaxReceiver() );
        }

        static bool TryEmit(
            IGrouping<INamedTypeSymbol,FieldTypeMeta>? typeMetaGroup,
            CodeWriter codeWriter,
            ReferenceSymbols references,
            in SourceProductionContext context)
        {
            FieldTypeMeta[] fieldTypeMetas = new FieldTypeMeta[] { };
            INamedTypeSymbol classSymbol = null;
            ClassDeclarationSyntax? classSyntax = null;
            var error = false;
            
            try
            {
                if (typeMetaGroup is not null)
                {
                    fieldTypeMetas = typeMetaGroup.ToArray();
                    // 親クラスを取得
                    classSymbol = typeMetaGroup.Key;
                    classSyntax = typeMetaGroup.First().ClassSyntax;
                }
            
                if (classSymbol is null || classSyntax is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ClassNotFound,
                        fieldTypeMetas.First().Syntax.GetLocation(),
                        String.Join("/",fieldTypeMetas.Select(x => x.VariableSyntax))));
                    error = true;
                }
                // verify is partial
                else if (!classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MustBePartial,
                        classSyntax.Identifier.GetLocation(),
                        classSyntax.Identifier.Text));
                    error = true;
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnexpectedErrorDescriptor,
                    Location.None,
                    ex.ToString()));
                return false;
            }
            

            try
            {
                var nameSpaceIsGlobal = classSymbol != null && classSymbol.ContainingNamespace.IsGlobalNamespace;
                var nameSpaceStr = nameSpaceIsGlobal ? "" : $"namespace {classSymbol.ContainingNamespace.ToDisplayString()}\n{{\n";
                var classAccessiblity = classSymbol?.DeclaredAccessibility.ToString().ToLower();
                
                codeWriter.AppendLine(nameSpaceStr);
                if(!nameSpaceIsGlobal) codeWriter.BeginBlock();
                
                codeWriter.AppendLine("// This class is generated by AutoPropertyGenerator.");
                codeWriter.AppendLine($"{classAccessiblity} partial class {classSymbol?.Name}");
                
                foreach (var fieldtypeMeta in fieldTypeMetas)
                {
                    var className = fieldtypeMeta?.TargetType?.ToDisplayString();
                    var sourceClassName = fieldtypeMeta?.TargetType?.ToDisplayString();
                    if (fieldtypeMeta?.TargetType?.Name is null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.VaribleNameNotFound,
                            fieldtypeMeta.Syntax.GetLocation(),
                            String.Join("/",fieldtypeMeta.VariableSyntax)));
                        error = true;
                    }
                    var propertyName = GetPropertyName(fieldtypeMeta?.TargetType?.Name);
                    bool typeIsSame = className == sourceClassName;
                    switch (fieldtypeMeta.AXSArgument)
                    {
                        case AXS.PrivateGet:
                        case AXS.PrivateGetSet:
                            codeWriter.AppendLine($"private");
                            break;
                        case AXS.PublicGet:
                        case AXS.PublicGetSet:
                        case AXS.PublicGetPrivateSet:
                            codeWriter.AppendLine($"public");
                            break;
                        case AXS.ProtectedGet:
                        case AXS.ProtectedGetSet:
                        case AXS.ProtectedGetPrivateSet:
                            codeWriter.AppendLine($"protected");
                            break;
                        case AXS.InternalGet:
                        case AXS.InternalGetSet:
                        case AXS.InternalGetPrivateSet:
                            codeWriter.AppendLine($"internal");
                            break;
                        case AXS.ProtectedInternalGet:
                        case AXS.ProtectedInternalGetSet:
                        case AXS.ProtectedInternalGetPrivateSet:
                            codeWriter.AppendLine($"protected internal");
                            break;
                        default:
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.AXSNotFound,
                                fieldtypeMeta.Syntax.GetLocation(),
                                String.Join("/",fieldtypeMeta.VariableSyntax)));
                            error = true;
                            break;
                    }
                    
                    codeWriter.Append($" {className} {propertyName}", false);
                    codeWriter.BeginBlock();
                    codeWriter.AppendLine("get");
                    codeWriter.BeginBlock();
                    
                    if (typeIsSame)
                    {
                        codeWriter.AppendLine($"return this.{fieldtypeMeta.Symbol.Name};");
                        codeWriter.EndBlock();
                    }
                    else
                    {
                        codeWriter.AppendLine($"return ({className})this.{fieldtypeMeta.Symbol.Name};");
                        codeWriter.EndBlock();
                    }

                    switch (fieldtypeMeta.AXSArgument)
                    {
                        case AXS.PrivateGetSet:
                        case AXS.ProtectedGetSet:
                        case AXS.PublicGetSet:
                        case AXS.InternalGetSet:
                        case AXS.ProtectedInternalGetSet:
                            codeWriter.AppendLine("set");
                            codeWriter.BeginBlock();
                            if (typeIsSame)
                            {
                                codeWriter.AppendLine($"this.{fieldtypeMeta.Symbol.Name} = value;");
                                codeWriter.EndBlock();
                            }
                            else
                            {
                                codeWriter.AppendLine($"this.{fieldtypeMeta.Symbol.Name} = ({sourceClassName})value;");
                                codeWriter.EndBlock();
                            }

                            break;

                        case AXS.PublicGetPrivateSet:
                        case AXS.ProtectedGetPrivateSet:
                        case AXS.InternalGetPrivateSet:
                        case AXS.ProtectedInternalGetPrivateSet:
                            codeWriter.AppendLine("private set");
                            codeWriter.BeginBlock();
                            if (typeIsSame)
                            {
                                codeWriter.AppendLine($"this.{fieldtypeMeta.Symbol.Name} = value;");
                                codeWriter.EndBlock();
                            }
                            else
                            {
                                codeWriter.AppendLine($"this.{fieldtypeMeta.Symbol.Name} = ({sourceClassName})value;");
                                codeWriter.EndBlock();
                            }

                            break;
                    }
                    codeWriter.EndBlock();

                }
                
                codeWriter.EndBlock();
                if(!nameSpaceIsGlobal) codeWriter.EndBlock();

                return true;
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnexpectedErrorDescriptor,
                    Location.None,
                    ex.ToString()));
                return false;
            }
            
        }


        static string? GetNamespace(ClassDeclarationSyntax classDeclaration)
        {
            var current = classDeclaration.Parent;
            while (current != null)
            {
                if (current is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    return namespaceDeclaration.Name.ToString();
                }
                current = current.Parent;
            }

            return null; // グローバル名前空間にある場合
        }
        
        private void SetDefaultAttribute(IncrementalGeneratorPostInitializationContext context)
        {
            // AutoPropertyAttributeのコード本体
            const string AttributeText = @"
using System;
namespace AutoProperty
{
    /// <summary>
    /// This class is generated by AutoPropertyGenerator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field,
                    Inherited = false, AllowMultiple = false)]
    sealed class AutoPropAttribute : Attribute
    {
    
        public Type Type { get; set; }
        public AXS AXSType { get; set; }
        
        // デフォルトアクセスレベルを変える場合は、ここを変更する
        public AutoPropAttribute(AXS access = AXS.PublicGet)
        {
            AXSType = access;
        }

        // デフォルトアクセスレベルを変える場合は、ここを変更する
        public AutoPropAttribute(Type type, AXS access = AXS.PublicGet)
        {
            Type = type;
            AXSType = access;
        }
        

    }

    [Flags]
    internal enum AXS
    {
        PublicGet = 1,
        PublicGetSet = 1 << 1,
        PublicGetPrivateSet = 1 << 2,
        PrivateGet = 1 << 3,
        PrivateGetSet = 1 << 4,
        ProtectedGet = 1 << 5,
        ProtectedGetSet = 1 << 6,
        ProtectedGetPrivateSet = 1 << 7,
        InternalGet = 1 << 8,
        InternalGetSet = 1 << 9,
        InternalGetPrivateSet = 1 << 10,
        ProtectedInternalGet = 1 << 11,
        ProtectedInternalGetSet = 1 << 12,
        ProtectedInternalGetPrivateSet = 1 << 13,
    }
}
";            
            //コンパイル時に参照するアセンブリを追加
            context.AddSource
            (
                "AutoPropAttribute.cs",
                SourceText.From(AttributeText,Encoding.UTF8)
            );
        }
        
        private static string GetPropertyName(string fieldName)
        {
            
            // 最初の大文字に変換可能な文字を探す
            for (int i = 0; i < fieldName.Length; i++)
            {
                if (char.IsLower(fieldName[i]))
                {
                    // 大文字に変換して、残りの文字列を結合
                    return char.ToUpper(fieldName[i]) + fieldName.Substring(i + 1);
                }
            }

            // 大文字に変換可能な文字がない場合
            return "NoLetterCanUppercase";
        }

        
    }


    
}