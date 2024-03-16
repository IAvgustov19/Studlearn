using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using SW.Core.DataLayer.Notifications;
using SW.Shared.Models.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SW.Frontend.Controllers
{
    public class UnsubscribeController : Controller
    {
        private IAccountUOW _accountUOW { get; set; }
        private INotificationsUOW _notificationsUOW { get; set; }
        private IDocumentsUOW _documentsUOW { get; set; }

        public UnsubscribeController(IAccountUOW accountUOW, INotificationsUOW notificationUOW, IDocumentsUOW documentsUOW)
        {
            _accountUOW = accountUOW;
            _notificationsUOW = notificationUOW;
            _documentsUOW = documentsUOW;
        }

        // GET: Unsubscribe
        public ActionResult Index(string id)
        {
            var mail = _notificationsUOW.QueuedMailsRepository.GetAll()
                .FirstOrDefault(x => x.UnsubscribeHash == id);

            if (mail == null)
                return RedirectToAction("NotFound");

            var subscription = _accountUOW.SubscriptionsRepository.GetAll().FirstOrDefault(x => x.Email == mail.Recepient);
            if (subscription == null)
                return RedirectToAction("NotFound");

            var groups = new List<UserSubscription>();
            var user = _accountUOW.UsersRepository.GetAll().FirstOrDefault(x => x.Email == mail.Recepient);

            var buyer = _documentsUOW.DocumentSalesRepository.GetAll().FirstOrDefault(x => x.BuyerEmail == mail.Recepient);


            groups = _accountUOW.DeliveryGroupsRepository.GetAll()
                .Where(x => x.Code != "Authors" && x.Code != "Buyers")
                .ToList()
                .Select(x => new UserSubscription
                {
                    Id = x.Id,
                    Title = x.Code,
                    Description = x.Description,
                    Active = subscription != null ? subscription.DeliveryGroups.Any(y => y.Id == x.Id) : false
                })
                .ToList();

            if (user != null)
                groups.AddRange(_accountUOW.DeliveryGroupsRepository.GetAll()
                    .Where(x => x.Code == "Authors")
                    .ToList()
                    .Select(x => new UserSubscription
                    {
                        Id = x.Id,
                        Title = x.Code,
                        Description = x.Description,
                        Active = subscription != null ? subscription.DeliveryGroups.Any(y => y.Id == x.Id) : false
                    })
                    .ToList());

            if (buyer != null)
                groups.AddRange(_accountUOW.DeliveryGroupsRepository.GetAll()
                    .Where(x => x.Code == "Buyers")
                    .ToList()
                    .Select(x => new UserSubscription
                    {
                        Id = x.Id,
                        Title = x.Code,
                        Description = x.Description,
                        Active = subscription != null ? subscription.DeliveryGroups.Any(y => y.Id == x.Id) : false
                    })
                    .ToList());

            ViewBag.Id = id;
            ViewBag.Email = mail.Recepient;
            return View(groups);
        }

        public ActionResult NotFound()
        {
            return View();
        }

        public ActionResult Apply(string id, int[] subscriptions = null)
        {
            var mail = _notificationsUOW.QueuedMailsRepository.GetAll()
                .FirstOrDefault(x => x.UnsubscribeHash == id);

            if (mail == null)
                return RedirectToAction("NotFound");

            var subscription = _accountUOW.SubscriptionsRepository.GetAll().FirstOrDefault(x => x.Email == mail.Recepient);
            if (subscription == null)
                return RedirectToAction("NotFound");

            subscription.DeliveryGroups.Clear();
            if (subscriptions != null)
                foreach (var group in subscriptions)
                {
                    var dbGroup = _accountUOW.DeliveryGroupsRepository.GetByID(group);
                    subscription.DeliveryGroups.Add(dbGroup);
                }
            _accountUOW.Commit();

            return RedirectToAction("Index", "Home");
        }

        public ActionResult DeactivateSubscription(string id)
        {
            var mail = _notificationsUOW.QueuedMailsRepository.GetAll()
                .FirstOrDefault(x => x.UnsubscribeHash == id);

            if (mail == null)
                return RedirectToAction("NotFound");

            var subscription = _accountUOW.SubscriptionsRepository.GetAll().FirstOrDefault(x => x.Email == mail.Recepient);
            if (subscription == null)
                return RedirectToAction("NotFound");

            subscription.IsActive = false;
            _accountUOW.Commit();

            return RedirectToAction("Index", "Home");
        }
    }
}