## UrlActionGenerator

[![NuGet Version](http://img.shields.io/nuget/v/UrlActionGenerator.svg?style=flat)](https://www.nuget.org/packages/UrlActionGenerator/) 
[![Coverage Status](https://coveralls.io/repos/github/sleeuwen/UrlActionGenerator/badge.svg?branch=master)](https://coveralls.io/github/sleeuwen/UrlActionGenerator?branch=master)

UrlActionGenerator is a C# Source Generator for ASP.NET Core apps that create
strongly typed extension methods for the generation of URL's in ASP.NET Core
projects.

So instead of writing this by using magical strings:
```razor
<a asp-controller="Home" asp-action="Index" asp-route-param="4">Link</a>
```

You can write the following with the advantages of autocomplete and strong types:
```razor
<a href="@Url.Actions().Home.Index(param: 4)">Link</a>
```

### Installation

To start using this package, simply add the `UrlActionGenerator` package to
your ASP.NET Core project by using the IDE tooling or running the below
command:

```bash
dotnet add package UrlActionGenerator
```

### How to use

Once the package is added, extension methods are automatically added to the
`IUrlHelper` interface based on the following template:

For MVC:
```C#
IUrlHelper.Actions().[ControllerName].[ActionName](...parameters)
IUrlHelper.[AreaName]Actions().[ControllerName].[ActionName](...parameters)
```

For Razor Pages:
```C#
IUrlHelper.Pages().Folder.Page(...parameters)
IUrlHelper.[AreaName]Pages().Folder.Page(...parameters)
```

For more examples check out the [wiki](https://github.com/sleeuwen/UrlActionGenerator/wiki)
for [MVC examples](https://github.com/sleeuwen/UrlActionGenerator/wiki/MVC-Examples)
or [Razor Pages examples](https://github.com/sleeuwen/UrlActionGenerator/wiki/Razor-Pages-Examples).

Interested in contributing? check out the [technical details](https://github.com/sleeuwen/UrlActionGenerator/wiki/How-it-works).
