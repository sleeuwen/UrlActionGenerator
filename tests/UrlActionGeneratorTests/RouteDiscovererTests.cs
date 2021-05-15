using System;
using System.Collections.Generic;
using FluentAssertions;
using UrlActionGenerator;
using UrlActionGenerator.Descriptors;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class RouteDiscovererTests
    {
        public static object[][] DiscoverRouteParameters_Data => new[]
        {
            new object[] { "", Array.Empty<ParameterDescriptor>() },
            new object[] { "/page", Array.Empty<ParameterDescriptor>() },
            new object[] { "/{id}", new[] { new ParameterDescriptor("id", "string", false, null) } },
            new object[] { "/{id:int}", new[] { new ParameterDescriptor("id", "int", false, null) } },
            new object[] { "/{id:int:min(1)}", new[] { new ParameterDescriptor("id", "int", false, null) } },
            new object[] { "/{id:min(1)}", new[] { new ParameterDescriptor("id", "int", false, null) } },
            new object[] { "/{id:min(1):max(4)}", new[] { new ParameterDescriptor("id", "int", false, null) } },
            new object[] { "/{id:minlength(1)}", new[] { new ParameterDescriptor("id", "string", false, null) } },
            new object[] { "/{id:alpha}", new[] { new ParameterDescriptor("id", "string", false, null) } },
            new object[] { "/{id:bool}", new[] { new ParameterDescriptor("id", "bool", false, null) } },
            new object[] { "/{id:long}", new[] { new ParameterDescriptor("id", "long", false, null) } },
            new object[] { "/{id:float}", new[] { new ParameterDescriptor("id", "float", false, null) } },
            new object[] { "/{id:double}", new[] { new ParameterDescriptor("id", "double", false, null) } },
            new object[] { "/{id:decimal}", new[] { new ParameterDescriptor("id", "decimal", false, null) } },
            new object[] { "/{id:guid}", new[] { new ParameterDescriptor("id", "System.Guid", false, null) } },
            new object[] { "/{id:datetime}", new[] { new ParameterDescriptor("id", "System.DateTime", false, null) } },
            new object[] { "/{id:int}/{other}", new[] { new ParameterDescriptor("id", "int", false, null), new ParameterDescriptor("other", "string", false, null) } },
            new object[] { "/{culture:culture}", new[] { new ParameterDescriptor("culture", "string", false, null) } },
        };

        [Theory]
        [MemberData(nameof(DiscoverRouteParameters_Data))]
        public void DiscoverRouteParameters_ReturnsTheCorrectParameters(string route, IEnumerable<ParameterDescriptor> expectedParameters)
        {
            // Act
            var parameters = RouteDiscoverer.DiscoverRouteParameters(route);

            // Assert
            parameters.Should().BeEquivalentTo(expectedParameters);
        }
    }
}
