// The following blog entry is useful for understanding the code below:
// https://blog.emirosmanoski.mk/2020-11-02-Roslyn-Roslyn-Analyzer-Part2/

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelizableAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ParallelizableAnalyzerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PARA01";

    private const string Title = "Method contains async tasks that might be parallelizable";
    private const string MessageFormat = "Method '{0}' contains async tasks that might be parallelizable";
    private const string Description = "Consider parallelizing execution of async tasks.";
    private const string Category = "Parallelism";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCodeBlockStartAction<SyntaxKind>(analysisContext =>
        {
            if (analysisContext.OwningSymbol.Kind != SymbolKind.Method)
            {
                return;
            }

            analysisContext.RegisterSyntaxNodeAction(
               ctx => AnalyzeSyntaxNode(ctx, analysisContext.CodeBlock), SyntaxKind.AwaitExpression);
        });
    }

    /// <summary>
    /// Analyzes a method code block for await expressions.
    /// </summary>
    // TODO: ignore some await Task.WhenAll() instances
    // TODO: ignore awaited tasks whose outputs chain to another awaited task
    private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, SyntaxNode methodCodeBlock)
    {
        SemanticModel semanticModel = context.SemanticModel;
        AwaitExpressionSyntax node = (AwaitExpressionSyntax)context.Node;

        var allAwaitExpressions = methodCodeBlock.DescendantNodes().OfType<AwaitExpressionSyntax>().ToList();

        // Only do the analysis and potentially report the method in diagnostics once per method.
        // Otherwise, we could report the same method multiple times.
        if (node != allAwaitExpressions[0])
        {
            return;
        }

        // We have more than one await expression in the method
        if (allAwaitExpressions.Count > 1)
        {
            ReportDiagnosticOnEnclosingMethodNode(context, node);

            return;
        }

        // Also mark methods where we have single awaits located within loops
        foreach (AwaitExpressionSyntax awaitExpression in allAwaitExpressions)
        {
            SyntaxNode traversalNode = awaitExpression.Parent;
            while (traversalNode is not MethodDeclarationSyntax &&
                   traversalNode is not ConstructorDeclarationSyntax) // Yes, some people await in constructors
            {
                if (traversalNode is ForStatementSyntax ||
                    traversalNode is ForEachStatementSyntax ||
                    traversalNode is WhileStatementSyntax ||
                    traversalNode is DoStatementSyntax)
                {
                    ReportDiagnosticOnEnclosingMethodNode(context, node);
                    return;
                }

                traversalNode = traversalNode.Parent;
            }
        }
    }

    private static void ReportDiagnosticOnEnclosingMethodNode(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        while (node is not MethodDeclarationSyntax &&
               node is not ConstructorDeclarationSyntax)
        {
            node = node.Parent;
        }

        string methodName = null;
        Location location = null;
        if (node is MethodDeclarationSyntax methodDeclarationNode)
        {
            methodName = methodDeclarationNode.Identifier.ValueText;
            location = methodDeclarationNode.Identifier.GetLocation();
        }
        else if (node is ConstructorDeclarationSyntax constructorDeclarationNode)
        {
            methodName = constructorDeclarationNode.Identifier.ValueText;
            location = constructorDeclarationNode.Identifier.GetLocation();
        }

        Diagnostic diagnostic = Diagnostic.Create(Rule, location, methodName);

        context.ReportDiagnostic(diagnostic);
    }
}
