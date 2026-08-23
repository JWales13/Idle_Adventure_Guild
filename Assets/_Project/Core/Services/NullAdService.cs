using System;
using UnityEngine;

namespace IdleGuild.Core.Services
{
    /// <summary>
    /// Stand-in used until a real ad SDK is integrated (roadmap Day 18). Reports no
    /// inventory and fails every request, so the surrounding flow — offer shown,
    /// reward withheld, UI recovered — is exercised from Day 1 rather than first
    /// meeting its failure path in Week 3.
    /// </summary>
    public sealed class NullAdService : IAdService
    {
        public bool IsRewardedAdReady => false;

        public void ShowRewardedAd(string placementId, Action<RewardedAdResult> onFinished)
        {
            Debug.Log($"[NullAdService] Rewarded ad '{placementId}' requested; no ad service wired up.");
            onFinished?.Invoke(RewardedAdResult.NotAvailable);
        }

        public void ShowInterstitial(string placementId)
        {
            Debug.Log($"[NullAdService] Interstitial '{placementId}' requested; no ad service wired up.");
        }
    }
}
