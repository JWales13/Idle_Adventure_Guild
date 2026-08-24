using System;
#if UNITY_EDITOR
using System.Collections.Generic;
#endif

namespace IdleGuild.Core
{
    /// <summary>
    /// Runs a definition asset's self-check one editor tick after <c>OnValidate</c>
    /// rather than during it.
    ///
    /// <c>OnValidate</c> fires while Unity is still deserialising the object — on an
    /// import-worker pass and on every domain reload — and at that moment every
    /// serialised field reads as its type default. A fully populated asset therefore
    /// reports an empty Id, an empty effects array, and a tier gate with one building
    /// in it. Day 4–5 caught half of this and fixed <c>GameContent</c> by counting
    /// <c>Tiers.Length</c> rather than dereferencing <c>StartingTier</c> — but the
    /// length reads zero as well, so the warning came back. The distinction was never
    /// *what* the check looks at; it is *when* the check runs.
    ///
    /// The cost of getting this wrong is not the noise itself. It is that a warning
    /// which cries wolf on every reload teaches you to scroll past the console, which
    /// is exactly where a real one will be sitting the day a balance pass breaks
    /// something.
    ///
    /// Clamps stay in <c>OnValidate</c>: clamping a field that currently reads zero to
    /// zero is harmless, and a clamp has to apply the moment a value is typed.
    /// </summary>
    public static class AssetValidation
    {
#if UNITY_EDITOR
        // At most one queued check per asset. OnValidate fires several times during a
        // single import, and without this each firing logged the same warning again —
        // which is most of why the console filled up rather than merely being wrong.
        //
        // Keyed by the object rather than by an instance id on purpose: GetInstanceID
        // is deprecated in Unity 6 and its [Obsolete] is marked as an error, not a
        // warning, so calling it does not compile. UnityEngine.Object's own equality
        // and hash code are what a set needs anyway.
        private static readonly HashSet<UnityEngine.Object> Queued = new HashSet<UnityEngine.Object>();
#endif

        /// <summary>
        /// Queue <paramref name="check"/> to run once <paramref name="asset"/> is fully
        /// loaded. Compiled out entirely outside the editor, so neither the call nor the
        /// delegate it allocates reaches a player build.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void WhenLoaded(UnityEngine.Object asset, Action check)
        {
#if UNITY_EDITOR
            if (asset == null || check == null)
            {
                return;
            }

            if (!Queued.Add(asset))
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall += () =>
            {
                Queued.Remove(asset);

                // The asset can be deleted, or the domain reloaded, between the queue
                // and the tick. A destroyed UnityEngine.Object compares equal to null
                // rather than throwing, which is what makes this the right check.
                if (asset == null)
                {
                    return;
                }

                check();
            };
#endif
        }
    }
}
