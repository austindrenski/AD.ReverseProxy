using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AD.ReverseProxy
{
    /// <summary>
    /// Provides support to dispatch incoming HTTP requests to alternate targets.
    /// </summary>
    [PublicAPI]
    public class ReverseProxyMiddleware
    {
        /// <summary>
        /// The next <see cref="RequestDelegate"/> in the HTTP pipeline.
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// The <see cref="HttpMessageInvoker"/> that dispatches to targets.
        /// </summary>
        private readonly HttpMessageInvoker _httpMessageInvoker;

        /// <summary>
        /// The alternate targets available for dispatch.
        /// </summary>
        private readonly IEnumerable<ReverseProxyTarget> _targets;

        /// <summary>
        /// Constructs the <see cref="ReverseProxyMiddleware"/>. This is called by the framework.
        /// </summary>
        /// <param name="next">
        /// The next <see cref="RequestDelegate"/> in the HTTP pipeline.
        /// </param>
        /// <param name="httpMessageInvoker">
        /// The <see cref="HttpMessageInvoker"/> that dispatches to targets.
        /// </param>
        /// <param name="options">
        /// The alternate targets available for dispatch.
        /// </param>
        public ReverseProxyMiddleware(RequestDelegate next, HttpMessageInvoker httpMessageInvoker, IOptions<ReverseProxyOptions> options)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (httpMessageInvoker is null)
            {
                throw new ArgumentNullException(nameof(httpMessageInvoker));
            }
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _httpMessageInvoker = httpMessageInvoker;
            _targets = options.Value.Targets.ToArray();
        }

        /// <summary>
        /// This method is called when the request reaches this middleware in the HTTP pipeline. This method is called by the framework.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/> for the current request.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the execution of this middleware.
        /// </returns>
        public Task InvokeAsync(HttpContext context)
        {
            // Look for a viable target.
            foreach (ReverseProxyTarget target in _targets)
            {
                if (context.Request.Path.StartsWithSegments(target.PathBase))
                {
                   return Dispatch(context, target);
                }
            }
            
            // No viable target; continue down the pipeline.
            return _next(context);
        }

        /// <summary>
        /// Dispatches the HTTP request to the <see cref="ReverseProxyTarget"/>.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/> of the current request.
        /// </param>
        /// <param name="target">
        /// The <see cref="ReverseProxyTarget"/> to which the request is dispatched.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the execution of this middleware.
        /// </returns>
        private async Task Dispatch(HttpContext context, ReverseProxyTarget target)
        {
            HttpRequestMessage requestMessage =
                new HttpRequestMessage
                {
                    Content = context.Request.ContentLength is null ? null : new StreamContent(context.Request.Body),
                    Method = new HttpMethod(context.Request.Method),
                    RequestUri = target.ConstructUri(context)
                };

            foreach ((string key, string[] value) in context.Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(key, value))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(key, value);
                }
            }
            
            using (HttpResponseMessage responseMessage = await _httpMessageInvoker.SendAsync(requestMessage, context.RequestAborted))
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;

                foreach ((string key, IEnumerable<string> value) in responseMessage.Headers)
                {
                    context.Response.Headers[key] = value as string[] ?? value.ToArray();
                }

                foreach ((string key, IEnumerable<string> value) in responseMessage.Content.Headers)
                {
                    context.Response.Headers[key] = value as string[] ?? value.ToArray();
                }

                context.Response.Headers.Remove("transfer-encoding");

                await responseMessage.Content.CopyToAsync(context.Response.Body);
            }
        }
    }
}