using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using SW.Frontend.Models;
using System.Configuration;
using System.Collections.Generic;

namespace SW.Frontend.Helpers
{
    public static class SWHelpers
    {
        public static IHtmlString CustomPager(this HtmlHelper helper, string actionName, string controllerName, PagerModel model, string additionalQuery = null) {
            var sb = new StringBuilder("<div class=\"custom-container pagination-container\"><ul class=\"pagination-list\">");
            var url = new UrlHelper(helper.ViewContext.RequestContext);

            if (model.Count == 0)
            {
                return new HtmlString(sb.ToString());
            }

            if (model.CurrentPage - 3 > 1)
            {
                sb.AppendFormat("<li onclick=\"window.location.href=\'{0}{1}\'\">1</li>", url.Action(actionName, controllerName, new { q = model.QueryString }), string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
                sb.Append("<li><span>...</span></li>");
            }

            for (int i = model.CurrentPage - 3 > 1 ? model.CurrentPage - 2 : 1; i <= (model.LastPage - model.CurrentPage > 3 ? model.CurrentPage + 2 : model.LastPage); i++)
            {
                if (i <= model.LastPage)
                {
                    if (i == model.CurrentPage)
                    {
                        sb.AppendFormat("<li class=\"current-page\"><span>{0}</span></li>", i);
                    }
                    else
                    {
                        if (i == 1)
                        {
                            var link = url.Action(actionName, controllerName, new { q = model.QueryString });
                            if (link.Contains("?"))
                            {
                                link += string.IsNullOrEmpty(additionalQuery) ? "" : ("&" + additionalQuery);
                            }
                            else
                            {
                                link += string.IsNullOrEmpty(additionalQuery) ? "" : ("?" + additionalQuery);
                            }
                            sb.AppendFormat("<li onclick=\"window.location.href=\'{0}\'\">{1}</li>", link, i);
                        }
                        else
                            sb.AppendFormat("<li onclick=\"window.location.href=\'{0}{2}\'\">{1}</li>", url.Action(actionName, controllerName, new { q = model.QueryString, page = i }), i, string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
                    }
                }

            }

            if (model.LastPage - model.CurrentPage > 3)
            {
                sb.Append("<li><span class=\"bg-reset\">...</span></li>");
                sb.AppendFormat("<li onclick=\"window.location.href=\'{0}{2}\'\">{1}</li>", url.Action(actionName, controllerName, new { q = model.QueryString, page = model.LastPage }), model.LastPage, string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
            }
            sb.Append("</ul></div>");

            return new HtmlString(sb.ToString());
        }
        public static IHtmlString Pager(this HtmlHelper helper, string actionName, string controllerName, PagerModel model, string additionalQuery = null)
        {
            var sb = new StringBuilder("<div class=\"row\">");
            var url = new UrlHelper(helper.ViewContext.RequestContext);

            sb.AppendFormat("<div class=\"col-md-4 col-sm-4 items-info\">{3} {0} - {1} {4} {2}</div>", (model.CurrentPage - 1) * model.Rows + 1, (model.CurrentPage - 1) * model.Rows + model.Count, model.Total, Resources.Frontend.General.LabelSearchResults, Resources.Frontend.General.LabelSearchResultsOf);
            if (model.Count == 0)
            {
                sb.Append("<div class=\"col-md-8 col-sm-8\">");
                sb.Append("</div></div>");
                return new HtmlString(sb.ToString());
            }
            sb.Append("<div class=\"col-md-8 col-sm-8\">");
            sb.Append("<ul class=\"pagination pull-right\">");
            if ((model.CurrentPage - 1) == 1 || (model.CurrentPage - 1) == 0)
            {
                var link = url.Action(actionName, controllerName, new { q = model.QueryString });
                if (link.Contains("?"))
                {
                    link += string.IsNullOrEmpty(additionalQuery) ? "" : ("&" + additionalQuery);
                }
                else
                {
                    link += string.IsNullOrEmpty(additionalQuery) ? "" : ("?" + additionalQuery);
                }
                if (model.CurrentPage == 1)
                    link = "javascript:void(0);";

                sb.AppendFormat("<li><a href=\"{0}\" title=\"Предыдущая страница\">«</a></li>", link);
            }
            else
            {
                sb.AppendFormat("<li><a href=\"{0}\" title=\"Предыдущая страница\">«</a></li>",
                    model.CurrentPage == 1 ? "javascript:void(0);" : (url.Action(actionName, controllerName, new { q = model.QueryString, page = model.CurrentPage - 1 }) + (string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery)));
            }

            if (model.CurrentPage - 3 > 1)
            {
                sb.AppendFormat("<li><a href=\"{0}{1}\">1</a></li>", url.Action(actionName, controllerName, new { q = model.QueryString }), string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
                sb.Append("<li><span>...</span></li>");
            }

            for (int i = model.CurrentPage - 3 > 1 ? model.CurrentPage - 2 : 1; i <= (model.LastPage - model.CurrentPage > 3 ? model.CurrentPage + 2 : model.LastPage); i++)
            {
                if (i <= model.LastPage)
                {
                    if (i == model.CurrentPage)
                    {
                        sb.AppendFormat("<li><span class=\"bg-primary\">{0}</span></li>", i);
                    }
                    else
                    {
                        if (i == 1)
                        {
                            var link = url.Action(actionName, controllerName, new { q = model.QueryString });
                            if (link.Contains("?"))
                            {
                                link += string.IsNullOrEmpty(additionalQuery) ? "" : ("&" + additionalQuery);
                            }
                            else
                            {
                                link += string.IsNullOrEmpty(additionalQuery) ? "" : ("?" + additionalQuery);
                            }
                            sb.AppendFormat("<li><a href=\"{0}\">{1}</a></li>", link, i);
                        }
                        else
                            sb.AppendFormat("<li><a href=\"{0}{2}\">{1}</a></li>", url.Action(actionName, controllerName, new { q = model.QueryString, page = i }), i, string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
                    }
                }

            }

            if (model.LastPage - model.CurrentPage > 3)
            {
                sb.Append("<li><span class=\"bg-reset\">...</span></li>");
                sb.AppendFormat("<li><a href=\"{0}{2}\">{1}</a></li>", url.Action(actionName, controllerName, new { q = model.QueryString, page = model.LastPage }), model.LastPage, string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
            }

            sb.AppendFormat("<li><a href=\"{0}\" title=\"Следующая страница\">»</a></li>", model.CurrentPage == model.LastPage ? "javascript:void(0);" : (url.Action(actionName, controllerName, new { q = model.QueryString, page = model.CurrentPage + 1 }) + (string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery)));
            sb.Append("</ul></div></div>");

            return new HtmlString(sb.ToString());
        }

        public static IHtmlString PrevNextLinksBlock(this HtmlHelper helper, string actionName, string controllerName, PagerModel model, string additionalQuery = null)
        {
            var sb = new StringBuilder("");
            var url = new UrlHelper(helper.ViewContext.RequestContext);
            if ((model.CurrentPage - 1) == 1 || (model.CurrentPage - 1) == 0)
            {
                var link = ConfigurationManager.AppSettings["frontendUrl"] + url.Action(actionName, controllerName, new { q = model.QueryString });
                if (link.Contains("?"))
                {
                    link += string.IsNullOrEmpty(additionalQuery) ? "" : ("&" + additionalQuery);
                }
                else
                {
                    link += string.IsNullOrEmpty(additionalQuery) ? "" : ("?" + additionalQuery);
                }
                if (model.CurrentPage != 1)
                    sb.AppendFormat("<link href=\"{0}\" rel=\"prev\">", link);
            }
            else
            {
                if (model.CurrentPage != 1)
                    sb.AppendFormat("<link href=\"{0}\" rel=\"prev\">",
                        (ConfigurationManager.AppSettings["frontendUrl"] + url.Action(actionName, controllerName, new { q = model.QueryString, page = model.CurrentPage - 1 }) + (string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery)));
            }
            if (model.CurrentPage != model.LastPage)
                sb.AppendFormat("<link href=\"{0}\" rel=\"next\">", ConfigurationManager.AppSettings["frontendUrl"] + (url.Action(actionName, controllerName, new { q = model.QueryString, page = model.CurrentPage + 1 }) + (string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery)));
            return new HtmlString(sb.ToString());
        }

        public static IHtmlString Pager(this HtmlHelper helper, string actionName, PagerModel model, string additionalQuery = null)
        {
            var sb = new StringBuilder("<div class=\"row\">");
            var url = new UrlHelper(helper.ViewContext.RequestContext);

            sb.AppendFormat("<div class=\"col-md-4 col-sm-4 items-info\">{3} {0} - {1} {4} {2}</div>", (model.CurrentPage - 1) * model.Rows + 1, (model.CurrentPage - 1) * model.Rows + model.Count, model.Total, Resources.Frontend.General.LabelSearchResults, Resources.Frontend.General.LabelSearchResultsOf);
            sb.Append("<div class=\"col-md-8 col-sm-8\">");
            sb.Append("<ul class=\"pagination pull-right\">");
            sb.AppendFormat("<li><a href=\"{0}{1}\">«</a></li>", model.CurrentPage == 1 ? "javascript:void(0);" : url.Action(actionName, new { q = model.QueryString, page = model.CurrentPage - 1 }), string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);

            if (model.CurrentPage - 3 > 1)
            {
                sb.AppendFormat("<li><a href=\"{0}{1}\">1</a></li>", url.Action(actionName, new { q = model.QueryString, page = 1 }), string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
                sb.Append("<li><span>...</span></li>");
            }

            for (int i = model.CurrentPage - 3 > 1 ? model.CurrentPage - 2 : 1; i <= (model.LastPage - model.CurrentPage > 3 ? model.CurrentPage + 2 : model.LastPage); i++)
            {
                if (i <= model.LastPage)
                {
                    if (i == model.CurrentPage)
                    {
                        sb.AppendFormat("<li><span>{0}</span></li>", i);
                    }
                    else
                    {
                        sb.AppendFormat("<li><a href=\"{0}{2}\">{1}</a></li>", url.Action(actionName, new { q = model.QueryString, page = i }), i, string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
                    }
                }

            }

            if (model.LastPage - model.CurrentPage > 3)
            {
                sb.Append("<li><span>...</span></li>");
                sb.AppendFormat("<li><a href=\"{0}{2}\">{1}</a></li>", url.Action(actionName, new { q = model.QueryString, page = model.LastPage }), model.LastPage, string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
            }

            sb.AppendFormat("<li><a href=\"{0}{1}\">»</a></li>", model.CurrentPage == model.LastPage ? "javascript:void(0);" : url.Action(actionName, new { q = model.QueryString, page = model.CurrentPage + 1 }), string.IsNullOrEmpty(additionalQuery) ? "" : "&" + additionalQuery);
            sb.Append("</ul></div></div>");

            return new HtmlString(sb.ToString());
        }
    }
}