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

using Microsoft.CodeAnalysis;

namespace Amenonegames.SourceGenerator;

public class ReferenceSymbols
{
    public static ReferenceSymbols? Create(Compilation compilation)
    {
        var yamlObjectAttribute = compilation.GetTypeByMetadataName("VYaml.Annotations.YamlObjectAttribute");
        if (yamlObjectAttribute is null)
            return null;

        return new ReferenceSymbols
        {
            YamlObjectAttribute = yamlObjectAttribute,
            YamlMemberAttribute = compilation.GetTypeByMetadataName("VYaml.Annotations.YamlMemberAttribute")!,
            YamlIgnoreAttribute = compilation.GetTypeByMetadataName("VYaml.Annotations.YamlIgnoreAttribute")!,
            YamlConstructorAttribute = compilation.GetTypeByMetadataName("VYaml.Annotations.YamlConstructorAttribute")!,
            YamlObjectUnionAttribute = compilation.GetTypeByMetadataName("VYaml.Annotations.YamlObjectUnionAttribute")!,
            NamingConventionEnum = compilation.GetTypeByMetadataName("VYaml.Annotations.NamingConvention")!
        };
    }

    public INamedTypeSymbol YamlObjectAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlMemberAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlIgnoreAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlConstructorAttribute { get; private set; } = default!;
    public INamedTypeSymbol YamlObjectUnionAttribute { get; private set; } = default!;
    public INamedTypeSymbol NamingConventionEnum { get; private set; } = default!;
}
