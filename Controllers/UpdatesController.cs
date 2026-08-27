using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

public class UpdatesController : Controller
{
    public IActionResult Index() => View(new UpdatesViewModel());
}
