using System;
using IdleGuild.App;
using IdleGuild.Core;

namespace IdleGuild.UI
{
    /// <summary>
    /// Everything a screen is allowed to touch: the world to read, the services to call,
    /// and a way to tell the player what happened.
    ///
    /// This exists because the UI assembly sits above App and can therefore see the
    /// whole game — a deliberate choice, but one that needs a visible boundary to stop
    /// screens quietly growing rules of their own. A view takes a context and nothing
    /// else. It never receives <c>GameBootstrap</c>, so it cannot reach Unity's
    /// lifecycle, start a coroutine, or save the game behind the controller's back.
    ///
    /// The rule the context is meant to make obvious: <b>views read state and call
    /// services; they never compute one.</b> A cost, a gate, a failure chance or an
    /// unlock belongs to a definition asset or a service, and a screen that works one out
    /// for itself has put a rule somewhere the tests and the balance pass will never
    /// look.
    /// </summary>
    public sealed class GuildContext
    {
        private readonly Action<string, bool> _report;

        public GuildContext(
            GameWorld world,
            BuildingUpgradeService buildings,
            RecruitmentService recruitment,
            TrainingService training,
            QuestDispatchService dispatch,
            TierAdvancementService tiers,
            Action<string, bool> report)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            Recruitment = recruitment ?? throw new ArgumentNullException(nameof(recruitment));
            Training = training ?? throw new ArgumentNullException(nameof(training));
            Dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            Tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
            _report = report;
        }

        public GameWorld World { get; }

        public BuildingUpgradeService Buildings { get; }

        public RecruitmentService Recruitment { get; }

        public TrainingService Training { get; }

        public QuestDispatchService Dispatch { get; }

        public TierAdvancementService Tiers { get; }

        /// <summary>The aggregated building effects, as everything outside Guild sees them.</summary>
        public IGuildStats Stats => World.Stats;

        /// <summary>
        /// Say something to the player — the result of what they just did, whether it
        /// worked or not. Routed through the context rather than reached for directly so
        /// a view never holds a reference to the chrome around it.
        /// </summary>
        public void Report(string message, bool succeeded)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _report?.Invoke(message, succeeded);
            }
        }
    }
}
