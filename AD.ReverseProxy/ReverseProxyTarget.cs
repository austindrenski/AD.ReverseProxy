using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace AD.ReverseProxy
{
    /// <summary>
    /// Represents a target for dispatch by the <see cref="ReverseProxyMiddleware"/>.
    /// </summary>
    [PublicAPI]
    public class ReverseProxyTarget
    {
        /// <summary>
        /// The scheme: 'http' or 'https'.
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// The host string.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The port number. If unknown, provide '80' for 'http' or '443' for 'https'.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The path that follows the port. This should start with a '/'.
        /// </summary>
        public string PathBase { get; set; }

        /// <summary>
        /// Gets '{Scheme}://{Host}:{Port}{PathBase}'.
        /// </summary>
        public string BaseUri => $"{Scheme}://{Host}:{Port}{PathBase}";

        /// <summary>
        /// Default constructor required for configuration binding.
        /// </summary>
        public ReverseProxyTarget()
        {
        }

        /// <summary>
        /// Constructs a <see cref="ReverseProxyTarget"/>.
        /// </summary>
        public ReverseProxyTarget(string scheme, string host, int port, string pathBase)
        {
            if (scheme is null)
            {
                throw new ArgumentNullException(nameof(scheme));
            }
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (port < 0)
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (pathBase is null)
            {
                throw new ArgumentNullException(nameof(pathBase));
            }

            Scheme = scheme;
            Host = host;
            Port = port;
            PathBase = pathBase;
        }

        /// <summary>
        /// Constructs a <see cref="Uri"/> for the redirected request.
        /// </summary>
        /// <param name="context">
        /// The context that supplies the information that follows the <see cref="PathBase"/>.
        /// </param>
        /// <returns>
        /// Returns a <see cref="Uri"/> for the redirected request.
        /// </returns>
        public Uri ConstructUri(HttpContext context)
        {
            string path = context.Request.Path;

            if (path.StartsWith(PathBase))
            {
                path = path.Replace(PathBase, null, StringComparison.OrdinalIgnoreCase);
            }

            return new Uri($"{BaseUri}{path}{context.Request.QueryString}");
        }
    }
}