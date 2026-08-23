using System;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// The bottom navigation. Three destinations, always visible, always in the same
    /// order — the one part of the interface that must never move, because it is how a
    /// player builds a mental map of the game in the first thirty seconds.
    /// </summary>
    public sealed class TabBar : VisualElement
    {
        private readonly Button _hall;
        private readonly Button _quests;
        private readonly Button _roster;

        public TabBar(Action<GuildScreen> onSelect)
        {
            AddToClassList("tab-bar");

            _hall = Tab("Guild Hall", GuildScreen.Hall, onSelect);
            _quests = Tab("Quests", GuildScreen.Quests, onSelect);
            _roster = Tab("Roster", GuildScreen.Roster, onSelect);

            Add(_hall);
            Add(_quests);
            Add(_roster);
        }

        public void SetActive(GuildScreen screen)
        {
            _hall.EnableInClassList("tab--active", screen == GuildScreen.Hall);
            _quests.EnableInClassList("tab--active", screen == GuildScreen.Quests);
            _roster.EnableInClassList("tab--active", screen == GuildScreen.Roster);
        }

        private static Button Tab(string label, GuildScreen screen, Action<GuildScreen> onSelect)
        {
            Button button = new Button(() => onSelect?.Invoke(screen)) { text = label };
            button.AddToClassList("tab");
            return button;
        }
    }
}
