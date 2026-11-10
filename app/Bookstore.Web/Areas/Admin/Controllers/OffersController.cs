using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Domain.Offers;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Areas.Admin.Models.Offers;

namespace Bookstore.Web.Areas.Admin.Controllers
{
    public class OffersController : AdminAreaControllerBase
    {
        private readonly IOfferService offerService;
        private readonly IReferenceDataService referenceDataService;

        public OffersController(IOfferService offerService, IReferenceDataService referenceDataService)
        {
            this.offerService = offerService;
            this.referenceDataService = referenceDataService;
        }

        public async Task<ActionResult> Index(OfferFilters filters, int pageIndex = 1, int pageSize = 10)
        {
            var offers = await offerService.GetOffersAsync(filters, pageIndex, pageSize);
            var referenceData = await referenceDataService.GetAllReferenceDataAsync();

            return View(new OfferIndexViewModel(offers, referenceData));
        }

        // ASP.NET Core trims the "Async" suffix from action names by default (SuppressAsyncSuffixInActionNames);
        // [ActionName] keeps the MVC5 URLs (/Admin/Offers/ApproveAsync/{id}, ...) that Views/Offers/Index.cshtml posts to.
        [HttpPost]
        [ActionName("ApproveAsync")]
        public async Task<ActionResult> ApproveAsync(int id)
        {
            return await UpdateOfferStatus(id, OfferStatus.Approved, "The offer has been approved");
        }

        [HttpPost]
        [ActionName("RejectAsync")]
        public async Task<ActionResult> RejectAsync(int id)
        {
            return await UpdateOfferStatus(id, OfferStatus.Rejected, "The offer has been rejected");
        }

        [HttpPost]
        [ActionName("ReceivedAsync")]
        public async Task<ActionResult> ReceivedAsync(int id)
        {
            return await UpdateOfferStatus(id, OfferStatus.Received, "The book has been received");
        }

        [HttpPost]
        [ActionName("PaidAsync")]
        public async Task<ActionResult> PaidAsync(int id)
        {
            return await UpdateOfferStatus(id, OfferStatus.Paid, "The customer has been paid");
        }

        private async Task<ActionResult> UpdateOfferStatus(int id, OfferStatus status, string message)
        {
            var dto = new UpdateOfferStatusDto(id, status);

            await offerService.UpdateOfferStatusAsync(dto);

            TempData["Message"] = message;

            return RedirectToAction("Index");
        }
    }
}