using System.Security.Claims;
using System.Security.Principal;

namespace Geonorge.AuthLib.Common
{
    /// <summary>
    /// Utility class for quickly accessing user claims. See examples of use in ControllerBase class in MetadataEditor.
    /// </summary>
    public static class ClaimsPrincipalUtility
    {
        public static string GetCurrentUserOrganizationName(IPrincipal user)
        {
            if (user is ClaimsPrincipal principal)
            {
                return principal.GetOrganizationName();
            }

            return null;
        }

        public static string GetUsername(IPrincipal user)
        {
            if (user is ClaimsPrincipal principal)
            {
                return principal.GetUsername();
            }

            return null;
        }

        public static bool UserHasMetadataAdminRole(IPrincipal user)
        {
            if (user is ClaimsPrincipal principal)
            {
                return principal.IsInRole(GeonorgeRoles.MetadataAdmin);
            }
            return false;
        }
    }
}