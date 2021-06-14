using System;
using Microsoft.CodeAnalysis;
using UrlActionGenerator.Extensions;

namespace UrlActionGenerator.Descriptors
{
    public class ParameterDescriptor
    {
        public ParameterDescriptor(string name, ITypeSymbol type, bool hasDefaultValue, object defaultValue)
        {
            Name = name?.ToCamelCase() ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public ITypeSymbol Type { get; }

        public bool IsNullable => Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).EndsWith("?");

        public bool HasDefaultValue { get; }

        public object DefaultValue { get; }
    }
}
