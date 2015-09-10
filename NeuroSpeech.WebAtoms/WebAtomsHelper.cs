using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;
using System.Web.Handlers;
using System.Web.UI;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NeuroSpeech.WebAtoms;


[assembly: WebResource("NeuroSpeech.WebAtoms.Scripts.WebAtoms.Debug.js", "application/x-javascript")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Scripts.FlashPlayer.js", "application/x-javascript")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Scripts.jwplayer.js", "application/x-javascript")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Scripts.JSON.js", "application/x-javascript")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Scripts.ie9.js", "application/x-javascript")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Scripts.linq.min.js", "application/x-javascript")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Scripts.WebAtoms.js", "application/x-javascript")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Content.WebAtoms.css", "text/css")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Content.Busy.svg", "image/svg+xml")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Content.busy.gif", "image/gif")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Content.Icons.png", "image/png")]

[assembly: WebResource("NeuroSpeech.WebAtoms.Content.Flash.playerProductInstall.swf", "application/flash")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Content.Flash.FileUploader.swf", "application/flash")]


[assembly: WebResource("NeuroSpeech.WebAtoms.Content.buttons.up.png", "image/png")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Content.buttons.down.png", "image/png")]
[assembly: WebResource("NeuroSpeech.WebAtoms.Content.buttons.delete.png", "image/png")]


namespace System.Web.Mvc
{
	public class WebAtomsType { 
	}

	public static class WebAtomsHelper
	{

		private static string ScriptTag(string url) {

			return "<script type='text/javascript' src='" +  url+ "'></script>\r\n";
		}
		private static string StyleTag(string url)
		{
			return "<link href='" + url + "' type='text/css' rel='stylesheet' />\r\n";
		}

		private static string ResourceUrl(string type,bool subtitute = false) {
			string pad = "";
			if (subtitute) {
				pad += "&Substitute=true";
			}

			string url = "/resources/NeuroSpeech.WebAtoms/" + type + "?v=" + GetVersion() + "" + pad;
            if (CachedRoute.CDNHost != null) {
                url = "//" + CachedRoute.CDNHost + url;
            }
            return url;
		}

		public static HtmlString WebAtomsIE9Fix(this HtmlHelper html) {

			var fix = "<!--[if gte IE 9]>" +
					"<style type=\"text/css\">" +
			":root *" +
			"{" +
			"    filter: progid:DXImageTransform.Microsoft.gradient(enabled='false') !important;" +
			"}		</style>" +
			"<![endif]-->";
			return new HtmlString(fix);
		}

		public static string WebAtoms(IDictionary<string, object> viewData, bool debug, bool serializeViewBag, bool serializeAllItems)
		{
			string json = ScriptTag(ResourceUrl("Scripts/JSON.js")) + ScriptTag(ResourceUrl("Scripts/linq.min.js"));
			string flashPlayer = ScriptTag(ResourceUrl("Scripts/FlashPlayer.js", true));
			string jwPlayer = "\r\n";
			string webAtoms = ScriptTag(ResourceUrl( debug ? "Scripts/WebAtoms.Debug.js" : "Scripts.WebAtoms.js" ));
			string webAtomsCss = StyleTag(ResourceUrl("Content/WebAtoms.css"));
            string ie9 = ScriptTag(ResourceUrl("Scripts/ie9.js"));

			string model = serializeViewBag ? _SerializeViewBag(viewData, serializeAllItems) : "";

			return webAtomsCss + json + flashPlayer + jwPlayer + webAtoms + model + ie9 + WebAtomsIE9Fix(null).ToString();
		}

		public static HtmlString WebAtomsJavaScriptMobile(this HtmlHelper htmlHelper)
		{
			string json = ScriptTag(ResourceUrl("Scripts/JSON.js")) + ScriptTag(ResourceUrl("Scripts/linq.min.js"));
			string webAtoms = ScriptTag(ResourceUrl("Scripts/WebAtoms.js"));
			string webAtomsCss = StyleTag(ResourceUrl("Content/WebAtoms.css"));

			string model = _SerializeViewBag(htmlHelper.ViewData, false);

			var result = webAtomsCss + json + webAtoms + model;

			return new HtmlString(result);
		}


		public static HtmlString WebAtomsJavaScript(this HtmlHelper htmlHelper, bool serializeViewBag = true, bool serializeAllItems = false)
		{
			return new HtmlString(WebAtoms(htmlHelper.ViewData, false, serializeViewBag, serializeAllItems));
		}

		#region private static string GetVersion()
		public static string GetVersion()
		{
			return Assembly.GetAssembly(typeof(WebAtomsType)).GetName().Version.ToString();
		}
		#endregion


		public static HtmlString WebAtomsJavaScriptDebug(this HtmlHelper htmlHelper, bool serializeViewBag = true, bool serializeAllItems = false)
		{
			return new HtmlString(WebAtoms(htmlHelper.ViewData, true, serializeViewBag, serializeAllItems));
		}

        public static HtmlString CachedUrl(this HtmlHelper htmlHelper, string url) {
            try
            {
                if (htmlHelper.ViewContext.HttpContext.Request.QueryString["NoCache"] != null)
                    return new HtmlString(url);
            }
            catch { }
            return CachedRoute.CachedUrl(url);
        }

		public static HtmlString SerializeViewBag(this HtmlHelper htmlHelper, bool allValues = false) {
			return new HtmlString( _SerializeViewBag(htmlHelper.ViewData, allValues) );
		}

		private static String _SerializeViewBag(IDictionary<string, object> viewData, bool allValues = false)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var item in viewData)
			{
				if (item.Value is HtmlString)
				{
					HtmlString hs = item.Value as HtmlString;
					sb.Append(hs.ToString());
				}
				else {
					if (allValues && item.Value != null) {
						sb.Append( item.Value.ToJavaScriptTag(item.Key) );
					}
				}
			}
			return sb.ToString();
		}
	}
}
