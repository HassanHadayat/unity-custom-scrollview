using CustomScrollView.Controller;
using CustomScrollView.Data;

namespace CustomScrollView.Extensions
{
    /// <summary>
    /// Fluent extensions for quick setup.
    /// </summary>
    public static class ScrollViewExtensions
    {
        /// <summary>
        /// Creates a SimpleDataSource with a single section of uniform items.
        /// </summary>
        public static SimpleDataSource CreateUniformDataSource(int itemCount, float itemSize, float headerSize = 0f, float footerSize = 0f)
        {
            var ds = new SimpleDataSource();
            int section = ds.AddSection(headerSize, footerSize);
            ds.AddItems(section, itemCount, itemSize);
            return ds;
        }
    }
}
