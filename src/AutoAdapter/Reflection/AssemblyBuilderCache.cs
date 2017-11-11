namespace AutoAdapter.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Represents an <see cref="AssemblyBuilder"/> cache.
    /// </summary>
    public class AssemblyBuilderCache
    {
        /// <summary>
        /// The cache.
        /// </summary>
        private Dictionary<string, AssemblyBuilder> cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyBuilderCache"/> class.
        /// </summary>
        public AssemblyBuilderCache()
        {
            this.cache = new Dictionary<string, AssemblyBuilder>();
        }

        /// <summary>
        /// Gets or creates an <see cref="AssemblyBuilder"/> and <see cref="ModuleBuilder"/> pair.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly builder.</param>
        /// <returns>An assembly builder instance.</returns>
        public AssemblyBuilder GetOrCreateAssemblyBuilder(string assemblyName)
        {
            AssemblyBuilder builder;
            if (this.cache.TryGetValue(assemblyName, out builder) == false)
            {
                AssemblyName name = new AssemblyName(assemblyName);
                builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
                this.cache.Add(assemblyName, builder);
            }

            return builder;
        }

        /// <summary>
        /// Removes an assembly builder and all of its module builders.
        /// </summary>
        /// <param name="name">The name of the assembly builder.</param>
        /// <returns>True if removed; otherwise false.</returns>
        public bool RemoveAssemblyBuilder(string name)
        {
            return this.cache.Remove(name);
        }

        public IEnumerable<Assembly> GetAssemblies()
        {
            return this.cache.Values;
        }
    }
}
