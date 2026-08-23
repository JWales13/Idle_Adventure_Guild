using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// One line telling the player what just happened, and why when it did not.
    ///
    /// Small, but it is the piece that makes a disabled button honest. Every service in
    /// the game returns an outcome enum naming the gate that stopped it, and without
    /// somewhere to put that sentence the player is left tapping something inert and
    /// guessing. It hides itself again after a few seconds so it never becomes furniture.
    /// </summary>
    public sealed class ToastBar : VisualElement
    {
        /// <summary>How long a message stays up. Long enough to read twice, short enough not to linger.</summary>
        private const long VisibleMilliseconds = 3200L;

        private readonly Label _message;

        private IVisualElementScheduledItem _hideTimer;

        public ToastBar()
        {
            AddToClassList("toast");
            AddToClassList("toast--hidden");

            _message = new Label();
            _message.style.whiteSpace = WhiteSpace.Normal;
            Add(_message);
        }

        public void Show(string message, bool succeeded)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _message.text = message;
            EnableInClassList("toast--positive", succeeded);
            EnableInClassList("toast--negative", !succeeded);
            RemoveFromClassList("toast--hidden");

            // Restarted rather than stacked: a player tapping a locked button four times
            // should see one message for a few seconds, not four queued behind each other.
            _hideTimer?.Pause();
            _hideTimer = schedule.Execute(Hide).StartingIn(VisibleMilliseconds);
        }

        public void Hide()
        {
            AddToClassList("toast--hidden");
        }
    }
}
