using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LiteDB.Generator;

[Generator]
public class LiteGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            ExecuteCore(context);
        }
        catch (Exception exception)
        {
            RaiseExceptionDiagnostic(context, exception);
        }
    }

    private void ExecuteCore(GeneratorExecutionContext context)
    {
        // adding AutoInterfaceAttribute to source code
        context.AddSource(
            Attributes.AutoInterfaceClassname,
            SourceText.From(Attributes.AttributesSourceCode, Encoding.UTF8));

        var codeBase = new CodeBase(context);

        InterfaceGen.GenerateCode(codeBase);
    }

    private static void RaiseExceptionDiagnostic(GeneratorExecutionContext context, Exception exception)
    {
        var descriptor = new DiagnosticDescriptor(
            "InterfaceGenerator.CriticalError",
            $"Exception thrown in InterfaceGenerator",
            $"{exception.GetType().FullName} {exception.Message} {exception.StackTrace.Trim()}",
            "InterfaceGenerator",
            DiagnosticSeverity.Error,
            true,
            customTags: WellKnownDiagnosticTags.AnalyzerException);

        var diagnostic = Diagnostic.Create(descriptor, null);
        
        context.ReportDiagnostic(diagnostic);
    }
}