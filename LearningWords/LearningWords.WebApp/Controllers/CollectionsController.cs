using LearningWords.BL.Services;
using LearningWords.WebApp.Models.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearningWords.WebApp.Controllers
{
    [Authorize]
    public class CollectionsController : Controller
    {
        private readonly CollectionService collectionService;

        public CollectionsController(CollectionService collectionService) 
        {
            this.collectionService = collectionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var collections = await collectionService.GetAll(userId);
            var model = new CollectionsListViewModel() { Collections = collections };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> New()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Save()
        {
            return View();
        }
    }
}
