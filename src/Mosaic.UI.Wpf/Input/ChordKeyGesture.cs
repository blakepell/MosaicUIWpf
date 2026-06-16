/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Input
{
    /// <summary>
    /// A key gesture that requires two keys to be pressed in sequence.
    /// </summary>
    public class KeyChordGesture : KeyGesture
    {
        private readonly Key _key;
        private bool _gotFirstGesture;
        private readonly InputGesture _firstGesture;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="firstKey"></param>
        /// <param name="secondKey"></param>
        public KeyChordGesture(ModifierKeys modifier, Key firstKey, Key secondKey) : base(Key.None)
        {
            _firstGesture = new KeyGesture(firstKey, modifier);
            _key = secondKey;
        }

        /// <summary>
        /// Determines if the input matches the gesture.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="inputEventArgs"></param>
        public override bool Matches(object obj, InputEventArgs inputEventArgs)
        {
            var keyArgs = inputEventArgs as KeyEventArgs;

            if (keyArgs == null || keyArgs.IsRepeat)
            {
                return false;
            }

            if (_gotFirstGesture)
            {
                _gotFirstGesture = false;

                if (keyArgs.Key == _key)
                {
                    inputEventArgs.Handled = true;
                }

                return keyArgs.Key == _key;
            }

            _gotFirstGesture = _firstGesture.Matches(null, inputEventArgs);

            if (_gotFirstGesture)
            {
                inputEventArgs.Handled = true;
            }

            return false;
        }
    }
}
