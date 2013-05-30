﻿
namespace Sitecore.Feedback.Module.BusinessLayer.Configuration
{
  using Sitecore.Diagnostics;
  using Sitecore.Install.Framework;
  using Sitecore.Jobs.AsyncUI;
  using Sitecore.Text;
  using Sitecore.Web.UI.Sheer;
  using System;
  using System.Collections.Specialized;
  using System.IO;
  using System.ServiceModel.Activation;
  using System.Text.RegularExpressions;
  using System.Web.Hosting;
  using System.Xml;

  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
  public class PostStep : IPostStep
  {
    public void Run(ITaskOutput output, NameValueCollection metaData)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(metaData, "metaData");
      ChangeWebConfig();
      JobContext.SendMessage("sfm:controlpanel");

    }
    protected void Process(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (args.IsPostBack) return;
      SheerResponse.ShowModalDialog(new UrlString(UIUtil.GetUri("control:JetstreamControlPanel")).ToString(), true);
      args.WaitForPostBack();
    }

    public void Run(ClientPipelineArgs args)
    {
      if (!args.IsPostBack)
      {
        var urlString = new UrlString(UIUtil.GetUri("control:JetstreamControlPanel"));
        SheerResponse.ShowModalDialog(urlString.ToString(), true);
        args.WaitForPostBack();
      }
      else
      {
        if (args.HasResult)
        {
          SheerResponse.Alert("Response from JetstreamControlPanel: " + args.Result);
        }
      }
    }

    private static void ChangeWebConfig()
    {
      try
      {
        var webConfigFile = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "web.config");
        var fiWebConfig = new FileInfo(webConfigFile);
        Assert.IsNotNull(fiWebConfig, "Can't find webConfigFile.");
        if (fiWebConfig.IsReadOnly)
        {
          File.SetAttributes(webConfigFile, FileAttributes.Normal);
        }
        var webConfigDoc = new XmlDocument();
        webConfigDoc.Load(webConfigFile);

        var nodeErrorsDefault =
          webConfigDoc.SelectSingleNode(string.Format(
            "//configuration/system.web/customErrors[@defaultRedirect='{0}']", Constants.FeedbackErrorPageUrl));
        var nodeErrorsCurrent = webConfigDoc.SelectSingleNode("//configuration/system.web/customErrors");
        if (nodeErrorsDefault == null && nodeErrorsCurrent != null)
        {
          if (nodeErrorsCurrent.Attributes != null)
          {
            if (nodeErrorsCurrent.Attributes["mode"] != null)
              nodeErrorsCurrent.Attributes["mode"].Value = "On";
            else
            {
              var attrMode = webConfigDoc.CreateAttribute("mode");
              attrMode.Value = "On";
              nodeErrorsCurrent.Attributes.Append(attrMode);
            }
            if (nodeErrorsCurrent.Attributes["defaultRedirect"] != null)
              nodeErrorsCurrent.Attributes["defaultRedirect"].Value = Constants.FeedbackErrorPageUrl;
            else
            {
              var attrDefaultRedirect = webConfigDoc.CreateAttribute("defaultRedirect");
              attrDefaultRedirect.Value = Constants.FeedbackErrorPageUrl;
              nodeErrorsCurrent.Attributes.Append(attrDefaultRedirect);
            }
            if (nodeErrorsCurrent.Attributes["redirectMode"] != null)
              nodeErrorsCurrent.Attributes["redirectMode"].Value = "ResponseRewrite";
            else
            {
              var attrRedirectMode = webConfigDoc.CreateAttribute("redirectMode");
              attrRedirectMode.Value = "ResponseRewrite";
              nodeErrorsCurrent.Attributes.Append(attrRedirectMode);
            }
          }
          webConfigDoc.Save(webConfigFile);
        }
      }
      catch (Exception ex)
      {
        Log.Error("Change Web Config ", ex, typeof(Util));
      }
    }
  }

  public static class Util
  {
    public static string StripTagsRegexCompiled(string source)
    {
      return HtmlRegex.Replace(source, string.Empty);
    }
    private static readonly Regex HtmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

  }

}