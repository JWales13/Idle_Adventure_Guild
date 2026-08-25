using IdleGuild.App;
using IdleGuild.Core;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// The permanent header: which guild this is, what it is worth, and the one action
    /// that is always available.
    ///
    /// Balances are read every tick rather than driven by <c>CurrencyChanged</c>, which
    /// is exactly what that event's own documentation asks for — idle income accrues
    /// continuously, so a display bound to the event would either flood the bus or sit
    /// still between quests. The event remains useful as a correction signal; this bar
    /// simply does not need it.
    ///
    /// <b>The mailbox lives here rather than on a screen the player might not be on.</b>
    /// It is the guarantee behind §01's rule that no sequence of choices may leave the
    /// player unable to make progress, and a guarantee the player cannot find is not one.
    /// It sits beside the treasury for the same reason the debug console puts it there:
    /// that is where somebody looks when the treasury is the problem.
    /// </summary>
    public sealed class TreasuryBar : VisualElement
    {
        private readonly Label _tierName;
        private readonly Label _tierNote;
        private readonly Label _gold;
        private readonly Label _reputation;
        private readonly Button _stipend;

        private GuildContext _context;

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

            // Always present, never hidden, disabled while empty — the Day 15 icon rule
            // applied to an action instead of an image. Collapsing it while there is
            // nothing to collect is tidier and is the wrong trade, because a mailbox that
            // is empty and a mailbox that was never built look identical on screen. That
            // is precisely how this shipped invisible the first time.
            _stipend = Ui.Action(string.Empty, CollectStipend, "stipend");
            balances.Add(_stipend);

            Add(identity);
            Add(balances);
        }

        public void Refresh(GuildContext context)
        {
            _context = context;

            _tierName.text = context.World.GuildState.CurrentTier.DisplayName;
            _tierNote.text = $"{context.World.Roster.Count}/{context.World.Roster.CapacityWith(context.Stats)} beds  ·  " +
                             $"{context.World.QuestLog.ActiveCount}/{context.World.QuestLog.SlotsWith(context.Stats)} quests";

            _gold.text = Format.Amount(context.World.Economy.Get(CurrencyType.Gold));
            _reputation.text = Format.Amount(context.World.Economy.Get(CurrencyType.Reputation));

            RefreshStipend(context);
        }

        /// <summary>
        /// The mailbox reads one of three ways, and each of them is a sentence the player
        /// can act on rather than a state they have to infer.
        /// </summary>
        private void RefreshStipend(GuildContext context)
        {
            StipendService stipend = context.Stipend;

            if (stipend.GoldPerDelivery <= 0d)
            {
                // An unauthored tier. Said out loud rather than rendered as a dead
                // button, because the two look the same and only one of them is a bug.
                _stipend.text = "Stipend —";
                _stipend.SetEnabled(false);
                _stipend.EnableInClassList("stipend--ready", false);
                return;
            }

            int waiting = stipend.DeliveriesWaiting;
            bool ready = stipend.CanCollect;

            _stipend.text = ready
                ? $"Stipend  {waiting}"
                : $"Stipend  {Format.Duration(stipend.SecondsUntilNextDelivery)}";

            _stipend.SetEnabled(ready);
            _stipend.EnableInClassList("stipend--ready", ready);
        }

        private void CollectStipend()
        {
            if (_context == null)
            {
                return;
            }

            // Views read state and call services; they never compute one. Whether there
            // is anything to collect, and what it is worth, are both the service's answer.
            if (_context.Stipend.TryCollect(out double gold))
            {
                _context.Report($"The crown's courier hands over {Format.Amount(gold)} gold.", true);
            }
            else
            {
                _context.Report("Nothing has arrived from the crown yet.", false);
            }

            RefreshStipend(_context);
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
