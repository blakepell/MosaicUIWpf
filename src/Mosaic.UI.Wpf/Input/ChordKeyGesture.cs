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
        private bool _gotFirstGesture;
        private readonly InputGesture _firstGesture;
        private readonly Key _secondKey;
        private readonly ModifierKeys _secondModifier;
        private readonly bool _matchSecondModifier;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="firstKey"></param>
        /// <param name="secondKey"></param>
        public KeyChordGesture(ModifierKeys modifier, Key firstKey, Key secondKey) : base(Key.None)
        {
            _firstGesture = new KeyGesture(firstKey, modifier);
            _secondKey = secondKey;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="firstModifier">The modifier for the first key.</param>
        /// <param name="firstKey">The first key in the chord.</param>
        /// <param name="secondModifier">The modifier for the second key.</param>
        /// <param name="secondKey">The second key in the chord.</param>
        public KeyChordGesture(ModifierKeys firstModifier, Key firstKey, ModifierKeys secondModifier, Key secondKey) : this(firstModifier, firstKey, secondKey)
        {
            _secondModifier = secondModifier;
            _matchSecondModifier = true;
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
                bool matched = keyArgs.Key == _secondKey
                               && (!_matchSecondModifier || Keyboard.Modifiers == _secondModifier);

                if (matched)
                {
                    inputEventArgs.Handled = true;
                }

                return matched;
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
