using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace ResumeMVC.Models
{
    public class WordActionResult : ActionResult
    {
        private string _url;
        public string url
        {
            get { return _url; }
        }

        private string _filename;
        public string fileName
        {
            get { return _filename; }
        }

        public WordActionResult(string pUrl, string pFileName)
        {
            _url = pUrl;
            _filename = pFileName;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            HttpContext curContext = HttpContext.Current;
            curContext.Response.Clear();
            curContext.Response.AddHeader("content-disposition", "attachment;filename=" + _filename);
            curContext.Response.Charset = "";
            curContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            curContext.Response.ContentType = "application/ms-word";

            HttpWebRequest wreq = (HttpWebRequest)HttpWebRequest.Create(_url);

            // If we are using forms authentication we need to get the .ASPXAUTH cookie and add it to our request
            var httpCookie = context.HttpContext.Request.Cookies[".ASPXAUTH"];
            wreq.CookieContainer = new CookieContainer();
            wreq.CookieContainer.Add(new Cookie(httpCookie.Name, httpCookie.Value, httpCookie.Path, context.HttpContext.Request.Url.Host));

            HttpWebResponse wres = (HttpWebResponse)wreq.GetResponse();

            using (Stream s = wres.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s, Encoding.ASCII))
                {
                    curContext.Response.Write(sr.ReadToEnd());
                    curContext.Response.End();
                }
            }
        }
    }
}