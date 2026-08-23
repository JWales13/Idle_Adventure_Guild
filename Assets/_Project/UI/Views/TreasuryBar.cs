using IdleGuild.Core;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// The permanent header: which guild this is, and what it is worth.
    ///
    /// Balances are read every tick rather than driven by <c>CurrencyChanged</c>, which
    /// is exactly what that event's own documentation asks for — idle income accrues
    /// continuously, so a display bound to the event would either flood the bus or sit
    /// still between quests. The event remains useful as a correction signal; this bar
    /// simply does not need it.
    /// </summary>
    public sealed class TreasuryBar : VisualElement
    {
        private readonly Label _tierName;
        private readonly Label _tierNote;
        private readonly Label _gold;
        private readonly Label _reputation;

        public TreasuryBar()
        {
            AddToClassList("treasury");

            VisualElement identity = Ui.Box();
            _tierName = Ui.Text(string.Empty, "treasury__tier");
            _tierNote = Ui.Text(string.Empty, "treasury__tier-note");
            identity.Add(_tierName);
            identity.Add(_tierNote);

            VisualElement balances = Ui.Box("treasury__balances");
            balances.Add(Currency("Gold", "currency--gold", out _gold));
            balances.Add(Currency("Rep", "currency--reputation", out _reputation));

            Add(identity);
            Add(balances);
        }

        public void Refresh(GuildContext context)
        {
            _tierName.text = context.World.GuildState.CurrentTier.DisplayName;
            _tierNote.text = $"{context.World.Roster.Count}/{context.World.Roster.CapacityWith(context.Stats)} beds  ·  " +
                             $"{context.World.QuestLog.ActiveCount}/{context.World.QuestLog.SlotsWith(context.Stats)} quests";

            _gold.text = Format.Amount(context.World.Economy.Get(CurrencyType.Gold));
            _reputation.text = Format.Amount(context.World.Economy.Get(CurrencyType.Reputation));
        }

        private static VisualElement Currency(string label, string modifier, out Label value)
        {
            VisualElement group = Ui.Box("currency", modifier);
            group.Add(Ui.Text(label, "currency__label"));
            value = Ui.Text("0", "currency__value");
            group.Add(value);
            return group;
        }
    }
}
