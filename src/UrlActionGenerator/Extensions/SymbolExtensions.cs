using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator.Extensions
{
    internal static class SymbolExtensions
    {
        public static string GetFullNamespacedName(this INamespaceOrTypeSymbol symbol)
        {
            var fullName = new StringBuilder();

            while (symbol != null && symbol.Kind != SymbolKind.ErrorType)
            {
                if (!string.IsNullOrEmpty(symbol.Name))
                {
                    if (fullName.Length > 0)
                        fullName.Insert(0, '.');
                    fullName.Insert(0, symbol.Name);
                }

                symbol = symbol.ContainingSymbol as INamespaceOrTypeSymbol;
            }

            return fullName.ToString();
        }

        public static string GetTypeName(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol is IArrayTypeSymbol arrayType)
                return $"{GetTypeName(arrayType.ElementType)}[]";

            var rootType = typeSymbol.GetSimpleTypeName();

            if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
            {
                rootType += "<";
                rootType += string.Join(", ", namedType.TypeArguments.Select(GetTypeName));
                rootType += ">";
            }

            return rootType;
        }

        internal static string GetSimpleTypeName(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.GetFullNamespacedName() switch
            {
                "System.String" => "string",
                "System.Byte" => "byte",
                "System.SByte" => "sbyte",
                "System.Char" => "char",
                "System.Int16" => "short",
                "System.Int32" => "int",
                "System.Int64" => "long",
                "System.UInt16" => "ushort",
                "System.UInt32" => "uint",
                "System.UInt64" => "ulong",
                "System.Boolean" => "bool",
                "System.Decimal" => "decimal",
                "System.Double" => "double",
                "System.Single" => "float",
                "System.Object" => "object",
                var type => type,
            };
        }
    }
}
