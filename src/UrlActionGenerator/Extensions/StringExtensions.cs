namespace UrlActionGenerator.Extensions
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (!char.IsUpper(str[0]))
                return str;

            return char.ToLower(str[0]) + str.Substring(1);
        }
    }
}
