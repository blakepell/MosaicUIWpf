/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;

namespace Mosaic.UI.Wpf.Controls.Behaviors
{
    public class ReflectionDocumentationBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
            nameof(Type),
            typeof(Type),
            typeof(ReflectionDocumentationBehavior),
            new PropertyMetadata(null, OnResourcePathChanged));

        public Type Type
        {
            get => (Type)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }
        private static void OnResourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReflectionDocumentationBehavior behavior && behavior.AssociatedObject != null)
            {
                behavior.SetTextFromResource();
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            SetTextFromResource();
        }

        private void SetTextFromResource()
        {
            if (AssociatedObject == null)
            {
                return;
            }

            try
            {
                if (Type == null)
                {
                    AssociatedObject.Text = "No type specified.";
                    return;
                }

                // Load XML documentation file
                var xmlPath = Path.ChangeExtension(Type.Assembly.Location, ".xml");
                XDocument? xmlDoc = null;
                if (File.Exists(xmlPath))
                {
                    xmlDoc = XDocument.Load(xmlPath);
                }

                string GetXmlSummary(MemberInfo member)
                {
                    if (xmlDoc == null)
                    {
                        return string.Empty;
                    }

                    string? memberName = GetXmlMemberName(member);
                    if (string.IsNullOrEmpty(memberName))
                    {
                        return string.Empty;
                    }

                    var summary = xmlDoc.Descendants("member")
                        .FirstOrDefault(x => (string?)x.Attribute("name") == memberName)?
                        .Element("summary")?.Value?.Trim();
                    return summary ?? string.Empty;
                }

                var sb = new System.Text.StringBuilder();

                // Class header
                sb.AppendLine($"# {Type.FullName}");
                var classSummary = GetXmlSummary(Type);
                if (!string.IsNullOrWhiteSpace(classSummary))
                {
                    sb.AppendLine();
                    sb.AppendLine(classSummary);
                }

                // Properties
                var props = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (props.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("## Properties");
                    foreach (var prop in props)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"- `{prop.PropertyType.Name} {prop.Name}`");
                        var summary = GetXmlSummary(prop);
                        if (!string.IsNullOrWhiteSpace(summary))
                        {
                            sb.AppendLine($"  - {summary}");
                        }
                    }
                }

                // Methods
                var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName).ToArray();
                if (methods.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("## Methods");
                    foreach (var method in methods)
                    {
                        var paramList = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        sb.AppendLine();
                        sb.AppendLine($"- `{method.ReturnType.Name} {method.Name}({paramList})`");
                        var summary = GetXmlSummary(method);
                        if (!string.IsNullOrWhiteSpace(summary))
                        {
                            sb.AppendLine($"  - {summary}");
                        }
                    }
                }

                // Fields
                var fields = Type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (fields.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("## Fields");
                    foreach (var field in fields)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"- `{field.FieldType.Name} {field.Name}`");
                        var summary = GetXmlSummary(field);
                        if (!string.IsNullOrWhiteSpace(summary))
                        {
                            sb.AppendLine($"  - {summary}");
                        }
                    }
                }

                AssociatedObject.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                AssociatedObject.Text = $"Error loading resource: {ex.Message}";
            }
        }

        private static string? GetXmlMemberName(MemberInfo member)
        {
            if (member is Type type)
            {
                return "T:" + type.FullName;
            }

            if (member is PropertyInfo prop)
            {
                return "P:" + prop.DeclaringType?.FullName + "." + prop.Name;
            }

            if (member is FieldInfo field)
            {
                return "F:" + field.DeclaringType?.FullName + "." + field.Name;
            }

            if (member is MethodInfo method)
            {
                var paramTypes = method.GetParameters()
                    .Select(p => p.ParameterType.FullName?.Replace('+', '.'))
                    .ToArray();
                var paramList = paramTypes.Length > 0 ? $"({string.Join(",", paramTypes)})" : "";
                return $"M:{method.DeclaringType?.FullName}.{method.Name}{paramList}";
            }
            return null;
        }
    }
}
