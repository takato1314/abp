using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;
using Markdig;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Localization;
using Volo.Blogging.Localization;

namespace Volo.Blogging.Pages.Blog
{
    public class BloggingPageHelper : ITransientDependency
    {
        public IHtmlLocalizer<BloggingResource> L { get; set; }

        public const string DefaultTitle = "Blog";

        public const int MaxShortContentLength = 200;

        public string GetTitle(string title = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return DefaultTitle;
            }

            return title;
        }

        public string GetShortContent(string content)
        {
            var html = RenderMarkdownToHtmlAsString(content);
            var plainText = Regex.Replace(html, "<[^>]*>", "");

            if (string.IsNullOrWhiteSpace(plainText))
            {
                return "";
            }

            var shortContent = new StringBuilder();
            var lines = plainText.Split(Environment.NewLine).Where(s => !string.IsNullOrWhiteSpace(s));

            foreach (var line in lines)
            {
                if (shortContent.Length < MaxShortContentLength)
                {
                    shortContent.Append($" {line}");
                }

                if(shortContent.Length >= MaxShortContentLength)
                {
                    return shortContent.ToString().Substring(0, MaxShortContentLength) + "...";
                }
            }

            return shortContent.ToString();
        }

        public IHtmlContent RenderMarkdownToHtml(string content)
        {
            if(content.IsNullOrWhiteSpace())
            {
                return new HtmlString("");
            }

            var html = RenderMarkdownToHtmlAsString(content);

            html = ReplaceCodeBlocksLanguage(
                html,
                "language-C#",
                "language-csharp"
            );

            return new HtmlString(html);
        }

        protected string ReplaceCodeBlocksLanguage(string content, string currentLanguage, string newLanguage)
        {
            return Regex.Replace(content, "<code class=\"" + currentLanguage + "\">", "<code class=\"" + newLanguage + "\">", RegexOptions.IgnoreCase);
        }

        public string RenderMarkdownToHtmlAsString(string content)
        {
            if (content.IsNullOrWhiteSpace())
            {
                return "";
            }

            return Markdig.Markdown.ToHtml(Encoding.UTF8.GetString(Encoding.Default.GetBytes(content)),
                new MarkdownPipelineBuilder()
                    .UseAutoLinks()
                    .UseBootstrap()
                    .UseGridTables()
                    .UsePipeTables()
                    .Build());
        }

        public LocalizedHtmlString ConvertDatetimeToTimeAgo(DateTime dt)
        {
            var timeDiff = DateTime.Now - dt;

            var diffInDays = (int) timeDiff.TotalDays;

            switch (diffInDays)
            {
                case >= 365:
                    return ConvertDatetimeToTimeAgo("YearsAgo", "YearAgo", diffInDays / 365);
                case >= 30:
                    return ConvertDatetimeToTimeAgo("MonthsAgo", "MonthAgo", diffInDays / 30);
                case >= 7:
                    return ConvertDatetimeToTimeAgo("WeeksAgo", "WeekAgo", diffInDays / 7);
                case >= 1:
                    return ConvertDatetimeToTimeAgo("DaysAgo", "DayAgo", diffInDays);
            }

            var diffInSeconds = (int) timeDiff.TotalSeconds;

            switch (diffInSeconds)
            {
                case >= 3600:
                    return ConvertDatetimeToTimeAgo("HoursAgo", "HourAgo", diffInSeconds / 3600);
                case >= 60:
                    return ConvertDatetimeToTimeAgo("MinutesAgo", "MinuteAgo", diffInSeconds / 60);
                case >= 1:
                    return  ConvertDatetimeToTimeAgo("SecondsAgo", "SecondAgo", diffInSeconds);
                default:
                    return L["Now"];
            }
        }
        
        protected virtual LocalizedHtmlString ConvertDatetimeToTimeAgo(string pluralKey, string singularKey, int value)
        {
            if(value != 1)
            {
                return L[pluralKey, value];
            }
            
            var allStrings = L.GetAllStrings(false);
            
            var singularString = allStrings.FirstOrDefault(s => s.Name == singularKey);
            if(singularString == null || singularString.ResourceNotFound)
            {
                return L[pluralKey, value];
            }

            return new LocalizedHtmlString(singularKey, singularString.Value, singularString.ResourceNotFound, value);
        }
    }
}
