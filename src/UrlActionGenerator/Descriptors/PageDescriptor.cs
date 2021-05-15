using System;
using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
{
    public class PageDescriptor
    {
        public PageDescriptor(PageAreaDescriptor area, string name, string pageHandler = null, List<ParameterDescriptor> parameters = null)
        {
            Area = area ?? throw new ArgumentNullException(nameof(area));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            PageHandler = string.IsNullOrWhiteSpace(pageHandler) ? null : pageHandler;
            Parameters = parameters ?? new List<ParameterDescriptor>();
        }

        public PageAreaDescriptor Area { get; }

        public string Name { get; }

        public string PageHandler { get; }

        public List<ParameterDescriptor> Parameters { get; }
    }
}
