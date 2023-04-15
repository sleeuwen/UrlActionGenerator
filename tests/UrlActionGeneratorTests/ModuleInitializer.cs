using System.Runtime.CompilerServices;
using VerifyTests;

namespace UrlActionGeneratorTests
{
    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifySourceGenerators.Initialize();
        }
    }
}
