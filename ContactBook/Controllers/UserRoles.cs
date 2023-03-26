using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ContactBook.Controllers
{
   // [Authorize]
    public class UserRoles : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRoles(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

      //  [Authorize(Roles = "Admin")]
        //View all created roles
        public IActionResult Index()
        {
            var roles = _roleManager.Roles;
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(IdentityRole model)
        {
            if (!_roleManager.RoleExistsAsync(model.Name).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(model.Name)).GetAwaiter().GetResult();
            }

            return RedirectToAction("Index");
        }
    }
}
