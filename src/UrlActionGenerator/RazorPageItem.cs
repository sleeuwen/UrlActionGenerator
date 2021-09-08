using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UrlActionGenerator
{
    internal class RazorPageItem
    {
        private static readonly Regex _pageRouteRegex = new Regex(@"^\s*@page ""([^""]+)""", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _pageModelRegex = new Regex(@"^\s*@model ([\w\.]+)", RegexOptions.Compiled | RegexOptions.Multiline);

        private readonly string[] _pathParts;

        internal AdditionalText AdditionalText { get; }

        private string _sourceText;
        private string SourceText => _sourceText ??= AdditionalText.GetText().ToString();

        public RazorPageItem(AdditionalText additionalText)
        {
            _pathParts = additionalText.Path.Split('/', '\\');
            AdditionalText = additionalText;
        }

        public bool IsPage => SourceText.Contains("@page");

        private bool _areaInitialized;
        private string _area;
        public string Area
        {
            get
            {
                if (!_areaInitialized)
                {
                    var areasIdx = Array.IndexOf(_pathParts, "Areas");
                    if (areasIdx < 0)
                        _area = null;
                    else if (_pathParts.Length < areasIdx + 3)
                        _area = null;
                    else if (_pathParts[areasIdx + 2] != "Pages")
                        _area = null;
                    else
                        _area = _pathParts[areasIdx + 1];
                }

                return _area;
            }
        }

        private bool _pageInitialized;
        private string _page;
        public string Page
        {
            get
            {
                if (!_pageInitialized)
                {
                    var pagesIdx = Array.IndexOf(_pathParts, "Pages");
                    if (pagesIdx < 0)
                        _page = null;
                    else if (_pathParts.Length < pagesIdx + 2)
                        _page = null;
                    else
                    {
                        var page = "/" + string.Join("/", _pathParts.Skip(pagesIdx + 1));
                        _page = page.Substring(0, page.Length - 7);
                    }

                    _pageInitialized = true;
                }

                return _page;
            }
        }

        public string PageName => Path.GetFileName(Page);

        public string Folder => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.GetDirectoryName(Page)?.Replace('\\', '/')
            : Path.GetDirectoryName(Page);

        private bool _routeInitialized;
        private string _route;
        /// <summary>
        /// The route extracted from the @page directive in the source
        /// </summary>
        public string Route
        {
            get
            {
                if (!_routeInitialized)
                {
                    var match = _pageRouteRegex.Match(SourceText);
                    _route = !match.Success ? null : match.Groups[1].Value;
                    _routeInitialized = true;
                }

                return _route;
            }
        }

        private bool _modelInitialized;
        private string _model;
        /// <summary>
        /// The model extracted from the @model directive in the source
        /// </summary>
        public string Model
        {
            get
            {
                if (!_modelInitialized)
                {
                    var match = _pageModelRegex.Match(SourceText);
                    _model = !match.Success ? null : match.Groups[1].Value;
                    _modelInitialized = true;
                }

                return _model;
            }
        }
    }
}
