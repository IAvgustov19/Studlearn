using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using SW.Frontend.Optimization;

namespace SW.Frontend.App_Start
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            //BundleTable.EnableOptimizations = false;

            #region essentials

            var essentialJquery = new ScriptBundle("~/bundles/essentials/jquery")
                .Include(
                    "~/metronic/assets/global/plugins/jquery.min.js",
                    "~/metronic/assets/global/plugins/jquery-migrate.min.js",
                    "~/metronic/assets/global/plugins/fancybox/source/jquery.fancybox.pack.js",
                    "~/metronic/assets/global/plugins/jquery.blockui.min.js"
                    );
            essentialJquery.Orderer = new AsIsBundleOrderer();
            essentialJquery.Transforms.Add(new JsMinify());
            bundles.Add(essentialJquery);

            var essentialsMetronic = new ScriptBundle("~/bundles/essentials/metronic-js")
                .Include(
                    "~/metronic/assets/global/plugins/bootstrap/js/bootstrap.min.js",
                    "~/metronic/assets/frontend/layout/scripts/back-to-top.js",
                    "~/metronic/assets/global/scripts/metronic.js",
                    "~//metronic/assets/frontend/layout/scripts/layout.js"
                );
            essentialsMetronic.Transforms.Add(new JsMinify());
            bundles.Add(essentialsMetronic);

            var essentialSystem = new ScriptBundle("~/bundles/essentials/system-js")
                .Include(
                    "~/scripts/System/sw.core.js",
                    "~/scripts/System/sw.helpers.js",
                    "~/scripts/System/sw.init.js",
                    "~/scripts/System/sw.extensions.js",
                    "~/scripts/System/sw.subscriber-captcha.js",
                    "~/scripts/System/sw.site-review.js"
                );
            essentialSystem.Transforms.Add(new JsMinify());
            bundles.Add(essentialSystem);

            var essentialsKo = new ScriptBundle("~/bundles/essentials/ko-js")
                .Include(
                    "~/scripts/knockout-3.2.0.js",
                    "~/scripts/knockout.mapping-latest.js",
                    "~/scripts/knockout.validation.debug.js"
                );
            essentialsKo.Transforms.Add(new JsMinify());
            bundles.Add(essentialsKo);

            var essentialPlugins = new ScriptBundle("~/bundles/essentials/plugins-js")
                .Include(
                    "~/scripts/lazyload-echo.js",
                    "~/metronic/assets/global/plugins/carousel-owl-carousel/owl-carousel/owl.carousel.min.js",
                    "~/metronic/assets/global/plugins/bootstrap-toastr/toastr.min.js",
                    "~/metronic/assets/global/plugins/slider-revolution-slider/rs-plugin/js/jquery.themepunch.revolution.min.js",
                    "~/metronic/assets/global/plugins/slider-revolution-slider/rs-plugin/js/jquery.themepunch.tools.min.js",
                    "~/metronic/assets/frontend/pages/scripts/revo-slider-init.js"
                );
            essentialPlugins.Transforms.Add(new JsMinify());
            bundles.Add(essentialPlugins);

            var essentialGeneralCss = new StyleBundle("~/bundles/essentials/general-css")
                .Include("~/metronic/assets/global/plugins/font-awesome/css/font-awesome.min.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/global/plugins/bootstrap/css/bootstrap.min.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/global/plugins/carousel-owl-carousel/owl-carousel/owl.carousel.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/global/plugins/slider-revolution-slider/rs-plugin/css/settings.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/global/plugins/fancybox/source/jquery.fancybox.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/global/css/components.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/global/css/plugins.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/frontend/layout/css/style.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/frontend/layout/css/work-details.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/frontend/layout/css/site-review.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/frontend/pages/css/style-revolution-slider.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/frontend/layout/css/style-responsive.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/frontend/layout/css/custom.css", new CssRewriteUrlTransform())
                //.Include("~/metronic/assets/frontend/layout/css/themes/red.min.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/frontend/layout/css/themes/dark-blue.min.css", new CssRewriteUrlTransform())
                .Include("~/metronic/assets/global/plugins/bootstrap-toastr/toastr.min.css", new CssRewriteUrlTransform());
            essentialGeneralCss.Transforms.Add(new CssMinify());
            bundles.Add(essentialGeneralCss);
            #endregion
        }
    }
}