using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator
{
    internal class PageData
    {
        private readonly string[] _pathParts;

        internal AdditionalText AdditionalText { get; }

        private string _sourceText;
        private string SourceText => _sourceText ??= AdditionalText.GetText().ToString();

        public PageData(AdditionalText additionalText)
        {
            _pathParts = additionalText.Path.Split('/', '\\');
            AdditionalText = additionalText;
        }

        public string Area
        {
            get
            {
                var areasIdx = Array.IndexOf(_pathParts, "Areas");
                if (areasIdx < 0) return null;
                if (_pathParts.Length < areasIdx + 3) return null;
                if (_pathParts[areasIdx + 2] != "Pages") return null;

                return _pathParts[areasIdx + 1];
            }
        }

        public string Page
        {
            get
            {
                var pagesIdx = Array.IndexOf(_pathParts, "Pages");
                if (pagesIdx < 0) return null;
                if (_pathParts.Length < pagesIdx + 2) return null;

                var page = "/" + string.Join("/", _pathParts.Skip(pagesIdx + 1));
                return page.Substring(0, page.Length - 7);
            }
        }

        public string PageName => Path.GetFileName(Page);

        public string Folder => Path.GetDirectoryName(Page);

        /// <summary>
        /// The route extracted from the @page directive in the source
        /// </summary>
        public string Route
        {
            get
            {
                var match = Regex.Match(SourceText, @"^\s*@page ""([^""]+)""", RegexOptions.Multiline);
                if (!match.Success) return null;
                return match.Groups[1].Value;
            }
        }

        /// <summary>
        /// The model extracted from the @model directive in the source
        /// </summary>
        public string Model
        {
            get
            {
                var match = Regex.Match(SourceText, @"^\s*@model ([\w\.]+)", RegexOptions.Multiline);
                if (!match.Success) return null;
                return match.Groups[1].Value;
            }
        }
    }
}
