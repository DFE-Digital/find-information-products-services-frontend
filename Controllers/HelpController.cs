using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

public class HelpController : Controller
{
    public IActionResult Index() => View(new HelpViewModel());
}
