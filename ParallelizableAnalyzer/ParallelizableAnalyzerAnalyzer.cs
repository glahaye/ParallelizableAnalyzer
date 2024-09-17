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
        public const string DiagnosticId = "ParallelizableAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
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

            // TODO: ignore await Task.WhenAll() instances
            // TODO: also include when we only have one await expression that is in a loop
            // TODO: ignore awaited tasks whose outputs chain to another awaited task
            if (allAwaitExpressions.Count > 1)
            {
                var invocation = node.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                string methodName = invocation?.Expression.ToString();
                string argumentList = invocation.ArgumentList.Arguments.ToString();

                Diagnostic diagnostic = Diagnostic.Create(Rule, node.GetLocation(), methodName, argumentList);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
