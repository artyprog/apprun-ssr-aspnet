﻿using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication2
{
    public class VirtualDomAttribute : FilterAttribute, IResultFilter
    {
        StringWriter textWriter;
        TextWriter originalWriter; 
        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            originalWriter = filterContext.HttpContext.Response.Output;
            textWriter = new StringWriter(CultureInfo.InvariantCulture);
            filterContext.HttpContext.Response.Output = textWriter;
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            var capturedText = textWriter.ToString();
            var doc = new HtmlDocument();
            doc.LoadHtml(capturedText);
            var vdom = RemoveWhiteSpace(Convert(doc.DocumentNode).ToString());
            filterContext.HttpContext.Response.Output = originalWriter;
            filterContext.HttpContext.Response.Write(vdom);
        }

        string RemoveWhiteSpace(string s)
        {
            return s.Replace("\\r", "").Replace("\\n", "").Trim();
        }

        public JObject Convert(HtmlNode documentNode)
        {
            if (documentNode.Name == "#comment") return null;
            if (documentNode.Name == "#document") documentNode.Name = "div";
            var children = new JArray();
            foreach (var child in documentNode.ChildNodes)
            {
                if (child.Name == "#text")
                {
                    if (RemoveWhiteSpace(child.InnerText).Length > 0)
                    {
                        children.Add(new JValue(child.InnerText));
                    }
                }
                else
                {
                    var ch = Convert(child);
                    if (ch != null) children.Add(ch);
                }
            }
            return JObject.FromObject(new
            {
                tag = documentNode.Name,
                props = documentNode.Attributes.Select(attr => new JObject(
                        new JProperty(attr.Name, attr.Value))
                ).ToArray(),
                children = children
            });
        }
    }
    
}