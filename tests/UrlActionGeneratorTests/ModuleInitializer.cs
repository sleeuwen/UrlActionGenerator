using System.Runtime.CompilerServices;
using Microsoft;
using VerifyTests;

namespace UrlActionGeneratorTests
{
    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifySourceGenerators.Enable();
        }
    }
}
