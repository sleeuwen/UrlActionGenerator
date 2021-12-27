//HintName: ExcludedTypeAttribute.cs
namespace UrlActionGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class ExcludedTypeAttribute : System.Attribute
    {
        public ExcludedTypeAttribute(System.Type type)
        {
        }
    }
}