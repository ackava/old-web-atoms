using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;
using System.IO;
using System.Web.UI;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography;

namespace NeuroSpeech.WebAtoms
{


    using System;
    using System.Text;

    public sealed class Base32
    {

        // the valid chars for the encoding
        private static string ValidChars = "QAZ2WSX3" + "EDC4RFV5" + "TGB6YHN7" + "UJM8K9LP";

        /// <summary>
        /// Converts an array of bytes to a Base32-k string.
        /// </summary>
        public static string ToBase32String(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();         // holds the base32 chars
            byte index;
            int hi = 5;
            int currentByte = 0;

            while (currentByte < bytes.Length)
            {
                // do we need to use the next byte?
                if (hi > 8)
                {
                    // get the last piece from the current byte, shift it to the right
                    // and increment the byte counter
                    index = (byte)(bytes[currentByte++] >> (hi - 5));
                    if (currentByte != bytes.Length)
                    {
                        // if we are not at the end, get the first piece from
                        // the next byte, clear it and shift it to the left
                        index = (byte)(((byte)(bytes[currentByte] << (16 - hi)) >> 3) | index);
                    }

                    hi -= 3;
                }
                else if (hi == 8)
                {
                    index = (byte)(bytes[currentByte++] >> 3);
                    hi -= 3;
                }
                else
                {

                    // simply get the stuff from the current byte
                    index = (byte)((byte)(bytes[currentByte] << (8 - hi)) >> 3);
                    hi += 5;
                }

                sb.Append(ValidChars[index]);
            }

            return sb.ToString();
        }
    }
    public class CachedRoute : HttpTaskAsyncHandler, IRouteHandler
    {

        private CachedRoute()
        {
            // only one per app..
            Enabled = true;
        }

        public static bool Enabled { get; set; }

        public static string CDNHost { get; set; }

        private string Prefix { get; set; }

        public static string Version { get; private set; }

        public static string CORSOrigins { get; set; }

        private TimeSpan MaxAge { get; set; }

        //private static CachedRoute Instance;

        public static void Register(
            RouteCollection routes,
            TimeSpan? maxAge = null,
            string version = null)
        {
            CachedRoute sc = new CachedRoute();
            sc.MaxAge = maxAge == null ? TimeSpan.FromDays(30) : maxAge.Value;

            if (string.IsNullOrWhiteSpace(version))
            {
                version = System.Web.Configuration.WebConfigurationManager.AppSettings["Static-Content-Version"];
                if (string.IsNullOrWhiteSpace(version))
                {
                    version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
            }

            Version = version;

            var route = new Route("cached/{version}/{*name}", sc);
            route.Defaults = new RouteValueDictionary();
            route.Defaults["version"] = "1";
            routes.Add(route);
        }

        public override bool IsReusable
        {
            get
            {
                return true;
            }
        }

        public class CachedFileInfo {

            public string Version { get; set; }

            public string FilePath { get; set; }

            public CachedFileInfo(string path)
            {
                path = HttpContext.Current.Server.MapPath(path);

                FilePath = path;

                //Watch();

                Update(null, null);
            }

            private void Watch()
            {
                System.IO.FileSystemWatcher fs = new FileSystemWatcher(FilePath);
                fs.Changed += Update;
                fs.Deleted += Update;
                fs.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
            }

            private void Update(object sender, FileSystemEventArgs e)
            {
                FileInfo f = new FileInfo(FilePath);
                if (f.Exists)
                {
                    // lets read and create Sha1
                    var s = SHA1.Create();
                    using (var fs = f.OpenRead()) {
                        var hash = s.ComputeHash(fs);
                        Version = Base32.ToBase32String(hash);
                    }
                    // Version = f.LastWriteTimeUtc.ToString("yyyy-MM-dd-hh-mm-ss-FFFF");
                }
                else
                {
                    Version = "null";
                }
            }


        }

        private static ConcurrentDictionary<string, CachedFileInfo> CacheItems = new ConcurrentDictionary<string, CachedFileInfo>();

        public static string CORSHeaders = string.Join(",", new string[] {
            "Accept",
            "If-Modified-Since",
            "Origin",
            "Referer",
            "User-Agent",
            "Cache-Control"
        });

        public static HtmlString CachedUrl(string p)
        {
            //if (!Enabled)
            //    return new HtmlString(p);
            if (!p.StartsWith("/"))
                throw new InvalidOperationException("Please provide full path starting with /");

            string v = Version;

            var cv = CacheItems.GetOrAdd(p, k => new CachedFileInfo(k));
            v = cv.Version;

            if (CDNHost != null)
            {
                return new HtmlString("//" + CDNHost + "/cached/" + v + p);
            }
            return new HtmlString("/cached/" + v + p);
        }

        //[Obsolete("Replace with CachedUrl",true)]
        //public static HtmlString Url(string p)
        //{
        //    throw new InvalidOperationException();
        //}

        public override async System.Threading.Tasks.Task ProcessRequestAsync(HttpContext context)
        {
            var Response = context.Response;
            if (Enabled)
            {
                //Response.Cache.SetExpires(DateTime.UtcNow.Add(MaxAge));
                Response.Cache.SetCacheability(HttpCacheability.Public);
                Response.Cache.SetMaxAge(MaxAge);
                Response.Cache.SetSlidingExpiration(true);
            }
            else
            {
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-10));
            }
            Response.BufferOutput = true;
            if (CORSOrigins != null)
            {
                Response.Headers.Add("Access-Control-Allow-Origin", CORSOrigins);
                Response.Headers.Add("Access-Control-Allow-Headers", CORSHeaders);
            }

            string FilePath = context.Items["FilePath"] as string;

            var file = new FileInfo(context.Server.MapPath("/" + FilePath));
            if (!file.Exists)
            {

                using (var client = new HttpClient()) {

                    var url = new UriBuilder(context.Request.Url);
                    url.Path = "/" + FilePath;

                    var r = await client.GetAsync(url.ToString(), HttpCompletionOption.ResponseHeadersRead);

                    string ct = "text/html";

                    if (r.IsSuccessStatusCode) {
                        Response.Cache.SetCacheability(HttpCacheability.Public);
                        Response.Cache.SetMaxAge(TimeSpan.FromDays(30));
                        Response.Cache.SetSlidingExpiration(true);
                        Response.StatusCode = 200;
                        if (r.Content.Headers.ContentType != null) {
                            ct = r.Content.Headers.ContentType.ToString();
                        }
                    }
                    else
                    {
                        Response.Cache.SetCacheability(HttpCacheability.NoCache);
                        Response.StatusCode = (int)r.StatusCode;
                        Response.StatusDescription = r.ReasonPhrase;
                    }

                    Response.ContentType = ct;

                    var s = await r.Content.ReadAsStreamAsync();

                    await s.CopyToAsync(Response.OutputStream);
                }


                //Response.StatusCode = 404;
                //Response.StatusDescription = "Not Found by CachedRoute";
                //Response.ContentType = "text/plain";
                //Response.Output.Write("File not found by CachedRoute at " + file.FullName);
                return;
                
            }

            Response.ContentType = MimeMapping.GetMimeMapping(file.FullName);

            using (var fs = file.OpenRead())
            {
                await fs.CopyToAsync(Response.OutputStream);
            }
        }

        IHttpHandler IRouteHandler.GetHttpHandler(RequestContext requestContext)
        {
            //FilePath = requestContext.RouteData.GetRequiredString("name");
            requestContext.HttpContext.Items["FilePath"] = requestContext.RouteData.GetRequiredString("name");
            return (IHttpHandler)this;
        }
    }
}
