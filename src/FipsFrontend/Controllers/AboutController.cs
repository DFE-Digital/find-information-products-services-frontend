using Microsoft.AspNetCore.Mvc;

namespace FipsFrontend.Controllers;

public class AboutController : Controller
{
    public IActionResult Index() => View();
}
