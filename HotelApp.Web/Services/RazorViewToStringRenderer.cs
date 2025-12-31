using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace HotelApp.Web.Services
{
    public class RazorViewToStringRenderer : IRazorViewToStringRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public RazorViewToStringRenderer(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderViewToStringAsync(Controller controller, string viewName, object model)
        {
            var actionContext = controller.ControllerContext;

            var viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: true);
            if (!viewResult.Success)
            {
                // Fallback for explicit paths.
                viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
            }

            if (!viewResult.Success || viewResult.View == null)
            {
                var searched = viewResult.SearchedLocations != null
                    ? string.Join("; ", viewResult.SearchedLocations)
                    : "(none)";

                throw new InvalidOperationException($"Unable to find view '{viewName}'. Searched: {searched}");
            }

            await using var output = new StringWriter();

            var viewData = new ViewDataDictionary(
                metadataProvider: new EmptyModelMetadataProvider(),
                modelState: new ModelStateDictionary())
            {
                Model = model
            };

            // Copy any ViewData/ViewBag values already set on the controller.
            foreach (var kvp in controller.ViewData)
            {
                viewData[kvp.Key] = kvp.Value;
            }

            var tempDataFactory = _serviceProvider.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(controller.HttpContext);

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewData,
                tempData,
                output,
                new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return output.ToString();
        }
    }
}
