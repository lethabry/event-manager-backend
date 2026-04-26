using Microsoft.AspNetCore.Mvc;

namespace EventManager.Controllers;

public class EventsController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}