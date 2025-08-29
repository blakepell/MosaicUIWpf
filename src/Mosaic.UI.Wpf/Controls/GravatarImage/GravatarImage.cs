/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Globalization;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using Cysharp.Text;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Displays a Gravatar Image for a specified email address.
    /// </summary>
    public class GravatarImage : Image
    {
        /// <summary>
        /// Identifies the <see cref="Email"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EmailProperty = DependencyProperty.Register(
                nameof(Email), typeof(string), typeof(GravatarImage), new PropertyMetadata(string.Empty, OnPropertyChanged));

        /// <summary>
        /// Gets or sets the email address used to retrieve the Gravatar image.
        /// </summary>
        public string Email
        {
            get => (string)GetValue(EmailProperty);
            set => SetValue(EmailProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            nameof(Size), typeof(int), typeof(GravatarImage), new PropertyMetadata(80, OnPropertyChanged));

        /// <summary>
        /// Gets or sets the size of the Gravatar image in pixels.
        /// </summary>
        public int Size
        {
            get => (int)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DefaultImage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultImageProperty = DependencyProperty.Register(
            nameof(DefaultImage), typeof(string), typeof(GravatarImage), new PropertyMetadata("mm", OnPropertyChanged));

        /// <summary>
        /// Gets or sets the default image type to display if the email address has no associated Gravatar.
        /// </summary>
        public string DefaultImage
        {
            get => (string)GetValue(DefaultImageProperty);
            set => SetValue(DefaultImageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Rating"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RatingProperty = DependencyProperty.Register(
            nameof(Rating), typeof(string), typeof(GravatarImage), new PropertyMetadata("g", OnPropertyChanged));

        /// <summary>
        /// Gets or sets the rating of the Gravatar image (e.g., "g", "pg", "r", "x").
        /// </summary>
        public string Rating
        {
            get => (string)GetValue(RatingProperty);
            set => SetValue(RatingProperty, value);
        }

        /// <summary>
        /// Called when a dependency property value has changed.
        /// </summary>
        /// <param name="d">The dependency object on which the property has changed.</param>
        /// <param name="e">Event data that describes the property change.</param>
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GravatarImage control)
            {
                control.UpdateSource();
            }
        }

        /// <summary>
        /// Updates the <see cref="Source"/> property with the new Gravatar image based on the current properties.
        /// </summary>
        private void UpdateSource()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                Source = null;
                return;
            }

            string emailHash = ComputeEmailHash(Email);
            string url = $"https://secure.gravatar.com/avatar/{emailHash}?s={Size}&d={DefaultImage}&r={Rating}";

            Source = new BitmapImage(new Uri(url, UriKind.Absolute));
        }

        /// <summary>
        /// Computes the MD5 hash of the provided email address.
        /// </summary>
        /// <param name="email">The email address to hash.</param>
        /// <returns>The MD5 hash of the email address as a hexadecimal string.</returns>
        private static string ComputeEmailHash(string email)
        {
            using var md5 = MD5.Create();
            byte[] emailBytes = Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant());
            byte[] hashBytes = md5.ComputeHash(emailBytes);

            using (var sb = ZString.CreateStringBuilder())
            {
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }

                return sb.ToString();
            }
        }
    }
}