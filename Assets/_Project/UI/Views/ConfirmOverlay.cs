using System;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// What a screen is asking the player to confirm.
    ///
    /// A parameter object rather than five arguments, so a screen raising a dialog reads
    /// as one statement and a later addition — an icon, a "don't ask again" — does not
    /// touch every call site.
    /// </summary>
    public readonly struct ConfirmRequest
    {
        public ConfirmRequest(string title, string body, string confirmLabel, Action onConfirm, bool destructive = false)
        {
            Title = title;
            Body = body;
            ConfirmLabel = confirmLabel;
            OnConfirm = onConfirm;
            Destructive = destructive;
        }

        public string Title { get; }

        /// <summary>
        /// What the player is agreeing to, in full. This is the only place some
        /// consequences are ever stated, so it says the cost rather than implying it.
        /// </summary>
        public string Body { get; }

        public string ConfirmLabel { get; }

        public Action OnConfirm { get; }

        /// <summary>Styles the confirm button as a loss rather than as a purchase.</summary>
        public bool Destructive { get; }
    }

    /// <summary>
    /// One question with two answers, raised over whatever screen asked it.
    ///
    /// It exists because Day 12 introduced the game's first action a player can regret.
    /// Everything before it either spent gold, which the treasury shows, or changed a
    /// number that could be changed back; retiring an adventurer ends a person. The
    /// dialog is not ceremony — it is where the consequences that no card has room for
    /// get said out loud, which is what turns the retire button from a trapdoor into a
    /// decision.
    ///
    /// It holds no opinion about what is being confirmed. The caller supplies the words
    /// and the action, which keeps this reusable for Week 3's "watch an ad?" and "reset
    /// progress?" without either of those rules landing in a view.
    /// </summary>
    public sealed class ConfirmOverlay : VisualElement
    {
        private readonly Label _title;
        private readonly Label _body;
        private readonly Button _confirm;

        private Action _onConfirm;

        public ConfirmOverlay()
        {
            AddToClassList("overlay");
            AddToClassList("overlay--hidden");

            VisualElement panel = Ui.Box("overlay__panel");
            _title = Ui.Text(string.Empty, "overlay__title");
            _body = Ui.Text(string.Empty, "card__subtitle");

            VisualElement actions = Ui.Box("overlay__actions");
            _confirm = Ui.Action(string.Empty, OnConfirmClicked, "button--wide");
            Button cancel = Ui.Action("Cancel", Close, "button--spaced");
            actions.Add(_confirm);
            actions.Add(cancel);

            panel.Add(_title);
            panel.Add(_body);
            panel.Add(actions);
            Add(panel);

            // Same arrangement as the building overlay: the scrim dismisses, the panel
            // swallows its own taps so they do not bubble out and close it. Dismissing
            // is always the safe answer here, which is why the scrim is not disabled.
            RegisterCallback<ClickEvent>(_ => Close());
            panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        }

        public bool IsOpen => !ClassListContains("overlay--hidden");

        public void Ask(ConfirmRequest request)
        {
            _title.text = request.Title;
            _body.text = request.Body;
            _confirm.text = request.ConfirmLabel;
            _confirm.EnableInClassList("button--destructive", request.Destructive);
            _confirm.EnableInClassList("button--primary", !request.Destructive);
            _onConfirm = request.OnConfirm;

            RemoveFromClassList("overlay--hidden");
        }

        public void Close()
        {
            AddToClassList("overlay--hidden");
            _onConfirm = null;
        }

        /// <summary>
        /// Close first, then act. The action can raise a toast of its own, and a dialog
        /// still standing in front of it would hide the answer to the question it just
        /// asked.
        /// </summary>
        private void OnConfirmClicked()
        {
            Action action = _onConfirm;
            Close();
            action?.Invoke();
        }
    }
}
