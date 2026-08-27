using Microsoft.AspNetCore.Mvc;

namespace FipsFrontend.Controllers;

public class HelpController : Controller
{
    public IActionResult Index() => View();
}
