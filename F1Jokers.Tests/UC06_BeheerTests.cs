using Xunit;
using F1Jokers.Controllers;
using F1Jokers.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;

namespace F1Jokers.Tests
{
    public class UC06_BeheerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // UC-06: Controleert of de controller validatie ingrijpt bij het aanvragen van een lege API-ronde
        [Fact]
        public async Task HaalUitslagenOp_LegeInput_StoptVerwerkingEnRedirect()
        {
            // 1. Arrange (Klaarzetten)
            var context = GetInMemoryDbContext();

            // We vullen de services met null, maar geven wél de database mee
            var controller = new BeheerController(null, null, context);

            // OPLOSSING: We geven de controller een 'nep' TempData object.
            // Hierdoor crasht hij niet als hij een foutmelding (bijv. TempData["Error"]) probeert te zetten.
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            // 2. Act (Uitvoeren)
            var result = await controller.HaalUitslagenOp("") as RedirectToActionResult;

            // 3. Assert (Controleren)
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }
    }
}