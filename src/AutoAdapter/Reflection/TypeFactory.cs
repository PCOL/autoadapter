namespace AutoAdapter.Reflection
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    public class TypeFactory
    {
        /// <summary>
        /// The default <see cref="TypeFactory"/> instance.
        /// </summary>
        private static Lazy<TypeFactory> instance = new Lazy<TypeFactory>(() => new TypeFactory("Default", "Default"), true);

        /// <summary>
        /// The assemlby cache.
        /// </summary>
        private AssemblyBuilderCache assemblyCache;

        /// <summary>
        /// The assembly builder.
        /// </summary>
        private AssemblyBuilder assemblyBuilder;

        /// <summary>
        /// Initialises a new instance of the <see cref=""/> class.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="moduleName">The module name.</param>
        public TypeFactory(string assemblyName, string moduleName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            if (string.IsNullOrEmpty(assemblyName) == true)
            {
                throw new ArgumentException("Value cannot be empty.", nameof(assemblyName));
            }

            if (moduleName == null)
            {
                throw new ArgumentNullException(nameof(moduleName));
            }

            if (string.IsNullOrEmpty(moduleName) == true)
            {
                throw new ArgumentException("Value cannot be empty.", nameof(moduleName));
            }

            this.assemblyCache = new AssemblyBuilderCache();
            this.assemblyBuilder =  this.assemblyCache.GetOrCreateAssemblyBuilder(assemblyName);
            this.ModuleBuilder = this.assemblyBuilder.DefineDynamicModule(moduleName);
        }

        /// <summary>
        /// Gets the default type factory.
        /// </summary>
        public static TypeFactory Default
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// THe module builder
        /// </summary>
        public ModuleBuilder ModuleBuilder { get;}

        /// <summary>
        /// Creates the global functions in a module.
        /// </summary>
        public void CreateGlobalFunctions()
        {
            this.ModuleBuilder.CreateGlobalFunctions();
        }

        /// <summary>
        /// Gets a method from the type factory.
        /// </summary>
        /// <param name="methodName">The method name.</param>
        /// <returns>A <see cref="MethodInfo"/> instance if found; otherwise null.</returns>
        public MethodInfo GetMethod(string methodName)
        {
            return this.ModuleBuilder.GetMethod(methodName);
        }

                /// <summary>
        /// Gets a type by name from the current <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <param name="dynamicOnly">A value indicating whether only dynamic assemblies should be checked or not.</param>
        /// <returns>A <see cref="Type"/> representing the type if found; otherwise null.</returns>
        public Type GetType(string typeName, bool dynamicOnly)
        {
//Console.WriteLine("TypeName: {0} - {1}", typeName, dynamicOnly);

            var list = this.assemblyCache.GetAssemblies()
                .Union(AssemblyCache.GetAssemblies()).ToArray();

// Console.WriteLine("=======================");
// Console.WriteLine("Assembly Count: {0}", list.Count());
// Console.WriteLine("=======================");

            foreach (var ass in list)
            {
                if (dynamicOnly == false ||
                    ass.IsDynamic == true)
                {
                    Type type = ass.GetType(typeName);
                    if (type != null)
                    {
//Console.WriteLine("Found");
                        return type;
                    }
                }
            }

            return null;
        }
    }
}