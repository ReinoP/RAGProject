using AIproject.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIproject.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IUserContext _userContext;

        public HomeController(IUserContext userContext)
        {
            _userContext = userContext;

        }
        public IActionResult Index()
        {
            ViewBag.UserName = _userContext.GetUserName();
            return View();
        }
    }
}