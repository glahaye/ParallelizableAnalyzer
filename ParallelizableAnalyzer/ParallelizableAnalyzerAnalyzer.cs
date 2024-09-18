// The following blog entry is useful for understanding the code below:
// https://blog.emirosmanoski.mk/2020-11-02-Roslyn-Roslyn-Analyzer-Part2/

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelizableAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ParallelizableAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PARA01";

        private const string Title = "Method contains async tasks that might be parallelizable";
        private const string MessageFormat = "Method '{0}' contains async tasks that might be parallelizable";
        private const string Description = "Consider parallelizing execution of async tasks";
        private const string Category = "Parallelism";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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
        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, SyntaxNode methodCodeBlock)
        {
            SemanticModel semanticModel = context.SemanticModel;
            AwaitExpressionSyntax node = (AwaitExpressionSyntax)context.Node;

            var allAwaitExpressions = methodCodeBlock.DescendantNodes().OfType<AwaitExpressionSyntax>().ToList();

            // There are no await expressions in the method - We are done here
            if (allAwaitExpressions.Count == 0)
            {
                return;
            }

            // We have more than one await expression in the method
            if (allAwaitExpressions.Count > 1)
            {
                ReportDiagnosticOnNode(context, node);

                return;
            }

            // Also include single awaits located within loops
            SyntaxNode traversalNode = context.Node.Parent;
            while (!(traversalNode is MethodDeclarationSyntax))
            {
                if (traversalNode is ForStatementSyntax ||
                    traversalNode is ForEachStatementSyntax ||
                    traversalNode is WhileStatementSyntax ||
                    traversalNode is DoStatementSyntax)
                {
                    ReportDiagnosticOnNode(context, node);
                    break;
                }

                traversalNode = traversalNode.Parent;
            }
        }

        private static void ReportDiagnosticOnNode(SyntaxNodeAnalysisContext context, AwaitExpressionSyntax node)
        {
            var invocation = node.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            string methodName = invocation?.Expression.ToString();
            string argumentList = invocation.ArgumentList.Arguments.ToString();

            Diagnostic diagnostic = Diagnostic.Create(Rule, node.GetLocation(), methodName, argumentList);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
