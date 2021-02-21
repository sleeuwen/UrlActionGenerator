## UrlActionGenerator

UrlActionGenerator is a C# Source Generator that assists the generation of URL's in ASP.NET Core projects
and enforces the existance of a controller and method for the referenced URL.

### Getting Started

To start using this package, simply add the `UrlActionGenerator` package to your ASP.NET Core project.
After the package is installed, it will automatically find your controllers and add extension methods to
the `IUrlHelper` class, which is available in Controllers and Razor views through the `Url` property.

### Examples

How to use with a simple controller/action combo:

**Controller:**
```
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

An action with parameters

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
Url.Actions().Home.Search(2, pageSize: 50);
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


### How it works

UrlActionGenerator is a C# Source Generator that runs as part of the compilation process. Every time
your project is being compiled, the sources are passed into UrlActionGenerator which in turn uses these
sources to search for controllers and actions.

When there are actions found in the project, a new _generated_ class is added to the compilation with
the extension methods on `IUrlHelper`.
