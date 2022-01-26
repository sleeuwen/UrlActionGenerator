//HintName: ExcludedParameterTypeAttribute.cs
namespace UrlActionGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class ExcludedParameterTypeAttribute : System.Attribute
    {
        public ExcludedParameterTypeAttribute(System.Type type)
        {
        }
    }
}