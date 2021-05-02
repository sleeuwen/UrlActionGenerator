using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UrlActionGenerator
{
    public class MySyntaxReceiver : ISyntaxReceiver
    {
        public List<TypeDeclarationSyntax> PossibleControllers { get; } = new List<TypeDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not TypeDeclarationSyntax classSyntax)
                return;

            if (MvcFacts.CanBeController(classSyntax))
            {
                PossibleControllers.Add(classSyntax);
            }
        }
    }
}
