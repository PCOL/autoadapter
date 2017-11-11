namespace AutoAdapter.Extensions
{
    using System;

    /// <summary>
    /// An attribute to allow an adapter extension to be applied to a parameter, property, or return value.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Property)]
    public class AdapterExtensionAttribute
        : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterExtensionAttribute"/> class.
        /// </summary>
        /// <param name="extensionName">The name of the extension to apply.</param>
        public AdapterExtensionAttribute(string extensionName)
        {
            this.ExtensionName = extensionName;
        }

        /// <summary>
        /// Gets the name of the extension.
        /// </summary>
        public string ExtensionName { get; }

        /// <summary>
        /// Gets or sets the extension methods placement.
        /// </summary>
        public AdapterExtensionPlacement? Placement { get; set; }
    }
}
