using System.Collections.Generic;
using UnityEngine;
using ProjectLM.LLM;

namespace ProjectLM.Game
{
    /// <summary>
    /// Receives LLM-generated unlocks and applies them to the game state.
    /// </summary>
    public class SkillUnlocker : MonoBehaviour
    {
        private GameManager _gameManager;
        private List<ClickNode> _allNodes = new List<ClickNode>();
        private float _defenseMultiplier = 1f;
        private float _globalClickMultiplier = 1f;

        public float GetDefenseMultiplier() => _defenseMultiplier;

        private void Start()
        {
            _gameManager = FindAnyObjectByType<GameManager>();
        }

        public void RegisterNode(ClickNode node)
        {
            if (!_allNodes.Contains(node))
                _allNodes.Add(node);
        }

        /// <summary>
        /// Apply an LLM-generated unlock to the game.
        /// </summary>
        public void ApplyUnlock(LlmUnlock unlock)
        {
            Debug.Log($"[SkillUnlocker] Applying: {unlock.unlockName} ({unlock.unlockType})");

            switch (unlock.unlockType)
            {
                case "upgrade":
                    ApplyUpgrade(unlock);
                    break;
                case "path":
                    ApplyPath(unlock);
                    break;
                case "synergy":
                    ApplySynergy(unlock);
                    break;
                case "evolution":
                    ApplyEvolution(unlock);
                    break;
                default:
                    ApplyUpgrade(unlock);
                    break;
            }
        }

        private void ApplyUpgrade(LlmUnlock unlock)
        {
            // Apply to all click nodes
            foreach (var node in _allNodes)
            {
                node.ApplyUpgrade(unlock.unlockEffect, unlock.unlockValue);
            }

            // Handle special effects
            switch (unlock.unlockEffect)
            {
                case "defense":
                    _defenseMultiplier = unlock.unlockValue;
                    break;
                case "auto_clicker":
                    // TODO: auto-clicker coroutine
                    break;
            }
        }

        private void ApplyPath(LlmUnlock unlock)
        {
            // Path unlocks are handled by NodeSpawner (new clickable path)
            // Also apply a minor global bonus
            _globalClickMultiplier *= 1.1f;
        }

        private void ApplySynergy(LlmUnlock unlock)
        {
            // Synergies combine effects from multiple sources
            ApplyUpgrade(unlock);
        }

        private void ApplyEvolution(LlmUnlock unlock)
        {
            // Evolution = replace existing upgrade with a better version
            ApplyUpgrade(unlock);
        }
    }
}
