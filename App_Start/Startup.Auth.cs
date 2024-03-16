using System.Web;
using System.Web.Mvc;

using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Configuration;


namespace SW.Frontend
{
    public partial class Startup
    {
        public const string ExternalOAuthAuthenticationType = "ExternalToken";
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }
        public static CookieAuthenticationOptions CookieOptions { get; private set; }

        //public static IdentityManagerFactory IdentityManagerFactory { get; set; }

        public static string PublicClientId { get; private set; }

        static Startup()
        {
            PublicClientId = "self";

            //IdentityManagerFactory = new IdentityManagerFactory(IdentityConfig.Settings, () => new IdentityStore());

            //CookieOptions = new CookieAuthenticationOptions();

            //OAuthOptions = new OAuthAuthorizationServerOptions
            //{
            //    TokenEndpointPath = new PathString("/Token"),
            //    AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
            //    Provider = new ApplicationOAuthProvider(PublicClientId, IdentityManagerFactory, CookieOptions)
            //};
        }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Enable the application to use a cookie to store information for the signed in user
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                CookieDomain = ConfigurationManager.AppSettings["subdomain"]
            });
            // Use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);


            //var twitterOptions = new Microsoft.Owin.Security.Twitter.TwitterAuthenticationOptions();
            //twitterOptions.ConsumerKey = ConfigurationManager.AppSettings["twitterKey"];
            //twitterOptions.ConsumerSecret = ConfigurationManager.AppSettings["twitterSecret"];

            ////Twitter
            //app.UseTwitterAuthentication(twitterOptions);

            ////Facebook
            //var facebookOptions = new FacebookAuthenticationOptions();
            //facebookOptions.Scope.Add("email");
            //facebookOptions.AppId = ConfigurationManager.AppSettings["facebookAppId"];
            //facebookOptions.AppSecret = ConfigurationManager.AppSettings["facebookSecret"];

            //facebookOptions.Provider = new FacebookAuthenticationProvider()
            //{
            //    OnAuthenticated = async context =>
            //    {
            //        //Get the access token from FB and store it in the database and
            //        //use FacebookC# SDK to get more information about the user
            //        context.Identity.AddClaim(
            //            new System.Security.Claims.Claim("email",
            //                                             context.Email));
            //        context.Identity.AddClaim(
            //            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.GivenName,
            //                                             context.User["first_name"].ToObject<string>()));
            //        context.Identity.AddClaim(
            //            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Surname,
            //                                             context.User["last_name"].ToObject<string>()));
            //        context.Identity.AddClaim(
            //            new System.Security.Claims.Claim("image",
            //                                             @"https://graph.facebook.com/" + context.User["id"].ToObject<string>() + "/picture?type=large"));
            //    }
            //};

            //app.UseFacebookAuthentication(facebookOptions);


            ////Google+
            //app.UseGoogleAuthentication(new Microsoft.Owin.Security.Google.GoogleOAuth2AuthenticationOptions()
            //    {
            //        ClientId = ConfigurationManager.AppSettings["googlePlusClientId"],
            //        ClientSecret = ConfigurationManager.AppSettings["googlePlusSecret"],
            //        Provider = new Microsoft.Owin.Security.Google.GoogleOAuth2AuthenticationProvider()
            //        {
            //            OnAuthenticated = async context =>
            //            {
            //                context.Identity.AddClaim(
            //                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.GivenName,
            //                                             context.User["name"]["givenName"].ToObject<string>()));
            //                context.Identity.AddClaim(
            //                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Surname,
            //                                                     context.User["name"]["familyName"].ToObject<string>()));

            //                context.Identity.AddClaim(
            //                    new System.Security.Claims.Claim("image",
            //                                                     context.User["image"]["url"].ToObject<String>()));
            //            }
            //        }
            //    }
            //);

            ////VKontakte
            //app.UseVkAuthentication(ConfigurationManager.AppSettings["vkAppId"],
            //    ConfigurationManager.AppSettings["vkSecret"]);

            ////Odnoklassniki
            //app.UseOdnoklassnikiAuthentication(ConfigurationManager.AppSettings["odnoklassnikiClientId"],
            //    ConfigurationManager.AppSettings["odnoklassnikiPublic"],
            //    ConfigurationManager.AppSettings["odnoklassnikiSecret"]);

            //app.UseOAuthBearerTokens(OAuthOptions);
        }
    }
}