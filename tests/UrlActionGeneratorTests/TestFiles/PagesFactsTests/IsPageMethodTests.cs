using System;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UrlActionGeneratorTests.TestFiles.PagesFactsTests
{
    public abstract class OnGetTestIsPageMethodBase : PageModel
    {
        public abstract void OnGetAbstractMethod();

        public virtual void OnGetVirtualMethod() { }

        public virtual void OnGetMethodInBase() { }

        [NonHandler]
        public virtual void OnGetNonHandlerBase() { }
    }

    public class OnGetTestIsPageMethod : OnGetTestIsPageMethodBase, IDisposable
    {
        static OnGetTestIsPageMethod() { }

        OnGetTestIsPageMethod() { }

        public override void OnGetAbstractMethod() { }

        private void OnGetPrivateMethod() { }

        protected void OnGetProtectedMethod() { }

        internal void OnGetInternalMethod() { }

        public void OnGetGenericMethod<T>() { }

        public static void OnGetStaticMethod() { }

        [NonHandler]
        public void OnGetNonHandler() { }

        public override void OnGetNonHandlerBase() { }

        public void OnGetOrdinary() { }

        public void NoOnPrefix() { }

        public void OnTraceHandler() { }

        public void OnConnectHandler() { }

        public void Dispose() { }
    }
}
