//this code is edited from VYaml.SourceGenerator in https://github.com/hadashiA/VYaml?tab=MIT-1-ov-file

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

#pragma warning disable RS2008

using Microsoft.CodeAnalysis;

namespace Amenonegames.SourceGenerator;

static class DiagnosticDescriptors
{
    const string Category = "Amenonegames.AutoPropertyGenerator";

    public static readonly DiagnosticDescriptor UnexpectedErrorDescriptor = new(
        id: "AutoPropertyGenerator001",
        title: "Unexpected error during source code generation",
        messageFormat: "Unexpected error occurred during source code code generation: {0}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ClassNotFound = new(
        id: "AutoPropertyGenerator002",
        title: "Parent Class not found in AutoPropertyGenerator field",
        messageFormat: "Parent Class not found in AutoPropertyGenerator declaration '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "AutoPropertyGenerator003",
        title: "AutoPropertyGenerator class must be partial",
        messageFormat: "The Class using AutoPropertyGenerator '{0}' must be partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VaribleNameNotFound = new(
        id: "AutoPropertyGenerator004",
        title: "AutoPropertyGenerator variable name not found",
        messageFormat: "AutoPropertyGenerator variable name not found in '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor AXSNotFound = new(
        id: "AutoPropertyGenerator005",
        title: "AutoPropertyGenerator AXS not found",
        messageFormat: "AutoPropertyGenerator AXS not found in '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
}
