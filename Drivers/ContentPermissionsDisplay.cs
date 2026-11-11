using Etch.OrchardCore.ContentPermissions.Models;
using Etch.OrchardCore.ContentPermissions.Services;
using Etch.OrchardCore.ContentPermissions.ViewModels;
using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etch.OrchardCore.ContentPermissions.Drivers
{
    public class ContentPermissionsDisplay : ContentPartDisplayDriver<ContentPermissionsPart>
    {
        #region Dependencies

        private readonly IContentPermissionsService _contentPermissionsService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRoleService _roleService;
        private readonly IEnumerable<IContentPermissionsOverride> _overrides;

        #endregion

        #region Constructor

        public ContentPermissionsDisplay(
            IContentPermissionsService contentPermissionsService,
            IHttpContextAccessor httpContextAccessor,
            IRoleService roleService,
            IEnumerable<IContentPermissionsOverride> overrides)
        {
            _contentPermissionsService = contentPermissionsService;
            _httpContextAccessor = httpContextAccessor;
            _roleService = roleService;
            _overrides = overrides;
        }

        #endregion

        #region Overrides

        public override async Task<IDisplayResult> DisplayAsync(ContentPermissionsPart part, BuildPartDisplayContext context)
        {
            var settings = await _contentPermissionsService.GetSettingsAsync(part);

            if (context.DisplayType != "Detail" || await CanAccessAsync(part))
            {
                return null;
            }

            _httpContextAccessor.HttpContext.Response.StatusCode = 403;

            var redirectUrl = "/Error/403";

            if (settings.HasRedirectUrl)
            {
                redirectUrl = settings.RedirectUrl;

                if (!settings.RedirectUrl.StartsWith('/'))
                {
                    redirectUrl = $"/{redirectUrl}";
                }
            }

            // A "{returnUrl}" token in the configured URL is replaced with the escaped path and
            // query of the page being denied, so the target (e.g. a sign-in attempt) can return here.
            var request = _httpContextAccessor.HttpContext.Request;
            if (redirectUrl.Contains("{returnUrl}", StringComparison.Ordinal))
            {
                var returnUrl = Uri.EscapeDataString($"{request.Path}{request.QueryString}");
                redirectUrl = redirectUrl.Replace("{returnUrl}", returnUrl, StringComparison.Ordinal);
            }

            _httpContextAccessor.HttpContext.Response.Redirect($"{request.PathBase}{redirectUrl}", false);

            return null;
        }

        public override async Task<IDisplayResult> EditAsync(ContentPermissionsPart part, BuildPartEditorContext context)
        {
            var roles = await _roleService.GetRoleNamesAsync();

            return Initialize<ContentPermissionsPartEditViewModel>("ContentPermissionsPart_Edit", model =>
            {
                model.ContentPermissionsPart = part;
                model.Enabled = part.Enabled;
                model.PossibleRoles = roles.ToArray();
                model.Roles = part.Roles;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentPermissionsPart model, UpdatePartEditorContext context)
        {
            await context.Updater.TryUpdateModelAsync(model, Prefix, m => m.Enabled, m => m.Roles);

            if (!model.Enabled)
            {
                model.Roles = Array.Empty<string>();
            }

            return Edit(model, context);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// The role list, or failing that any registered <see cref="IContentPermissionsOverride"/>
        /// that grants the signed-in user this item.
        /// </summary>
        private async Task<bool> CanAccessAsync(ContentPermissionsPart part)
        {
            if (_contentPermissionsService.CanAccess(part))
            {
                return true;
            }

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || part.ContentItem == null)
            {
                return false;
            }

            foreach (var permissionsOverride in _overrides)
            {
                if (await permissionsOverride.CanAccessAsync(user, part.ContentItem))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
