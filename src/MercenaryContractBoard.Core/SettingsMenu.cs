using System;
using System.Collections.Generic;
using System.Linq;

namespace MercenaryContractBoard.Core
{
    /// <summary>How a setting shows in the Mod Configuration Menu.</summary>
    public enum MenuKind
    {
        /// <summary>A checkbox.</summary>
        Bool,

        /// <summary>A whole-number slider.</summary>
        Int,
    }

    /// <summary>One group (section) of the menu page, in page order.</summary>
    public sealed class MenuGroup
    {
        internal MenuGroup(string name, int order)
        {
            Name = name;
            Order = order;
        }

        public string Name { get; }
        public int Order { get; }
    }

    /// <summary>
    /// One setting of the menu page and how it maps onto <see cref="BoardSettings"/>: <see cref="Get"/> gives the
    /// value in menu units (0/1 for a bool), <see cref="Set"/> takes one, clamps it and writes the settings object.
    /// The module only wraps Get/Set in MCM's ProxyRefs (the sister mods' pattern).
    /// </summary>
    public sealed class MenuSetting
    {
        private readonly Func<BoardSettings, double> _get;
        private readonly Action<BoardSettings, double> _set;

        internal MenuSetting(string id, string label, MenuGroup group, string hint, MenuKind kind, double min, double max,
            Func<BoardSettings, double> get, Action<BoardSettings, double> set)
        {
            Id = id;
            Label = label;
            Group = group;
            Hint = hint;
            Kind = kind;
            Min = min;
            Max = max;
            _get = get;
            _set = set;
        }

        /// <summary>The MCM property id (= the <see cref="BoardSettings"/> property name).</summary>
        public string Id { get; }

        public string Label { get; }
        public MenuGroup Group { get; }
        public string Hint { get; }
        public MenuKind Kind { get; }
        public double Min { get; }
        public double Max { get; }

        public double Get(BoardSettings settings) => _get(settings);

        /// <summary>Writes a menu-unit value, clamped to the range (NaN = left as it is). True when it changed.</summary>
        public bool Set(BoardSettings settings, double value)
        {
            if (double.IsNaN(value)) return false;
            if (Kind == MenuKind.Bool) value = value != 0 ? 1 : 0;
            else value = Math.Round(Math.Max(Min, Math.Min(Max, value)), MidpointRounding.AwayFromZero);
            double before = _get(settings);
            _set(settings, value);
            return Math.Abs(_get(settings) - before) > 1e-6;
        }
    }

    /// <summary>
    /// Every setting as the in-game Mod Configuration Menu page shows it: groups, order, labels, hints and ranges.
    /// Game-free on purpose (unit-tested); the module builds the MCM page from this list.
    /// </summary>
    public static class SettingsMenu
    {
        public const string PageId = "MercenaryContractBoard_v1";
        public const string PageName = ModInfo.DisplayName;
        public const string FolderName = "MercenaryContractBoard";

        public static readonly MenuGroup General = new MenuGroup("General", 0);
        public static readonly MenuGroup Board = new MenuGroup("The board", 1);

        public static IReadOnlyList<MenuGroup> Groups { get; } = new[] { General, Board };

        public static IReadOnlyList<MenuSetting> All { get; } = new List<MenuSetting>
        {
            Flag(General, nameof(BoardSettings.Enabled), "Enabled",
                "Every town's menu has a \"Mercenary Contract Board\" option listing the mercenary contracts kingdoms would offer you, with vanilla's conditions and pay. Off = no menu option. Default on.",
                s => s.Enabled, (s, v) => s.Enabled = v),
            Flag(Board, nameof(BoardSettings.ShowUnavailable), "Show kingdoms that would not hire you",
                "Kingdoms that would not offer you a contract right now are listed greyed out, with the reason in their tooltip. Off = only contracts on offer. Default on.",
                s => s.ShowUnavailable, (s, v) => s.ShowUnavailable = v),
            Flag(Board, nameof(BoardSettings.SortByPay), "Best pay first",
                "Contracts on offer are sorted by pay, best first. Off = by kingdom name. Default on.",
                s => s.SortByPay, (s, v) => s.SortByPay = v),
            Flag(Board, nameof(BoardSettings.ConfirmFirstContract), "Confirm a first contract",
                "Ask before signing when you are not under contract yet. Switching from one contract to another always asks. Default on.",
                s => s.ConfirmFirstContract, (s, v) => s.ConfirmFirstContract = v),
        };

        /// <summary>The settings of one group, in menu order.</summary>
        public static IEnumerable<MenuSetting> InGroup(MenuGroup group) => All.Where(s => s.Group == group);

        /// <summary>The setting with this id, or null.</summary>
        public static MenuSetting? Find(string id) => All.FirstOrDefault(s => s.Id == id);

        private static MenuSetting Flag(MenuGroup g, string id, string label, string hint,
            Func<BoardSettings, bool> get, Action<BoardSettings, bool> set) =>
            new MenuSetting(id, label, g, hint, MenuKind.Bool, 0, 1, s => get(s) ? 1 : 0, (s, v) => set(s, v != 0));
    }
}
