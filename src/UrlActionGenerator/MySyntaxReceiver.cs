using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UrlActionGenerator
{
    public class MySyntaxReceiver : ISyntaxContextReceiver
    {
        private IMethodSymbol _disposableDispose;

        private readonly Dictionary<TypeDeclarationSyntax, INamedTypeSymbol> _controllerSyntaxes = new();
        private readonly HashSet<INamedTypeSymbol> _partialControllers = new(SymbolEqualityComparer.Default);

        public List<AttributeSyntax> AssemblyAttributes { get; } = new();
        public List<INamedTypeSymbol> Controllers { get; } = new();
        public Dictionary<INamedTypeSymbol, List<IMethodSymbol>> ControllerActions { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is AttributeListSyntax attributeList)
            {
                if (attributeList.Target?.Identifier.ToString() != "assembly")
                    return;

                AssemblyAttributes.AddRange(attributeList.Attributes);
            }
            else if (context.Node is TypeDeclarationSyntax classSyntax)
            {
                VisitTypeDeclarationSyntax(context, classSyntax);
            }
            else if (context.Node is MethodDeclarationSyntax methodSyntax)
            {
                VisitMethodDeclarationSyntax(context, methodSyntax);
            }
        }

        private void VisitTypeDeclarationSyntax(GeneratorSyntaxContext context, TypeDeclarationSyntax classSyntax)
        {
            if (MvcFacts.CanBeController(classSyntax))
            {
                var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classSyntax);
                if (symbol is INamedTypeSymbol namedTypeSymbol && MvcFacts.IsController(namedTypeSymbol))
                {
                    var isNewSymbol = !classSyntax.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword))
                                      || _partialControllers.Add(namedTypeSymbol);

                    if (isNewSymbol)
                    {
                        Controllers.Add(namedTypeSymbol);
                        _controllerSyntaxes[classSyntax] = namedTypeSymbol;
                    }
                }
            }
        }

        private void VisitMethodDeclarationSyntax(GeneratorSyntaxContext context, MethodDeclarationSyntax methodSyntax)
        {
            if (methodSyntax.Parent is TypeDeclarationSyntax typeSyntax && _controllerSyntaxes.TryGetValue(typeSyntax, out var classSymbol))
            {
                var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, methodSyntax);
                _disposableDispose ??= (IMethodSymbol)context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers(nameof(IDisposable.Dispose)).First();

                if (symbol is IMethodSymbol methodSymbol && MvcFacts.IsControllerAction(methodSymbol, _disposableDispose))
                {
                    if (!ControllerActions.TryGetValue(classSymbol, out var actions))
                    {
                        actions = new List<IMethodSymbol>();
                        ControllerActions[classSymbol] = actions;
                    }

                    actions.Add(methodSymbol);
                }
            }
        }
    }
}
