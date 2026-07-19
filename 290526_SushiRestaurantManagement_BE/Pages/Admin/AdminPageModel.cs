using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class AdminPageModel : PageModel
    {
        public override void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("StaffRole");

            // Nếu không phải Admin thì đá ngược ra trang Login
            if (role != "Admin")
            {
                context.Result = new RedirectToPageResult("/Auth/Login");
            }

            base.OnPageHandlerExecuting(context);
        }
    }
}
