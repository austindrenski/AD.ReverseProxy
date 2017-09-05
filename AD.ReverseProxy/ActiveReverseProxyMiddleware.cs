//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
//using JetBrains.Annotations;
//using Microsoft.AspNetCore.Http;

//namespace AD.ReverseProxy
//{
//    [PublicAPI]
//    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
//    public class ActiveReverseProxyMiddleware
//    {
//        private const string RegisterForwardString = "/register-forward";
//        private const string RegisterReverseString = "/register-reverse";

//        private const string UnregisterForwardString = "/unregister-forward";
//        private const string UnregisterReverseString = "/unregister-reverse";

//        private static readonly PathString RegisterForwardPathString = RegisterForwardString;
//        private static readonly PathString RegisterReversePathString = RegisterReverseString;

//        private static readonly PathString UnregisterForwardPathString = UnregisterForwardString;
//        private static readonly PathString UnregisterReversePathString = UnregisterReverseString;

//        private readonly ISet<ReverseProxyTarget> _forwardTargets;
//        private readonly ISet<ReverseProxyTarget> _reverseTargets;

//        private readonly HttpMessageInvoker _httpMessageInvoker;

//        private readonly RequestDelegate _next;

//        public ActiveReverseProxyMiddleware(RequestDelegate next, HttpMessageInvoker httpMessageInvoker)
//        {
//            _next = next;

//            _forwardTargets = new HashSet<ReverseProxyTarget>();
//            _reverseTargets = new HashSet<ReverseProxyTarget>();

//            _httpMessageInvoker = httpMessageInvoker;
//        }

//        /// <summary>
//        /// Request handling method.
//        /// </summary>
//        /// <param name="context">
//        /// The <see cref="HttpContext"/> for the current request.
//        /// </param>
//        /// <returns>
//        /// A <see cref="Task"/> that represents the execution of this middleware.
//        /// </returns>
//        public async Task InvokeAsync(HttpContext context)
//        {
//            // Is this a request to register or unregister a target?
//            if (context.Request.Path.StartsWithSegments(RegisterForwardPathString))
//            {
//                RegisterForward(context);
//                return;
//            }
//            if (context.Request.Path.StartsWithSegments(UnregisterForwardPathString))
//            {
//                UnregisterForward(context);
//                return;
//            }
//            if (context.Request.Path.StartsWithSegments(RegisterReversePathString))
//            {
//                RegisterReverse(context);
//                return;
//            }
//            if (context.Request.Path.StartsWithSegments(UnregisterReversePathString))
//            {
//                UnregisterReverse(context);
//                return;
//            }

//            // Look for a viable target.
//            foreach (ReverseProxyTarget target in _forwardTargets)
//            {
//                if (!context.Request.Path.StartsWithSegments(target.PathBase))
//                {
//                    continue;
//                }

//                await Forward(context, target, UnregisterForward);
//                return;
//            }

//            // No viable target found. Continue down the HTTP pipeline.
//            if (!context.Response.HasStarted)
//            {
//                await _next(context);
//            }
//        }

//        protected virtual void RegisterForward(HttpContext context)
//        {
//            _forwardTargets.Add(new ReverseProxyTarget(new Uri(context.Request.Query["target"])));
//        }

//        protected virtual void RegisterReverse(HttpContext context)
//        {
//            _reverseTargets.Add(new ReverseProxyTarget(new Uri(context.Request.Query["target"])));
//        }

//        protected virtual void UnregisterForward(HttpContext context)
//        {
//            _forwardTargets.Remove(new ReverseProxyTarget(new Uri(context.Request.Query["target"])));
//        }

//        protected virtual void UnregisterReverse(HttpContext context)
//        {
//            _reverseTargets.Remove(new ReverseProxyTarget(new Uri(context.Request.Query["target"])));
//        }

//        protected virtual void UnregisterForward(ReverseProxyTarget target)
//        {
//            _forwardTargets.Remove(target);
//        }

//        protected virtual void UnregisterReverse(ReverseProxyTarget target)
//        {
//            _reverseTargets.Remove(target);
//        }

//        private Task Forward(HttpContext context, ReverseProxyTarget target, Action<ReverseProxyTarget> unregisterOnFail)
//        {
//            HttpRequestMessage requestMessage =
//                new HttpRequestMessage
//                {
//                    Content = context.Request.ContentLength is null ? null : new StreamContent(context.Request.Body),
//                    Method = new HttpMethod(context.Request.Method),
//                    RequestUri = target.ConstructUri(context)
//                };

//            foreach ((string key, string[] value) in context.Request.Headers)
//            {
//                if (!requestMessage.Headers.TryAddWithoutValidation(key, value))
//                {
//                    requestMessage.Content?.Headers.TryAddWithoutValidation(key, value);
//                }
//            }

//            try
//            {
//                return SendAsync(context, requestMessage);
//            }
//            catch (HttpRequestException)
//            {
//                // The idea here is that a target may fail because a new version was registered, and the old version was 
//                // taken offline without first being unregistered. If so, removing the expired target is sufficient and the user should reaccess.

//                unregisterOnFail(target);

//                if (context.Response.HasStarted)
//                {
//                    return _next(context);
//                }

//                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

//                return context.Response.WriteAsync($"{context.Response.StatusCode}: The registered service has expired. Please try again.");
//            }
//        }

//        private async Task SendAsync(HttpContext context, HttpRequestMessage requestMessage)
//        {
//            using (HttpResponseMessage responseMessage = await _httpMessageInvoker.SendAsync(requestMessage, context.RequestAborted))
//            {
//                context.Response.StatusCode = (int)responseMessage.StatusCode;
                
//                foreach ((string key, IEnumerable<string> value) in responseMessage.Headers)
//                {
//                    context.Response.Headers[key] = value as string[] ?? value.ToArray();
//                }

//                foreach ((string key, IEnumerable<string> value) in responseMessage.Content.Headers)
//                {
//                    context.Response.Headers[key] = value as string[] ?? value.ToArray();
//                }

//                context.Response.Headers.Remove("transfer-encoding");

//                await responseMessage.Content.CopyToAsync(context.Response.Body);
//            }
//        }
//    }
//}