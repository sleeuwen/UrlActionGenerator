using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using UrlActionGenerator;
using UrlActionGenerator.Descriptors;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class RouteDiscovererTests
    {
        public static object[][] DiscoverRouteParameters_Data
        {
            get
            {
                return new[]
                {
                    new object[] { "", Array.Empty<ParameterDescriptor>() },
                    new object[] { "/page", Array.Empty<ParameterDescriptor>() },
                    new object[] { "/{id}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.String"), false, null) } },
                    new object[] { "/{id:int}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Int32"), false, null) } },
                    new object[] { "/{id:int:min(1)}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Int32"), false, null) } },
                    new object[] { "/{id:min(1)}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Int32"), false, null) } },
                    new object[] { "/{id:min(1):max(4)}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Int32"), false, null) } },
                    new object[] { "/{id:minlength(1)}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.String"), false, null) } },
                    new object[] { "/{id:alpha}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.String"), false, null) } },
                    new object[] { "/{id:bool}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Boolean"), false, null) } },
                    new object[] { "/{id:long}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Int64"), false, null) } },
                    new object[] { "/{id:float}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Single"), false, null) } },
                    new object[] { "/{id:double}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Double"), false, null) } },
                    new object[] { "/{id:decimal}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Decimal"), false, null) } },
                    new object[] { "/{id:guid}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Guid"), false, null) } },
                    new object[] { "/{id:datetime}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.DateTime"), false, null) } },
                    new object[] { "/{id:int}/{other}", new[] { new ParameterDescriptor("id", TestCompilation.Default.GetTypeByMetadataName("System.Int32"), false, null), new ParameterDescriptor("other", TestCompilation.Default.GetTypeByMetadataName("System.String"), false, null) } },
                    new object[] { "/{culture:culture}", new[] { new ParameterDescriptor("culture", TestCompilation.Default.GetTypeByMetadataName("System.String"), false, null) } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DiscoverRouteParameters_Data))]
        public void DiscoverRouteParameters_ReturnsTheCorrectParameters(string route, IList<ParameterDescriptor> expectedParameters)
        {
            // Act
            var parameters = RouteDiscoverer.DiscoverRouteParameters(route, TestCompilation.Default).ToList();

            // Assert
            parameters.Should().BeEquivalentTo(expectedParameters);
        }
    }
}
