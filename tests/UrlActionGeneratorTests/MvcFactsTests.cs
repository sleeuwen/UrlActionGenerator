using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UrlActionGenerator;
using UrlActionGeneratorTests.TestFiles.MvcFactsTests;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class MvcFactsTests
    {
        private static readonly Type TestIsControllerActionType = typeof(TestIsControllerAction);

        #region CanBeController
        [Fact]
        public void CanBeController_ReturnsFalseForInterfaces() => CanBeControllerReturnsFalse(typeof(ITestController));

        [Fact]
        public void CanBeController_ReturnsFalseForAbstractTypes() => CanBeControllerReturnsFalse(typeof(AbstractController));

        [Fact]
        public void CanBeController_ReturnsFalseForValueType() => CanBeControllerReturnsFalse(typeof(ValueTypeController));

        [Fact]
        public void CanBeController_ReturnsFalseForGenericType() => CanBeControllerReturnsFalse(typeof(OpenGenericController<>));

        [Fact]
        public void CanBeController_ReturnsFalseForPocoType() => CanBeControllerReturnsFalse(typeof(PocoType));

        [Fact]
        public void CanBeController_ReturnsTrueForTypeDerivedFromPocoType() => CanBeControllerReturnsTrue(typeof(DerivedPocoType));

        [Fact]
        public void CanBeController_ReturnsTrueForTypeDerivingFromController() => CanBeControllerReturnsTrue(typeof(TypeDerivingFromController));

        [Fact]
        public void CanBeController_ReturnsTrueForTypeDerivingFromControllerBase() => CanBeControllerReturnsTrue(typeof(TypeDerivingFromControllerBase));

        [Fact]
        public void CanBeController_ReturnsTrueForTypeDerivingFromController_WithoutSuffix() => CanBeControllerReturnsTrue(typeof(NoSuffix));

        [Fact]
        public void CanBeController_ReturnsFalseForTypeWithSuffix_ThatIsNotDerivedFromController() => CanBeControllerReturnsFalse(typeof(PocoController));

        [Fact]
        public void CanBeController_ReturnsTrueForTypeWithoutSuffix_WithControllerAttribute() => CanBeControllerReturnsTrue(typeof(CustomBase));

        [Fact]
        public void CanBeController_ReturnsTrueForTypeDerivingFromCustomBaseThatHasControllerAttribute() => CanBeControllerReturnsTrue(typeof(ChildOfCustomBase));

        [Fact]
        public void CanBeController_ReturnsTrueForTypeWithNonControllerAttribute() => CanBeControllerReturnsTrue(typeof(BaseNonController));

        [Fact]
        public void CanBeController_ReturnsTrueForTypesDerivingFromTypeWithNonControllerAttribute() => CanBeControllerReturnsTrue(typeof(BasePocoNonControllerChildController));

        [Fact]
        public void CanBeController_ReturnsTrueForTypesDerivingFromTypeWithNonControllerAttributeWithControllerAttribute() =>
            CanBeControllerReturnsTrue(typeof(ControllerAttributeDerivingFromNonController));

        [Fact]
        public void CanBeController_ReturnsFalseForInternalControllerType() => CanBeControllerReturnsFalse(typeof(InternalController));

        [Fact]
        public void CanBeController_ReturnsFalseForPrivateControllerType() => CanBeControllerReturnsFalse(typeof(PrivateController));

        [Fact]
        public void CanBeController_ReturnsFalseForNestedControllerType() => CanBeControllerReturnsFalse(typeof(ParentClass.NestedController));

        private void CanBeControllerReturnsFalse(Type type)
        {
            var compilation = GetIsControllerCompilation();
            var syntaxTree = compilation.GetTypeByMetadataName(type.FullName).DeclaringSyntaxReferences.Single().SyntaxTree;

            var typeName = type.IsGenericType ? type.Name.Substring(0, type.Name.IndexOf('`')) : type.Name;
            var typeDeclarationSyntax = syntaxTree.GetRoot().DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .First(node => node.Identifier.ToString() == typeName);

            // Act
            var canBeController = MvcFacts.CanBeController(typeDeclarationSyntax);

            // Assert
            Assert.False(canBeController);
        }

        private void CanBeControllerReturnsTrue(Type type)
        {
            var compilation = GetIsControllerCompilation();
            var syntaxTree = compilation.GetTypeByMetadataName(type.FullName).DeclaringSyntaxReferences.Single().SyntaxTree;

            var typeName = type.IsGenericType ? type.Name.Substring(0, type.Name.IndexOf('`')) : type.Name;
            var typeDeclarationSyntax = syntaxTree.GetRoot().DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .First(node => node.Identifier.ToString() == typeName);

            // Act
            var canBeController = MvcFacts.CanBeController(typeDeclarationSyntax);

            // Assert
            Assert.True(canBeController);
        }

        #endregion CanBeController

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

        private void IsControllerReturnsFalse(Type type)
        {
            var compilation = GetIsControllerCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);

            // Act
            var isController = MvcFacts.IsController(typeSymbol);

            // Assert
            Assert.False(isController);
        }

        private void IsControllerReturnsTrue(Type type)
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
