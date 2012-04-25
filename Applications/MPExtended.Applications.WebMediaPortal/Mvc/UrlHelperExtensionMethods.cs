﻿#region Copyright (C) 2012 MPExtended
// Copyright (C) 2012 MPExtended Developers, http://mpextended.github.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MPExtended.Applications.WebMediaPortal.Code;

namespace MPExtended.Applications.WebMediaPortal.Mvc
{
    public static class UrlHelperExtensionMethods
    {
        private static string ViewLocalPath(HttpContextBase context, string viewFile)
        {
            var relativePath = ViewEngines.Engines.OfType<SkinnableViewEngine>()
                .Select(sve => sve.BaseDirectory + "/" + viewFile)
                .FirstOrDefault(path => File.Exists(context.Server.MapPath(path)));
            if (relativePath == null)
            {
                relativePath = "~/Views/" + viewFile;
            }
            return relativePath;
        }

        public static string ViewLocalPath(this UrlHelper helper, string viewFile)
        {
            return ViewLocalPath(helper.RequestContext.HttpContext, viewFile);
        }

        public static string ViewContent(this UrlHelper helper, string viewContentPath)
        {
            return helper.Content(helper.ViewLocalPath(viewContentPath));
        }

        public static string GenerateViewContentUrl(string viewContentPath, HttpContextBase httpContext)
        {
            return UrlHelper.GenerateContentUrl(ViewLocalPath(httpContext, viewContentPath), httpContext);
        }
    }
}