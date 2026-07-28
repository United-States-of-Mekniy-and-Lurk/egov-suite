using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Egov.Platform.Feeds;

/// <summary>
/// Serializes feed channel + items to RSS 2.0 XML.
/// </summary>
public static class RssSerializer
{
    private static readonly XNamespace AtomNs = "http://www.w3.org/2005/Atom";

    public static string Serialize(RssFeedChannel channel, IReadOnlyList<RssFeedItem> items, string? selfUrl = null)
    {
        var channelElement = new XElement("channel",
            new XElement("title", channel.Title),
            new XElement("link", channel.Link),
            new XElement("description", channel.Description),
            new XElement("generator", "Egov.Platform.Feeds"));

        if (!string.IsNullOrWhiteSpace(channel.Language))
            channelElement.Add(new XElement("language", channel.Language));
        if (!string.IsNullOrWhiteSpace(channel.Copyright))
            channelElement.Add(new XElement("copyright", channel.Copyright));
        if (!string.IsNullOrWhiteSpace(channel.ManagingEditor))
            channelElement.Add(new XElement("managingEditor", channel.ManagingEditor));
        if (!string.IsNullOrWhiteSpace(channel.WebMaster))
            channelElement.Add(new XElement("webMaster", channel.WebMaster));
        if (channel.TimeToLiveMinutes.HasValue)
            channelElement.Add(new XElement("ttl", channel.TimeToLiveMinutes.Value));
        if (items.Count > 0)
        {
            var lastBuild = items.Where(i => i.PublishedAt.HasValue).Max(i => i.PublishedAt);
            if (lastBuild.HasValue)
                channelElement.Add(new XElement("lastBuildDate", FormatRfc822(lastBuild.Value)));
        }

        if (!string.IsNullOrWhiteSpace(selfUrl))
        {
            channelElement.Add(new XElement(AtomNs + "link",
                new XAttribute("href", selfUrl),
                new XAttribute("rel", "self"),
                new XAttribute("type", "application/rss+xml")));
        }

        foreach (var item in items)
        {
            var itemElement = new XElement("item",
                new XElement("title", item.Title),
                new XElement("link", item.Link));

            if (!string.IsNullOrWhiteSpace(item.Description))
                itemElement.Add(new XElement("description", item.Description));
            if (item.PublishedAt.HasValue)
                itemElement.Add(new XElement("pubDate", FormatRfc822(item.PublishedAt.Value)));
            if (!string.IsNullOrWhiteSpace(item.Guid))
            {
                var guidElement = new XElement("guid", item.Guid);
                if (!item.GuidIsPermaLink)
                    guidElement.Add(new XAttribute("isPermaLink", "false"));
                itemElement.Add(guidElement);
            }
            if (!string.IsNullOrWhiteSpace(item.Author))
                itemElement.Add(new XElement("author", item.Author));
            foreach (var category in item.Categories)
                itemElement.Add(new XElement("category", category));

            channelElement.Add(itemElement);
        }

        var rssElement = new XElement("rss",
            new XAttribute("version", "2.0"),
            new XAttribute(XNamespace.Xmlns + "atom", AtomNs.NamespaceName),
            channelElement);

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), rssElement);

        using var writer = new StringWriter();
        using var xml = XmlWriter.Create(writer, new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = System.Text.Encoding.UTF8
        });
        doc.WriteTo(xml);
        xml.Flush();
        return writer.ToString();
    }

    private static string FormatRfc822(DateTime dateTime)
    {
        var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
        return utc.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT";
    }
}
