using OrchardCore.ContentManagement;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Etch.OrchardCore.ContentPermissions.Services
{
    /// <summary>
    /// Lets another module grant access to a content item that the item's role list
    /// (<see cref="Models.ContentPermissionsPart"/>) would deny, so its page opens. Register any
    /// number as <c>services.AddScoped&lt;IContentPermissionsOverride, ...&gt;()</c>; the display
    /// driver asks them only when the role list says no.
    /// </summary>
    public interface IContentPermissionsOverride
    {
        /// <summary>
        /// True grants access to <paramref name="contentItem"/>. False means "no opinion", never
        /// "deny": the role list (and any other override) still decides.
        /// </summary>
        Task<bool> CanAccessAsync(ClaimsPrincipal user, ContentItem contentItem);
    }
}
