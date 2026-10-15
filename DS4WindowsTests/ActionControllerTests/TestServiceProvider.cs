using System;
using DS4Windows.Actions;
using DS4Windows.DS4Control;
using Xunit;

namespace DS4Windows.Actions.Tests
{
    // Lightweight test ServiceProvider that returns a single object for requested service types.
    class TestServiceProvider : IServiceProvider
    {
        private readonly object svc;
        public TestServiceProvider(object svc) { this.svc = svc; }
        public object GetService(Type serviceType)
        {
            if (svc != null && serviceType.IsInstanceOfType(svc)) return svc;
            return null;
        }
    }
}