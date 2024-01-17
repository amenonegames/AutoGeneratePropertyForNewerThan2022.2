using System;
using System.Collections;
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
                    static (node, cancellation) => true,//node is FieldDeclarationSyntax,
                    static (cont, cancellation) => cont
                )
                .Combine(context.CompilationProvider)
                .WithComparer(Comparer.Instance);
            
            
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
                    var typeMetaList = new List<VariableTypeMeta>();
                    
                    var typemetalistCount = 0;
                    var groupCount = 0;
                    
                    foreach (var (x,y) in list)
                    {

                            typeMetaList.Add
                            (
                                new VariableTypeMeta(y,
                                    (VariableDeclaratorSyntax)x.TargetNode,
                                    (IFieldSymbol)x.TargetSymbol,
                                    x.Attributes,
                                    references)
                            );
                            typemetalistCount++;
                        
                        
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
                        groupCount++;
                    }


                }); 
        }
        

        static bool TryEmit(
            IGrouping<INamedTypeSymbol,VariableTypeMeta>? typeMetaGroup,
            CodeWriter codeWriter,
            ReferenceSymbols references,
            in SourceProductionContext context)
        {
            VariableTypeMeta[] variableTypeMetas = new VariableTypeMeta[] { };
            INamedTypeSymbol classSymbol = null;
            ClassDeclarationSyntax? classSyntax = null;
            var error = false;
            
            try
            {
                if (typeMetaGroup is not null)
                {
                    variableTypeMetas = typeMetaGroup.ToArray();
                    // 親クラスを取得
                    classSymbol = typeMetaGroup.Key;
                    classSyntax = typeMetaGroup.First().ClassSyntax;
                }
            
                if (classSymbol is null || classSyntax is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ClassNotFound,
                        variableTypeMetas.First().Syntax.GetLocation(),
                        String.Join("/",variableTypeMetas.Select(x => x.Syntax.ToString()))));
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
                var nameSpaceStr = nameSpaceIsGlobal ? "" : $"namespace {classSymbol.ContainingNamespace.ToDisplayString()}";
                var classAccessiblity = classSymbol?.DeclaredAccessibility.ToString().ToLower();
                
                codeWriter.AppendLine(nameSpaceStr);
                if(!nameSpaceIsGlobal) codeWriter.BeginBlock();
                
                codeWriter.AppendLine("// This class is generated by AutoPropertyGenerator.");
                codeWriter.AppendLine($"{classAccessiblity} partial class {classSymbol?.Name}");
                codeWriter.BeginBlock();
                
                foreach (var variableTypeMeta in variableTypeMetas)
                {
                    var className = variableTypeMeta?.TargetType?.ToDisplayString();
                    var sourceClassName = variableTypeMeta?.SourceType?.ToDisplayString();
                    if (variableTypeMeta?.TargetType?.Name is null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.VaribleNameNotFound,
                            variableTypeMeta.Syntax.GetLocation(),
                            String.Join("/",variableTypeMeta.Syntax.ToString())));
                        error = true;
                    }
                    var propertyName = GetPropertyName(variableTypeMeta?.Syntax.Identifier.ValueText);
                    bool typeIsSame = className == sourceClassName;
                    switch (variableTypeMeta.AXSArgument)
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
                                variableTypeMeta.Syntax.GetLocation(),
                                String.Join("/",variableTypeMeta.Syntax.ToString())));
                            error = true;
                            break;
                    }
                    
                    codeWriter.AppendLine($" {className} {propertyName}");
                    codeWriter.BeginBlock();
                    codeWriter.AppendLine("get");
                    codeWriter.BeginBlock();
                    
                    if (typeIsSame)
                    {
                        codeWriter.AppendLine($"return this.{variableTypeMeta.Symbol.Name};");
                        codeWriter.EndBlock();
                    }
                    else
                    {
                        codeWriter.AppendLine($"return ({className})this.{variableTypeMeta.Symbol.Name};");
                        codeWriter.EndBlock();
                    }

                    switch (variableTypeMeta.AXSArgument)
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
                                codeWriter.AppendLine($"this.{variableTypeMeta.Symbol.Name} = value;");
                                codeWriter.EndBlock();
                            }
                            else
                            {
                                codeWriter.AppendLine($"this.{variableTypeMeta.Symbol.Name} = ({sourceClassName})value;");
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
                                codeWriter.AppendLine($"this.{variableTypeMeta.Symbol.Name} = value;");
                                codeWriter.EndBlock();
                            }
                            else
                            {
                                codeWriter.AppendLine($"this.{variableTypeMeta.Symbol.Name} = ({sourceClassName})value;");
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

    class Comparer : IEqualityComparer<(GeneratorAttributeSyntaxContext, Compilation)>
    {
        public static readonly Comparer Instance = new();

        public bool Equals((GeneratorAttributeSyntaxContext, Compilation) x, (GeneratorAttributeSyntaxContext, Compilation) y)
        {
            return x.Item1.TargetNode.Equals(y.Item1.TargetNode);
        }

        public int GetHashCode((GeneratorAttributeSyntaxContext, Compilation) obj)
        {
            return obj.Item1.TargetNode.GetHashCode();
        }
    }
}