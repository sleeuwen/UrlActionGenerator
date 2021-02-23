## UrlActionGenerator

[![NuGet Version](http://img.shields.io/nuget/v/UrlActionGenerator.svg?style=flat)](https://www.nuget.org/packages/UrlActionGenerator/) 
[![Coverage Status](https://coveralls.io/repos/github/sleeuwen/UrlActionGenerator/badge.svg?branch=master)](https://coveralls.io/github/sleeuwen/UrlActionGenerator?branch=master)

UrlActionGenerator is a C# Source Generator that assists the generation of URL's in ASP.NET Core projects
and enforces the existance of a controller and method for the referenced URL.

### What is UrlActionGenerator

UrlActionGenerator is a C# Source Generator that runs as part of the compilation process. Every time
your project is being compiled, the compiler calls into UrlActionGenerator with the sources of your project.
Using the same set of rules used by ASP.NET Core, UrlActionGenerator will figure out what all the controller
classes are in your project and the actions.

Using the list of controllers and actions, some extra classes are generated as extension methods on the
`IUrlHelper` class to generate type-safe methods for all controller actions in your project. These methods 
will contain the same parameters as your action methods so you can set them like you would be calling the
method. The standard `IUrlHelper.Action("ActionName", "ControllerName")` method will then be used to generate
the actual url, so if you use any custom `[Route]` or `[HttpGet("url")]` attributes, they will still work the
same way as if you were using the `IUrlHelper.Action` method yourself.

### How to use

To start using this package, simply add the `UrlActionGenerator` package to your ASP.NET Core project by
using the IDE tooling or running the below command:

    dotnet add package UrlActionGenerator

After that, your controllers and actions are automatically discovered and intellisense should almost directly be
available in your controllers and views on the `IUrlHelper`.

UrlActionGenerator automatically generates extension methods on the `IUrlHelper`, the default method is `Actions()`
which returns a class with properties for all the controllers in your project, those properties return another
class with all the actions available in the controller and the parameters on those actions. If you use Area's in your
project there are some extra extensions methods generated, one for each area. The name of those extension methods
are `{AreaName}Actions()`, so for an "Admin" area it will be `AdminActions()`.

### Examples

Below are some examples on the methods that are generated in what scenario, how you can use it and what the result is.
First off, a simple controller action without any parameters:

**Controller:**
```csharp
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
```

**Usage:**
```
Url.Actions().Home.Index()
```

**Result:**
```
/Home/Index
```

-----

A simple controller action with parameters and default parameter values

**Controller:**
```csharp
public class HomeController : Controller
{
    public IActionResult Search(int page, int pageSize = 20)
    {
        return View();
    }
}
```

**Usage:**
```
Url.Actions().Home.Search(1);
Url.Actions().Home.Search(2, 50);
```

**Result:**
```
/Home/Search?page=1
/Home/Search?page=2&pageSize=50
```

-----

Use with controllers in areas

**Controller:**
```csharp
[Area("Admin")]
public class HomeController : Controller
{
    public IActionResult Index(string str)
    {
        return View();
    }
}
```

**Usage:**
```
Url.AdminActions().Home.Index("value");
```

**Result:**
```
/Admin/Home/Index?str=value
```
