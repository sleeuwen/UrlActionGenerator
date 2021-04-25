namespace UrlActionGenerator.Descriptors
{
    public record ParameterDescriptor
    {
        public ParameterDescriptor(string name, string type, bool hasDefaultValue, object defaultValue)
        {
            Name = name;
            Type = type;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public string Type { get; }

        public bool HasDefaultValue { get; }

        public object DefaultValue { get; }
    }
}
