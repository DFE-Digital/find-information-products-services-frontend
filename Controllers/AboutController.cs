using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

public class AboutController : Controller
{
    public IActionResult Index() => View(new AboutViewModel());
}
