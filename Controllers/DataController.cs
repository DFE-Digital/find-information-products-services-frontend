using Microsoft.AspNetCore.Mvc;
using FipsFrontend.Models;

namespace FipsFrontend.Controllers;

public class DataController : Controller
{
    public IActionResult Index() => View(new DataViewModel());
}
