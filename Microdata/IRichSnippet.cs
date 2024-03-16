using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW.Frontend.Microdata
{
    /// <summary>
    /// https://developers.google.com/structured-data/rich-snippets/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    interface IRichSnippet<T>
        where T : class
    {
        string Build(T model);
    }
}
