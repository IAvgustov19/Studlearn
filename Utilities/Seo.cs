using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Shared.Helpers.HTML;
using SW.Shared.Helpers.Essential;

namespace SW.Frontend.Utilities
{
    public static class Seo
    {
        public static string GetWorkTitle(string title, string type, string category)
        {
            return string.Format("{0} {1} {2} скачать | купить - Studlearn.com",
                title, type, category);
        }

        public static string GetThemeTitle(string title, int page, int type)
        {
            string typeDes = type == 0 ? "" : string.Format("(по типу {0})", type);
            if (page == 0 || page == 1)
                return string.Format("Курсовые и дипломные работы по программированию на тему «{0}» – Скачать {1} | Купить на Studlearn.com",
                    title, typeDes);
            return string.Format("Курсовые и дипломные работы по программированию на тему «{0}» cтраница {1}  – Скачать {2} | Купить на Studlearn.com",
                title, page, typeDes);
        }
        public static string GetThemeDescription(string title)
        {
            return string.Format("На нашем сайте вы можете скачать исходники различных программ на тему «{0}». Основы для написания дипломных, курсовых, лабораторных работ по программированию. Вся информация проверена.",
                title);
        }
        public static string GetCategoryTitle(string title, int page, int type)
        {
            string typeDes = type == 0 ? "" : string.Format("(по типу {0})", type);
            if (page == 0 || page == 1)
                return string.Format("{0} курсовая, диплом, реферат, скачать {1} | купить - Studlearn.com",
                    title, typeDes);
            return string.Format("{0} cтраница {1} курсовая, диплом, реферат, скачать {2} | купить - Studlearn.com",
                title, page, typeDes);
        }

        public static string GetCategoryDescription(string title, int page, int type)
        {
            return SanitizeMetadaDescription(GetCategoryTitle(title, page, type));
        }



        public static string GetAuthorTitle(string name)
        {
            return string.Format("Работы автора {0} - Studlearn.com", name);
        }

        public static string GetAuthorMetaDescription(string name)
        {
            string des = string.Format("{0} | лучшие работы, репутация, статистика и достижения", GetAuthorTitle(name));
            return SanitizeMetadaDescription(des);
        }

        public static string GetNewsItemTitle(string title)
        {
            return string.Format("{0} | Новости. Советы. Статьи - Studlearn.com", title);
        }

        public static string GetNewsListTitle(int page)
        {
            return string.Format("Новости. Советы. Статьи - страница {0} | Studlearn.com", page);
        }

        /// <summary>
        /// Exclude HTML tags, check length best practices
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string SanitizeMetadaDescription(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;
            if (content.Length < 60)
                return null;
            return content
                .ConvertToPlainText()
                .SubstringEx(160);    // https://moz.com/learn/seo/meta-description        
        }
    }
}
