using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryManagement.Filters;

public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    public RoleAuthorizeAttribute(params string[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Session.GetString("Role");
        if (string.IsNullOrWhiteSpace(role) || !_roles.Contains(role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
