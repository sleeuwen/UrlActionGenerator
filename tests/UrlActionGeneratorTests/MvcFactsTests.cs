using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UrlActionGenerator;
using UrlActionGeneratorTests.TestFiles.MvcFactsTests;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class MvcFactsTests
    {
        private static readonly Type TestIsControllerActionType = typeof(TestIsControllerAction);

        #region IsController
        [Fact]
        public void IsController_ReturnsFalseForInterfaces() => IsControllerReturnsFalse(typeof(ITestController));

        [Fact]
        public void IsController_ReturnsFalseForAbstractTypes() => IsControllerReturnsFalse(typeof(AbstractController));

        [Fact]
        public void IsController_ReturnsFalseForValueType() => IsControllerReturnsFalse(typeof(ValueTypeController));

        [Fact]
        public void IsController_ReturnsFalseForGenericType() => IsControllerReturnsFalse(typeof(OpenGenericController<>));

        [Fact]
        public void IsController_ReturnsFalseForPocoType() => IsControllerReturnsFalse(typeof(PocoType));

        [Fact]
        public void IsController_ReturnsFalseForTypeDerivedFromPocoType() => IsControllerReturnsFalse(typeof(DerivedPocoType));

        [Fact]
        public void IsController_ReturnsTrueForTypeDerivingFromController() => IsControllerReturnsTrue(typeof(TypeDerivingFromController));

        [Fact]
        public void IsController_ReturnsTrueForTypeDerivingFromControllerBase() => IsControllerReturnsTrue(typeof(TypeDerivingFromControllerBase));

        [Fact]
        public void IsController_ReturnsTrueForTypeDerivingFromController_WithoutSuffix() => IsControllerReturnsTrue(typeof(NoSuffix));

        [Fact]
        public void IsController_ReturnsTrueForTypeWithSuffix_ThatIsNotDerivedFromController() => IsControllerReturnsTrue(typeof(PocoController));

        [Fact]
        public void IsController_ReturnsTrueForTypeWithoutSuffix_WithControllerAttribute() => IsControllerReturnsTrue(typeof(CustomBase));

        [Fact]
        public void IsController_ReturnsTrueForTypeDerivingFromCustomBaseThatHasControllerAttribute() => IsControllerReturnsTrue(typeof(ChildOfCustomBase));

        [Fact]
        public void IsController_ReturnsFalseForTypeWithNonControllerAttribute() => IsControllerReturnsFalse(typeof(BaseNonController));

        [Fact]
        public void IsController_ReturnsFalseForTypesDerivingFromTypeWithNonControllerAttribute() => IsControllerReturnsFalse(typeof(BasePocoNonControllerChildController));

        [Fact]
        public void IsController_ReturnsFalseForTypesDerivingFromTypeWithNonControllerAttributeWithControllerAttribute() =>
            IsControllerReturnsFalse(typeof(ControllerAttributeDerivingFromNonController));

        private async void IsControllerReturnsFalse(Type type)
        {
            var compilation = GetIsControllerCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);

            // Act
            var isController = MvcFacts.IsController(typeSymbol);

            // Assert
            Assert.False(isController);
        }

        private async void IsControllerReturnsTrue(Type type)
        {
            var compilation = GetIsControllerCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);

            // Act
            var isController = MvcFacts.IsController(typeSymbol);

            // Assert
            Assert.True(isController);
        }

        #endregion

        #region IsControllerAction
        [Fact]
        public void IsAction_ReturnsFalseForConstructor() => IsActionReturnsFalse(TestIsControllerActionType, ".ctor");

        [Fact]
        public void IsAction_ReturnsFalseForStaticConstructor() => IsActionReturnsFalse(TestIsControllerActionType, ".cctor");

        [Fact]
        public void IsAction_ReturnsFalseForPrivateMethod() => IsActionReturnsFalse(TestIsControllerActionType, "PrivateMethod");

        [Fact]
        public void IsAction_ReturnsFalseForProtectedMethod() => IsActionReturnsFalse(TestIsControllerActionType, "ProtectedMethod");

        [Fact]
        public void IsAction_ReturnsFalseForInternalMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.InternalMethod));

        [Fact]
        public void IsAction_ReturnsFalseForGenericMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.GenericMethod));

        [Fact]
        public void IsAction_ReturnsFalseForStaticMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.StaticMethod));

        [Fact]
        public void IsAction_ReturnsFalseForNonActionMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.NonAction));

        [Fact]
        public void IsAction_ReturnsFalseForOverriddenNonActionMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.NonActionBase));

        [Fact]
        public void IsAction_ReturnsFalseForDisposableDispose() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.Dispose));

        [Fact]
        public void IsAction_ReturnsFalseForExplicitDisposableDispose() => IsActionReturnsFalse(typeof(ExplicitIDisposable), "System.IDisposable.Dispose");

        [Fact]
        public void IsAction_ReturnsFalseForAbstractMethods() => IsActionReturnsFalse(typeof(TestIsControllerActionBase), nameof(TestIsControllerActionBase.AbstractMethod));

        [Fact]
        public void IsAction_ReturnsFalseForObjectEquals() => IsActionReturnsFalse(typeof(object), nameof(object.Equals));

        [Fact]
        public void IsAction_ReturnsFalseForObjectHashCode() => IsActionReturnsFalse(typeof(object), nameof(object.GetHashCode));

        [Fact]
        public void IsAction_ReturnsFalseForObjectToString() => IsActionReturnsFalse(typeof(object), nameof(object.ToString));

        [Fact]
        public void IsAction_ReturnsFalseForOverriddenObjectEquals() =>
            IsActionReturnsFalse(typeof(OverridesObjectMethods), nameof(OverridesObjectMethods.Equals));

        [Fact]
        public void IsAction_ReturnsFalseForOverriddenObjectHashCode() =>
            IsActionReturnsFalse(typeof(OverridesObjectMethods), nameof(OverridesObjectMethods.GetHashCode));

        private void IsActionReturnsFalse(Type type, string methodName)
        {
            var compilation = GetIsControllerActionCompilation();
            var disposableDispose = GetDisposableDispose(compilation);
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);
            var method = (IMethodSymbol)typeSymbol.GetMembers(methodName).First();

            // Act
            var isControllerAction = MvcFacts.IsControllerAction(method, disposableDispose);

            // Assert
            Assert.False(isControllerAction);
        }

        [Fact]
        public void IsAction_ReturnsTrueForNewMethodsOfObject() => IsActionReturnsTrue(typeof(OverridesObjectMethods), nameof(OverridesObjectMethods.ToString));

        [Fact]
        public void IsAction_ReturnsTrueForNotDisposableDispose() => IsActionReturnsTrue(typeof(NotDisposable), nameof(NotDisposable.Dispose));

        [Fact]
        public void IsAction_ReturnsTrueForNotDisposableDisposeOnTypeWithExplicitImplementation() =>
            IsActionReturnsTrue(typeof(NotDisposableWithExplicitImplementation), nameof(NotDisposableWithExplicitImplementation.Dispose));

        [Fact]
        public void IsAction_ReturnsTrueForOrdinaryAction() => IsActionReturnsTrue(TestIsControllerActionType, nameof(TestIsControllerAction.Ordinary));

        [Fact]
        public void IsAction_ReturnsTrueForOverriddenMethod() => IsActionReturnsTrue(TestIsControllerActionType, nameof(TestIsControllerAction.AbstractMethod));

        [Fact]
        public void IsAction_ReturnsTrueForNotDisposableDisposeOnTypeWithImplicitImplementation()
        {
            var compilation = GetIsControllerActionCompilation();
            var disposableDispose = GetDisposableDispose(compilation);
            var typeSymbol = compilation.GetTypeByMetadataName(typeof(NotDisposableWithDisposeThatIsNotInterfaceContract).FullName);
            var method = typeSymbol.GetMembers(nameof(IDisposable.Dispose)).OfType<IMethodSymbol>().First(f => !f.ReturnsVoid);

            // Act
            var isControllerAction = MvcFacts.IsControllerAction(method, disposableDispose);

            // Assert
            Assert.True(isControllerAction);
        }

        private void IsActionReturnsTrue(Type type, string methodName)
        {
            var compilation = GetIsControllerActionCompilation();
            var disposableDispose = GetDisposableDispose(compilation);
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);
            var method = (IMethodSymbol)typeSymbol.GetMembers(methodName).First();

            // Act
            var isControllerAction = MvcFacts.IsControllerAction(method, disposableDispose);

            // Assert
            Assert.True(isControllerAction);
        }

        private IMethodSymbol GetDisposableDispose(Compilation compilation)
        {
            var type = compilation.GetSpecialType(SpecialType.System_IDisposable);
            return (IMethodSymbol)type.GetMembers(nameof(IDisposable.Dispose)).First();
        }
        #endregion


        private Compilation GetIsControllerCompilation() => GetCompilation("IsControllerTests");

        private Compilation GetIsControllerActionCompilation() => GetCompilation("IsControllerActionTests");

        private Compilation GetCompilation(string testMethod)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "TestFiles", GetType().Name, testMethod + ".cs");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"TestFile {testMethod} could not be found at {filePath}.", filePath);

            var testSource = File.ReadAllText(filePath);

            var compilation = CreateCompilation(testSource);
            return compilation;
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[]
                {
                    MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ValueTuple<>).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Controller).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(AreaAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(RouteValueAttribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IUrlHelper).Assembly.Location),
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
