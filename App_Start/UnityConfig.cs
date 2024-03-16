using System;
using System.Data.Entity;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using SW.Core.DataLayer.Products;
using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using SW.Frontend.Models;
using SW.Shared.Helpers.Validators.Rules;
using SW.Shared.Helpers.Validators;
using SW.Workflow.Components.Emails;
using SW.Shared.Constants;
using SW.Core.DataLayer.Notifications;
using SW.Core.DataLayer.Storage;
using SW.Workflow.Components;
using SW.Workflow.Components.Storage;
using SW.Workflow.Components.Files;
using SW.Workflow.Components.User;
using SW.Core.DataLayer.NewsUnit;
using SW.Core.DataLayer.HellGates;
using SW.Core.DataLayer.Sliders;
using SW.Workflow.Components.Cleaning;
using SW.Workflow.Components.TextSearch;
using SW.Workflow.Components.Statistics;
using SW.Shared.Models.Documents;
using SW.Core.DataLayer.ExternalWriters;
using SW.Frontend.Microdata.Products;
using SW.Shared.Models.News;

namespace SW.Frontend.App_Start
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public class UnityConfig
    {
        #region Unity Container
        private static Lazy<IUnityContainer> container = new Lazy<IUnityContainer>(() =>
        {
            var container = new UnityContainer();
            RegisterTypes(container);
            return container;
        });

        /// <summary>
        /// Gets the configured Unity container.
        /// </summary>
        public static IUnityContainer GetConfiguredContainer()
        {
            return container.Value;
        }
        #endregion

        /// <summary>Registers the type mappings with the Unity container.</summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>There is no need to register concrete types such as controllers or API controllers (unless you want to 
        /// change the defaults), as Unity allows resolving a concrete type even if it was not previously registered.</remarks>
        public static void RegisterTypes(IUnityContainer container)
        {

            container.RegisterType(typeof(UserManager<>), new InjectionConstructor(typeof(IUserStore<>)));
            container.RegisterType<IUser>(new InjectionFactory(c => c.Resolve<IUser>()));
            container.RegisterType(typeof(IUserStore<>), typeof(UserStore<>));
            container.RegisterType<IdentityUser, ApplicationUser>(new ContainerControlledLifetimeManager());
            container.RegisterType<DbContext, ApplicationDbContext>(new ContainerControlledLifetimeManager());

            container.RegisterType<IDocumentsUOW, DocumentsUOW>()
                .RegisterType<IAccountUOW, AccountUOW>()
                .RegisterType<IProductsUOW, ProductsUOW>()
                .RegisterType<INotificationsUOW, NotificationUOW>()
                .RegisterType<IStorageUOW, StorageUOW>()
                .RegisterType<INewsUOW, NewsUOW>()
                .RegisterType<IHellGateUOW, HellGateUOW>()
                .RegisterType<ISliderUOW, SliderUOW>()
                .RegisterType<IExternalWritersUOW, ExternalWritersUOW>();


            #region Likes

            container
                .RegisterType(typeof(SW.Workflow.Components.Like.ILiker<>), typeof(SW.Workflow.Components.Like.Liker<>));

            #endregion

            #region Validation rules

            container
                .RegisterType<IRule, EmailRFC5322Rule>(ValidationRuleType.Email.ToString());

            #endregion

            #region Email/Notifications

            container
                .RegisterType<ISmtpSettings, SendGridEopSmtpSettings>(ServerPriority.Main.ToString())
                .RegisterType<ISmtpSettings, MadrillSmtpSettings>(ServerPriority.Secondary.ToString())
                .RegisterType<ISmtpSettings, SendPulseSmtpSettings>(ServerPriority.Reserved.ToString());

            container
                .RegisterType<ISmtpMailer, SmtpMailer>(new InjectionConstructor(SW.Shared.Constants.Application.SenderEmail));

            container
                .RegisterType<IEmailsQueueComponent, EmailsQueueComponent>(
                    new InjectionProperty("MainSmtp", container.Resolve<ISmtpSettings>(ServerPriority.Main.ToString())),
                    new InjectionProperty("SecondarySmtp", container.Resolve<ISmtpSettings>(ServerPriority.Secondary.ToString())),
                    new InjectionProperty("ReservedSmtp", container.Resolve<ISmtpSettings>(ServerPriority.Reserved.ToString())),
                    new InjectionProperty("Mailer", container.Resolve<ISmtpMailer>()),
                    new InjectionProperty("NotificationsUOW", typeof(INotificationsUOW)),
                    new InjectionProperty("EmailValidationRule", container.Resolve<IRule>(ValidationRuleType.Email.ToString()))
                );

            container
                .RegisterType<INotificationComponent, NotificationComponent>(
                    new InjectionProperty("NotificationsUOW", typeof(INotificationsUOW))
                );

            #endregion

            #region Storage/Files

            container
                .RegisterType<ITokensIssuer, TokensIssuer>(
                    new InjectionProperty("StorageUOW", container.Resolve<IStorageUOW>())
                );

            #endregion

            #region Components

            container
                .RegisterType<IFilePreviewComponent, FilePreviewComponent>(
                    new InjectionConstructor(SW.Shared.Constants.Application.StorageConnectionStringName),
                    new InjectionProperty("StorageUOW", typeof(IStorageUOW))
                    )
                .RegisterType<IUserRatingComponent, UserRatingComponent>(
                    new InjectionProperty("AccountUOW", typeof(IAccountUOW))
                    )
                .RegisterType<IModeratorNotificationsComponent, ModeratorNotificationsComponent>(
                    new InjectionProperty("DocumentsUOW", typeof(IDocumentsUOW)),
                    new InjectionProperty("AccountUOW", typeof(IAccountUOW)),
                    new InjectionProperty("NotificationsComponent", typeof(INotificationComponent))
                );

            container
                .RegisterType<ICleaningComponent, DbMailsCleaningComponent>("dbMails",
                    new InjectionProperty("NotificationsUOW", typeof(INotificationsUOW)),
                    new InjectionProperty("Days", 7)
                )
                .RegisterType<ICleaningComponent, DbInvoicesCleaningComponent>("dbInvoices",
                    new InjectionProperty("DocumentsUOW", typeof(IDocumentsUOW)),
                    new InjectionProperty("Days", 7)
                )
                .RegisterType<ICleaningComponent, DbSlugsCleaning>("dbSlugs",
                    new InjectionProperty("DocumentsUOW", typeof(IDocumentsUOW)),
                    new InjectionProperty("AccountUOW", typeof(IAccountUOW)),
                    new InjectionProperty("ExternalWritersUOW", typeof(IExternalWritersUOW)),
                    new InjectionProperty("Limit", 50)
                )
                .RegisterType<ICleaningComponent, DbBannedAndDeletedWorksCleaningComponent>("dbWorks",
                    new InjectionProperty("DocumentsUOW", typeof(IDocumentsUOW)),
                    new InjectionProperty("Limit", 50),
                    new InjectionProperty("Days", 7)
                );

            container
                .RegisterType<ISubscriber, Subscriber>(
                    new InjectionProperty("AccountUOW", typeof(IAccountUOW)),
                    new InjectionProperty("EmailRule", container.Resolve<IRule>(ValidationRuleType.Email.ToString()))
                );

            container
                .RegisterType<IDayStatsComponent<DocumentVisitDto>, DocumentVisitsComponent>(
                    new InjectionProperty("DocumentsUOW", typeof(IDocumentsUOW))
                )
                .RegisterType<IDayStatsComponent<DocumentDownloadDto>, DocumentDownloadsComponent>(
                    new InjectionProperty("DocumentsUOW", typeof(IDocumentsUOW))
                )
                .RegisterType<IDayStatsComponent<NewsVisitDto>, NewsVisitsComponent>(
                    new InjectionProperty("NewsUOW", typeof(INewsUOW))
                );
            #endregion

            container
                .RegisterType<ISearchText, TextSearch>("lucene",
                    new InjectionProperty("DocumentsUOW", typeof(IDocumentsUOW)),
                    new InjectionProperty("ExternalWritersUOW", typeof(IExternalWritersUOW))
                );

            #region microdata
            container
                .RegisterType<IProductRichSnippet, ProductJsonldSnippet>(Microdata.Format.JSONLD.ToString());
            #endregion
        }
    }
}
