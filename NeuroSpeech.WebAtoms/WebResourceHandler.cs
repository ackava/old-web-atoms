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

namespace NeuroSpeech.WebAtoms
{
	public class WebResourceHandler : IHttpHandler
	{
		public bool IsReusable
		{
			get { return false; }
		}

		public string ResourceName { get; set; }

		public string AssemblyName { get; set; }

		#region public void  ProcessRequest(HttpContext context)
		public void ProcessRequest(HttpContext context)
		{

			Assembly assembly = Assembly.GetExecutingAssembly();

			WebResourceAttribute resource = null;

			object[] items = assembly.GetCustomAttributes(typeof(WebResourceAttribute), false);

            ResourceName = ResourceName.Replace("/", ".");

			if (!ResourceName.StartsWith("NeuroSpeech.WebAtoms."))
				ResourceName = "NeuroSpeech.WebAtoms." + ResourceName;

			foreach (WebResourceAttribute wa in items) {
				if (ResourceName == wa.WebResource) {
					resource = wa;
					break;
				}
			}

			if (resource == null)
				throw new FileNotFoundException();

			context.Response.ContentType = resource.ContentType;

			HttpCachePolicy cache = context.Response.Cache;

			cache.SetCacheability(HttpCacheability.Public);
			cache.SetMaxAge(TimeSpan.FromDays(30));
            cache.SetExpires(DateTime.Now.AddDays(30));

			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
			if (!string.IsNullOrWhiteSpace(context.Request.QueryString["Substitute"]))
			{
				var writer = new StreamReader(stream);
				var text = writer.ReadToEnd();
				var version = WebAtomsHelper.GetVersion();
				text = text.Replace("$$VERSION$$", version);
				context.Response.Write(text);
			}
			else
			{
				stream.CopyTo(context.Response.OutputStream);
			}
		}
		#endregion

	}

	public class WebResourceRouteHandler : IRouteHandler
	{
		#region public IHttpHandler  GetHttpHandler(RequestContext requestContext)
		public IHttpHandler GetHttpHandler(RequestContext requestContext)
		{
			return new WebResourceHandler() { 
				AssemblyName= requestContext.RouteData.Values["assembly"] as string,
				ResourceName = requestContext.RouteData.Values["name"] as string  };
		}
		#endregion
        
		#region public static void Register(RouteCollection routes)
		public static void Register(RouteCollection routes)
		{
			var route = new Route("resources/{*name}", new WebResourceRouteHandler());
			route.Defaults = new RouteValueDictionary();
			route.Defaults["version"] = "1";
			routes.Add(route);

		}
		#endregion
    }


    ///// <summary>
    ///// This class allows access to the internal MimeMapping-Class in System.Web
    ///// </summary>
    //class MimeMappingWrapper
    //{
    //    static MethodInfo getMimeMappingMethod;

    //    static MimeMappingWrapper()
    //    {
    //        // dirty trick - Assembly.LoadWIthPartialName has been depricated
    //        Assembly ass = Assembly.LoadWithPartialName("System.Web");
    //        Type t = ass.GetType("System.Web.MimeMapping");

    //        getMimeMappingMethod = t.GetMethod("GetMimeMapping", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
    //    }

    //    /// <summary>
    //    /// Returns a MIME type depending on the passed files extension
    //    /// </summary>
    //    /// <param name="fileName">File to get a MIME type for</param>
    //    /// <returns>MIME type according to the files extension</returns>
    //    public static string GetMimeMapping(string fileName)
    //    {
    //        return (string)getMimeMappingMethod.Invoke(null, new[] { fileName });
    //    }
    //}
}
