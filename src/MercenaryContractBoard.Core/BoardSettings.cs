namespace MercenaryContractBoard.Core
{
    /// <summary>
    /// Every setting of the mod. Plain properties on purpose: <see cref="SettingsMenu"/> binds them to MCM one by one
    /// (a new setting = a property here + a line in <see cref="SettingsMenu.All"/>). Nothing here is saved into the
    /// campaign; MCM keeps its own json.
    /// </summary>
    public sealed class BoardSettings
    {
        /// <summary>Master switch: off = no menu option in towns.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Kingdoms that would not hire you right now are listed greyed out, with the reason in the tooltip.
        /// Off = only the contracts on offer are listed.</summary>
        public bool ShowUnavailable { get; set; } = true;

        /// <summary>Contracts on offer are sorted by pay, best first. Off = by kingdom name.</summary>
        public bool SortByPay { get; set; } = true;

        /// <summary>Ask "are you sure" before signing a first contract. Switching contracts always asks.</summary>
        public bool ConfirmFirstContract { get; set; } = true;
    }
}
