namespace AutoAdapter.Extensions
{
    /// <summary>
    /// <see cref="AdapterTypeGenerator"/> extension placements.
    /// </summary>
    public enum AdapterExtensionPlacement
    {
        /// <summary>
        /// Execute the extension method before calling the adapted object.
        /// </summary>
        Before,

        /// <summary>
        /// Execue the extension method after calling the adapted object.
        /// </summary>
        After
    }
}
