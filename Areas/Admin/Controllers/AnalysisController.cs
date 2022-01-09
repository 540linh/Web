using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShoesLover.Data;
using ShoesLover.Models;

namespace ShoesLover.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AnalysisController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public IActionResult SoGiay()
        {
            StoreContext context = HttpContext.RequestServices.GetService(typeof(StoreContext)) as StoreContext;
            return View(context.SoLuongGiay());
        }
        public IActionResult DoanhThu()
        {
            StoreContext context = HttpContext.RequestServices.GetService(typeof(StoreContext)) as StoreContext;
            return View(context.DoanhThu());
        }
        public IActionResult LietKeSubCategory()
        {
            StoreContext context = HttpContext.RequestServices.GetService(typeof(StoreContext)) as StoreContext;
            IEnumerable<CategoryMasterModel> allCategory = context.GetCategoryMasters();
            ViewData["category"] = allCategory.Where(item => item.SubCategoryList.Count > 0);
            return View(context.GetSubCategories());
        }
        public IActionResult LietKeSoGiay(SubCategory sc)
        {
            StoreContext context = HttpContext.RequestServices.GetService(typeof(StoreContext)) as StoreContext;
            return View(context.GetProducts(sc.Id));
        }
        public IActionResult Top5BestSeller()
        {
            StoreContext context = HttpContext.RequestServices.GetService(typeof(StoreContext)) as StoreContext;
            return View(context.Top5BestSeller());
        }
    }
}
