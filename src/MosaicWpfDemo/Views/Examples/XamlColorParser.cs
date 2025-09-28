using System;
using System.Collections.Generic;   
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// A helper class to load a XAML ResourceDictionary, identify editable color/brush
    /// resources, and save changes while preserving file formatting.
    /// </summary>
    public static class XamlColorParser
    {
        public static List<XamlResourceItem> Parse(string xamlContent)
        {
            var items = new List<XamlResourceItem>();
            var xdoc = XDocument.Parse(xamlContent, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var ns = xdoc.Root.GetDefaultNamespace();
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

            var resources = xdoc.Descendants()
                                .Where(d => d.Name.LocalName == "Color" || d.Name.LocalName == "SolidColorBrush");

            foreach (var resource in resources)
            {
                var key = resource.Attribute(x + "Key")?.Value;
                if (string.IsNullOrEmpty(key)) continue;

                if (resource.Name.LocalName == "Color")
                {
                    var textNode = resource.Nodes().OfType<XText>().FirstOrDefault();
                    if (textNode != null)
                    {
                        items.Add(new XamlResourceItem(key, "Color", textNode.Value.Trim(), textNode));
                    }
                }
                else if (resource.Name.LocalName == "SolidColorBrush")
                {
                    var colorAttribute = resource.Attribute("Color");
                    if (colorAttribute != null)
                    {
                        items.Add(new XamlResourceItem(key, "SolidColorBrush", colorAttribute.Value, colorAttribute));
                    }
                }
            }

            return items;
        }

        public static string UpdateXamlContent(string originalContent, IEnumerable<XamlResourceItem> modifiedItems)
        {
            var lines = originalContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var lineChanges = new SortedDictionary<int, List<(int, int, string)>>();

            foreach (var item in modifiedItems.Where(i => i.IsModified))
            {
                if (item.SourceObject is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
                {
                    int lineIndex = lineInfo.LineNumber - 1;
                    if (!lineChanges.ContainsKey(lineIndex))
                    {
                        lineChanges[lineIndex] = new List<(int, int, string)>();
                    }

                    if (item.SourceObject is XAttribute attr)
                    {
                        // For attributes, we need to find the value part within the line
                        var line = lines[lineIndex];
                        var attrValueStartIndex = line.IndexOf(attr.Value, StringComparison.Ordinal);
                        if (attrValueStartIndex != -1)
                        {
                            lineChanges[lineIndex].Add((attrValueStartIndex, attr.Value.Length, item.CurrentValue));
                        }
                    }
                    else if (item.SourceObject is XText text)
                    {
                        // For text nodes, the column is where the text begins
                        var colIndex = lineInfo.LinePosition - 1;
                        lineChanges[lineIndex].Add((colIndex, text.Value.Length, item.CurrentValue));
                    }
                }
            }

            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lineChanges.TryGetValue(i, out var changes))
                {
                    var line = lines[i];
                    var lineSb = new StringBuilder();
                    int lastIndex = 0;
                    // Sort changes by start index to apply them correctly
                    foreach (var (start, length, newValue) in changes.OrderBy(c => c.Item1))
                    {
                        lineSb.Append(line.Substring(lastIndex, start - lastIndex));
                        lineSb.Append(newValue);
                        lastIndex = start + length;
                    }
                    lineSb.Append(line.Substring(lastIndex));
                    sb.AppendLine(lineSb.ToString());
                }
                else
                {
                    sb.AppendLine(lines[i]);
                }
            }

            // Remove the final newline added by AppendLine
            if (sb.Length > Environment.NewLine.Length)
            {
                sb.Length -= Environment.NewLine.Length;
            }

            return sb.ToString();
        }
    }
}