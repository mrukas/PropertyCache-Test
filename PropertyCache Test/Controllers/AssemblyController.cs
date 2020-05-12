using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace PropertyCache_Test.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssemblyController : ControllerBase
    {
        private static AssemblyLoadContext _loadContext;
        private static readonly IDictionary _propertiesCache;
        private static readonly IDictionary _visiblePropertiesCache;

        private readonly ApplicationPartManager _partManager;

        // We keep a reference to ihe internal property caches.
        static AssemblyController()
        {
            var coreAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName.Contains("Microsoft.AspNetCore.Mvc.Core"));
            var propertyHelper = coreAssembly.GetTypes().FirstOrDefault(x => x.FullName.Contains("PropertyHelper"));

            var propertiesCacheField = propertyHelper.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "PropertiesCache");
            var visiblePropertiesCacheField = propertyHelper.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "VisiblePropertiesCache");

            _propertiesCache = (IDictionary)propertiesCacheField.GetValue(null);
            _visiblePropertiesCache = (IDictionary)visiblePropertiesCacheField.GetValue(null);
        }

        public AssemblyController(ApplicationPartManager partManager)
        {
            _partManager = partManager;
        }

        [HttpGet("/api/assembly/loadunload")]
        public IActionResult LoadUnload()
        {
            bool isLoaded;

            if (_loadContext == null)
            {
                LoadAssembly();
                isLoaded = true;
            }
            else
            {
                UnloadAssembly();
                isLoaded = false;
            }


            return Ok(new { isLoaded, ItemsInPropertiesCache = _propertiesCache.Count, ItemsInVisiblePropertiesCache = _visiblePropertiesCache.Count });
        }

        [HttpGet("/api/assembly/loadtest")]
        public IActionResult LoadTest()
        {
            for (var i = 0; i < 10000; i++)
            {
                LoadAssembly();
                // This is only needed during debug otherwise the app crashes if we load and unload too fast.
                // Not needed in Production build.
                Thread.Sleep(500);
                UnloadAssembly();
            }

            return Ok();
        }

        private void LoadAssembly()
        {
            _loadContext = new AssemblyLoadContext(null, true);
            _loadContext.Unloading += LogContextUnloaded;

            var assembly = _loadContext.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), "Modules",
                 "Web Module.dll"));

            var pluginPart = new AssemblyPart(assembly);
            _partManager.ApplicationParts.Add(pluginPart);
            ModuleChangeProvider.Instance.NotifyChange();
        }

        private void LogContextUnloaded(AssemblyLoadContext context)
        {
            Console.WriteLine("Context has been unloaded.");
        }

        private void UnloadAssembly()
        {
            var appPartToRemove = _partManager.ApplicationParts.FirstOrDefault(p => p.Name == "Web Module");

            if (appPartToRemove != null)
            {
                _partManager.ApplicationParts.Remove(appPartToRemove);
            }

            _loadContext.Unload();
            _loadContext = null;

            ModuleChangeProvider.Instance.NotifyChange();

            // If this is not getting called the memory increases with every loading and unloading of an assembly.
            //ClearPropertyCaches();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            PrintNumberOfCacheEntries();
        }

        private void ClearPropertyCaches()
        {
            _propertiesCache.Clear();
            _visiblePropertiesCache.Clear();
        }

        // This is only used to show that the number of cache entries increases.
        private void PrintNumberOfCacheEntries()
        {
            Console.WriteLine($"PropertiesCache Count: {_propertiesCache.Keys.Count}");
            Console.WriteLine($"VisiblePropertiesCache Count: {_visiblePropertiesCache.Keys.Count}");
        }
    }
}