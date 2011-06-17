#region License

// Copyright 2010 Jeremy Skinner (http://www.jeremyskinner.co.uk)
//  
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at http://github.com/JeremySkinner/git-dot-aspx

#endregion

namespace GitAspx {
	using System;
	using System.ComponentModel;
	using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using System.Linq;

	public static class Helpers {
		static readonly string version;

		static Helpers() {
			version = typeof(Helpers).Assembly.GetName().Version.ToString();
		}

		public static string Version {
			get { return version;}
		}

		public static string ProjectUrl(this UrlHelper urlHelper, string project) {

            var host = urlHelper.RequestContext.HttpContext.Request.Url.Host;
            if (urlHelper.RequestContext.HttpContext.User.Identity.IsAuthenticated)
                host = urlHelper.RequestContext.HttpContext.User.Identity.Name.Split('\\').Last() + "@" + host;

			return urlHelper.RouteUrl("project", new RouteValueDictionary(new {project}),
			                          urlHelper.RequestContext.HttpContext.Request.Url.Scheme,
			                          host);
		}

		public static string ToPrettyDateString(this DateTime d) {
			TimeSpan s = DateTime.Now.Subtract(d);
			int dayDiff = (int)s.TotalDays;
			int secDiff = (int)s.TotalSeconds;

			if (dayDiff < 0 || dayDiff >= 31)
				return string.Format("{0:MMMM d, yyyy}", d);

			if (dayDiff == 0) {
				if (secDiff < 60)
					return "just now";

				if (secDiff < 120)
					return "1 minute ago";

				if (secDiff < 3600)
					return string.Format("{0} minutes ago", Math.Floor((double)secDiff / 60));

				if (secDiff < 7200)
					return "1 hour ago";

				if (secDiff < 86400)
					return string.Format("{0} hours ago", Math.Floor((double)secDiff / 3600));
			}

			if (dayDiff == 1)
				return "yesterday";

			if (dayDiff < 7)
				return string.Format("{0} days ago", dayDiff);

			if (dayDiff < 31)
				return string.Format("{0} weeks ago", Math.Ceiling((double)dayDiff / 7));

			return null;
		}


		public static string With(this string format, params string[] args) {
			return string.Format(format, args);
		}

		public static void WriteNoCache(this HttpResponseBase response) {
			response.AddHeader("Expires", "Fri, 01 Jan 1980 00:00:00 GMT");
			response.AddHeader("Pragma", "no-cache");
			response.AddHeader("Cache-Control", "no-cache, max-age=0, must-revalidate");
		}

		public static void PktWrite(this HttpResponseBase response, string input, params object[] args) {
			input = string.Format(input, args);
			var toWrite = (input.Length + 4).ToString("x").PadLeft(4, '0') + input;
			response.Write(toWrite);
		}

		public static void PktFlush(this HttpResponseBase response) {
			response.Write("0000");
		}
	}
}