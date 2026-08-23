using System;
using UnityEngine;

namespace IdleGuild.Core.Services
{
    /// <summary>
    /// Stand-in used until Unity IAP or an equivalent is integrated (roadmap Day 19).
    /// Owns nothing and declines every purchase, so no entitlement can be granted by
    /// accident in editor builds.
    /// </summary>
    public sealed class NullPurchaseService : IPurchaseService
    {
        public bool IsInitialized => false;

        public void Purchase(string productId, Action<PurchaseResult> onFinished)
        {
            Debug.Log($"[NullPurchaseService] Purchase '{productId}' requested; no store wired up.");
            onFinished?.Invoke(PurchaseResult.NotAvailable);
        }

        public void RestorePurchases(Action<bool> onFinished)
        {
            Debug.Log("[NullPurchaseService] Restore requested; no store wired up.");
            onFinished?.Invoke(false);
        }

        public bool IsOwned(string productId) => false;
    }
}
