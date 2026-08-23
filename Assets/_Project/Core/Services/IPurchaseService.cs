using System;

namespace IdleGuild.Core.Services
{
    /// <summary>Outcome of a single purchase attempt.</summary>
    public enum PurchaseResult
    {
        /// <summary>Payment succeeded — the caller must grant the entitlement.</summary>
        Purchased,
        /// <summary>The player backed out. Not an error.</summary>
        Cancelled,
        /// <summary>Payment or validation failed.</summary>
        Failed,
        /// <summary>Non-consumable the player already owns.</summary>
        AlreadyOwned,
        /// <summary>Store unreachable, or no purchase service is wired up on this build.</summary>
        NotAvailable
    }

    /// <summary>
    /// In-app purchase boundary. Mirrors <see cref="IAdService"/>: gameplay expresses
    /// intent in product IDs and reacts to results, with no knowledge of the store SDK.
    /// </summary>
    public interface IPurchaseService
    {
        /// <summary>True once the store catalogue has loaded and purchases can be attempted.</summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Attempt to buy <paramref name="productId"/>. <paramref name="onFinished"/> is
        /// always invoked exactly once.
        /// </summary>
        void Purchase(string productId, Action<PurchaseResult> onFinished);

        /// <summary>
        /// Restore non-consumables. Required by App Review for any app selling them.
        /// <paramref name="onFinished"/> receives true when the restore completed.
        /// </summary>
        void RestorePurchases(Action<bool> onFinished);

        /// <summary>True if the player owns this non-consumable.</summary>
        bool IsOwned(string productId);
    }
}
