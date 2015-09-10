using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace NeuroSpeech.WebAtoms.Mvc
{
    public class CompressionFilterAttribute : ActionFilterAttribute
    {

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var context = filterContext.HttpContext;

            var ae = context.Request.Headers["Accept-Encoding"];
            if (ae != null)
            {
                ae = ae.ToLower();
                var response = context.Response;
                if (ae.Contains("gzip"))
                {
                    response.AddHeader("Content-Encoding", "gzip");
                    response.Filter =
                        new System.IO.Compression.GZipStream(response.Filter, System.IO.Compression.CompressionMode.Compress);
                }
                else if (ae.Contains("deflate")) {
                    response.AddHeader("Content-Encoding", "deflate");
                    response.Filter =
                        new System.IO.Compression.DeflateStream(response.Filter, System.IO.Compression.CompressionMode.Compress);
                }
            }

            base.OnResultExecuting(filterContext);
        }

    }

}
