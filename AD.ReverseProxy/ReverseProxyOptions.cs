using System.Collections.Generic;
using JetBrains.Annotations;

namespace AD.ReverseProxy
{
    /// <summary>
    /// Options for the <see cref="ReverseProxyMiddleware"/>.
    /// </summary>
    [PublicAPI]
    public class ReverseProxyOptions
    {
        /// <summary>
        /// The collection of <see cref="ReverseProxyTarget"/> objects.
        /// </summary>
        public ICollection<ReverseProxyTarget> Targets { get; }

        /// <summary>
        /// Default constructor required for configuration binding.
        /// </summary>
        public ReverseProxyOptions()
        {
            Targets = new List<ReverseProxyTarget>();
        }
    }
}