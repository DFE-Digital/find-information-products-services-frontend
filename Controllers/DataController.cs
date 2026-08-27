using Microsoft.AspNetCore.Mvc;

namespace FipsFrontend.Controllers;

public class DataController : Controller
{
    public IActionResult Index() => View();
}
