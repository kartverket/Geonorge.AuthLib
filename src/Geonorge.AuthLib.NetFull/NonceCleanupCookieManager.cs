using System;
using System.Linq;
using Microsoft.Owin;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Infrastructure;

namespace Geonorge.AuthLib.NetFull
{
    /// <summary>
    /// Cookie manager for the OpenID Connect middleware that prevents "OpenIdConnect.nonce.*" cookies
    /// from piling up and causing "Request header / cookie too large" errors.
    ///
    /// Katana writes a new nonce cookie (path "/", lifetime 1 hour) for every redirect to the identity
    /// provider, and only removes it when the matching callback succeeds. Challenges that never complete
    /// (AJAX/fetch calls answered with 401, redirect loops between applications on the same host, aborted
    /// logins) therefore leave cookies behind, and they are sent with every request to the host.
    ///
    /// This manager:
    /// * writes cookies through System.Web (avoids the Katana/System.Web cookie bug where Set-Cookie headers are lost)
    /// * does not write nonce cookies for requests that are not browser navigations (they can never complete a login)
    /// * removes stale nonce cookies before writing a new one when there are more than <see cref="MaxNonceCookies"/>
    /// </summary>
    public class NonceCleanupCookieManager : ICookieManager
    {
        internal const string NonceCookiePrefix = "OpenIdConnect.nonce.";

        /// <summary>
        /// Number of existing nonce cookies tolerated before they are all removed. A few are allowed so that
        /// parallel logins in several tabs still work.
        /// </summary>
        public int MaxNonceCookies { get; set; } = 3;

        private readonly ICookieManager _inner = new SystemWebCookieManager();

        public string GetRequestCookie(IOwinContext context, string key)
        {
            return _inner.GetRequestCookie(context, key);
        }

        public void AppendResponseCookie(IOwinContext context, string key, string value, CookieOptions options)
        {
            if (key != null && key.StartsWith(NonceCookiePrefix, StringComparison.Ordinal))
            {
                if (!RequestUtility.IsBrowserNavigation(context.Request))
                    return;

                var staleNonceKeys = context.Request.Cookies
                    .Select(c => c.Key)
                    .Where(k => k.StartsWith(NonceCookiePrefix, StringComparison.Ordinal) && k != key)
                    .ToList();

                if (staleNonceKeys.Count > MaxNonceCookies)
                {
                    foreach (var staleKey in staleNonceKeys)
                    {
                        _inner.DeleteCookie(context, staleKey, new CookieOptions
                        {
                            Path = options?.Path,
                            Domain = options?.Domain,
                            HttpOnly = true,
                            Secure = context.Request.IsSecure,
                            SameSite = SameSiteMode.None
                        });
                    }
                }
            }

            _inner.AppendResponseCookie(context, key, value, options);
        }

        public void DeleteCookie(IOwinContext context, string key, CookieOptions options)
        {
            _inner.DeleteCookie(context, key, options);
        }
    }

    internal static class RequestUtility
    {
        /// <summary>
        /// True if the request is a top-level/frame browser navigation, i.e. a request where redirecting
        /// to the identity provider can actually complete a login.
        /// </summary>
        public static bool IsBrowserNavigation(IOwinRequest request)
        {
            var secFetchMode = request.Headers.Get("Sec-Fetch-Mode");
            if (!string.IsNullOrEmpty(secFetchMode))
                return string.Equals(secFetchMode, "navigate", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(request.Headers.Get("X-Requested-With"), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                return false;

            var accept = request.Headers.Get("Accept");
            return string.IsNullOrEmpty(accept) || accept.IndexOf("text/html", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
