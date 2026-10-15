using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DS4Windows;

namespace DS4Windows
{
    public static class AppHost
    {
        private static readonly System.Collections.Generic.HashSet<System.Type> _loggedResolvedTypes = new System.Collections.Generic.HashSet<System.Type>();
        public static Microsoft.Extensions.Hosting.IHost Host => DS4WinWPF.AppHost.Host;
        public static Microsoft.Extensions.Hosting.IHost CreateHost(string[] args = null) => DS4WinWPF.AppHost.CreateHost(args);
        public static T GetService<T>() where T : class => DS4WinWPF.AppHost.GetService<T>();
        public static object GetService(Type serviceType) => DS4WinWPF.AppHost.GetService(serviceType);
        public static void Dispose() => DS4WinWPF.AppHost.Dispose();
    }
}
