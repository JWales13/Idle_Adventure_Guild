using System;

namespace IdleGuild.Core.Services
{
    /// <summary>Outcome of a rewarded ad presentation.</summary>
    public enum RewardedAdResult
    {
        /// <summary>Watched to completion — the caller must grant the reward.</summary>
        Completed,
        /// <summary>Dismissed early — no reward is owed.</summary>
        Skipped,
        /// <summary>The network failed to present or errored mid-play.</summary>
        Failed,
        /// <summary>No fill, or no ad service is wired up on this build.</summary>
        NotAvailable
    }

    /// <summary>
    /// Advertising boundary. Gameplay depends on this interface only, never on a
    /// concrete ad SDK, so swapping networks is an adapter change rather than a
    /// rewrite of every call site.
    /// </summary>
    public interface IAdService
    {
        /// <summary>True when a rewarded ad can be shown right now.</summary>
        bool IsRewardedAdReady { get; }

        /// <summary>
        /// Present a rewarded ad. <paramref name="onFinished"/> is always invoked
        /// exactly once, including on failure, so callers never leak a pending reward.
        /// </summary>
        void ShowRewardedAd(string placementId, Action<RewardedAdResult> onFinished);

        /// <summary>Present an interstitial. Fire-and-forget; never gates a reward.</summary>
        void ShowInterstitial(string placementId);
    }
}
