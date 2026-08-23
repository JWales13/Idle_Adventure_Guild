using System;
using UnityEngine;

namespace IdleGuild.Guild
{
    /// <summary>A single "building X must be at least level N" clause of a tier gate.</summary>
    [Serializable]
    public struct BuildingLevelRequirement
    {
        [SerializeField] private BuildingDefinition _building;
        [SerializeField, Min(1)] private int _minimumLevel;

        public BuildingDefinition Building => _building;
        public int MinimumLevel => _minimumLevel;
    }
}
