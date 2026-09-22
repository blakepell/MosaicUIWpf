/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Covers the arrow-key semantics of <see cref="CommandBoxHistory"/>. No STA thread is needed
    /// here, the type is deliberately free of UI dependencies.
    /// </summary>
    public class CommandBoxHistoryTests
    {
        [Fact]
        public void Add_Ignores_Null_Empty_And_Whitespace()
        {
            var history = new CommandBoxHistory();

            history.Add(null);
            history.Add(string.Empty);
            history.Add("   ");

            Assert.Empty(history.Items);
        }

        [Fact]
        public void Add_Trims_The_Command()
        {
            var history = new CommandBoxHistory();

            history.Add("  look north  ");

            Assert.Equal(["look north"], history.Items);
        }

        [Fact]
        public void SkipConsecutive_Collapses_Only_Repeats_In_A_Row()
        {
            var history = new CommandBoxHistory { DuplicatePolicy = HistoryDuplicatePolicy.SkipConsecutive };

            history.Add("north");
            history.Add("north");
            history.Add("east");
            history.Add("north");

            Assert.Equal(["north", "east", "north"], history.Items);
        }

        [Fact]
        public void SkipAll_Never_Records_A_Command_Twice()
        {
            var history = new CommandBoxHistory { DuplicatePolicy = HistoryDuplicatePolicy.SkipAll };

            history.Add("north");
            history.Add("east");
            history.Add("north");

            Assert.Equal(["north", "east"], history.Items);
        }

        [Fact]
        public void Allow_Records_Every_Command()
        {
            var history = new CommandBoxHistory { DuplicatePolicy = HistoryDuplicatePolicy.Allow };

            history.Add("north");
            history.Add("north");

            Assert.Equal(["north", "north"], history.Items);
        }

        [Fact]
        public void MaxItems_Evicts_The_Oldest_Entries()
        {
            var history = new CommandBoxHistory { MaxItems = 2, DuplicatePolicy = HistoryDuplicatePolicy.Allow };

            history.Add("one");
            history.Add("two");
            history.Add("three");

            Assert.Equal(["two", "three"], history.Items);
        }

        [Fact]
        public void Lowering_MaxItems_Trims_Immediately()
        {
            var history = new CommandBoxHistory { DuplicatePolicy = HistoryDuplicatePolicy.Allow };

            history.Add("one");
            history.Add("two");
            history.Add("three");

            history.MaxItems = 1;

            Assert.Equal(["three"], history.Items);
        }

        [Fact]
        public void MaxItems_Of_Zero_Is_Unlimited()
        {
            var history = new CommandBoxHistory { MaxItems = 0, DuplicatePolicy = HistoryDuplicatePolicy.Allow };

            for (int i = 0; i < 500; i++)
            {
                history.Add($"command {i}");
            }

            Assert.Equal(500, history.Count);
        }

        [Fact]
        public void MoveBack_Walks_Toward_The_Oldest_And_Clamps()
        {
            var history = new CommandBoxHistory();

            history.Add("one");
            history.Add("two");

            Assert.Equal("two", history.MoveBack());
            Assert.Equal("one", history.MoveBack());
            Assert.Null(history.MoveBack());
        }

        [Fact]
        public void MoveBack_On_An_Empty_History_Returns_Null()
        {
            var history = new CommandBoxHistory();

            Assert.Null(history.MoveBack());
        }

        [Fact]
        public void MoveForward_Restores_The_Draft_Then_Returns_Null()
        {
            var history = new CommandBoxHistory();

            history.Add("one");
            history.Add("two");
            history.ResetCursor("half typed");

            Assert.Equal("two", history.MoveBack());
            Assert.Equal("half typed", history.MoveForward());
            Assert.Null(history.MoveForward());
        }

        [Fact]
        public void MoveForward_Without_A_Draft_Returns_An_Empty_String()
        {
            var history = new CommandBoxHistory();

            history.Add("one");

            Assert.Equal("one", history.MoveBack());
            Assert.Equal(string.Empty, history.MoveForward());
        }

        [Fact]
        public void IsNavigating_Tracks_The_Cursor()
        {
            var history = new CommandBoxHistory();

            history.Add("one");
            Assert.False(history.IsNavigating);

            history.MoveBack();
            Assert.True(history.IsNavigating);

            history.MoveForward();
            Assert.False(history.IsNavigating);
        }

        [Fact]
        public void Add_Resets_The_Cursor_To_Past_The_End()
        {
            var history = new CommandBoxHistory();

            history.Add("one");
            history.Add("two");
            history.MoveBack();
            history.MoveBack();

            history.Add("three");

            Assert.False(history.IsNavigating);
            Assert.Equal("three", history.MoveBack());
        }

        [Fact]
        public void Add_Resets_The_Cursor_Even_When_The_Command_Is_Skipped()
        {
            var history = new CommandBoxHistory();

            history.Add("one");
            history.Add("two");
            history.MoveBack();

            // A consecutive duplicate is not recorded, but the cursor still returns to the end.
            history.Add("two");

            Assert.Equal(["one", "two"], history.Items);
            Assert.False(history.IsNavigating);
        }

        [Fact]
        public void Import_Appends_In_Order()
        {
            var history = new CommandBoxHistory();

            history.Import(["one", "two", "three"]);

            Assert.Equal(["one", "two", "three"], history.Items);
            Assert.False(history.IsNavigating);
        }

        [Fact]
        public void Import_Of_Null_Is_A_No_Op()
        {
            var history = new CommandBoxHistory();

            history.Import(null);

            Assert.Empty(history.Items);
        }

        [Fact]
        public void Clear_Empties_The_List_And_The_Draft()
        {
            var history = new CommandBoxHistory();

            history.Add("one");
            history.ResetCursor("draft");
            history.Clear();

            Assert.Empty(history.Items);
            Assert.Null(history.MoveBack());
            Assert.Null(history.MoveForward());
        }
    }
}
