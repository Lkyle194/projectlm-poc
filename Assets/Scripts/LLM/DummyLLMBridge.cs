using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using ProjectLM.Core;

namespace ProjectLM.LLM
{
    /// <summary>
    /// Dummy LLM bridge for PC testing without a real model.
    /// Returns deterministic unlocks based on dominant trait.
    /// </summary>
    public class DummyLLMBridge : ILLMBridge
    {
        public bool IsModelReady => true;

        public Task<bool> LoadModel(string modelPath, IProgress<float> progress = null)
        {
            Debug.Log($"[DummyLLM] LoadModel called (ignored): {modelPath}");
            return Task.FromResult(true);
        }

        public Task<LlmUnlock> GenerateUnlock(BehaviorProfile profile, string behaviorSummary,
            int waveNumber, string[] availableUpgrades)
        {
            string dominant = profile.DominantTrait();
            var unlock = dominant switch
            {
                "aggressif" => new LlmUnlock
                {
                    behaviorLabel = "Berserker",
                    description = "Tu fonces dans le tas sans réfléchir !",
                    unlockType = "upgrade",
                    unlockName = "Rage Dévastatrice",
                    unlockEffect = "click_damage",
                    unlockValue = 2.0f,
                    unlockDescription = "Tes clics infligent 2x plus de dégâts, mais tu perds 1% de vie par clic.",
                    newPath = "chemin_de_la_guerre"
                },
                "prudent" => new LlmUnlock
                {
                    behaviorLabel = "Gardien",
                    description = "Prudent mais efficace, tu protèges tes arrières.",
                    unlockType = "upgrade",
                    unlockName = "Barrière Protectrice",
                    unlockEffect = "defense",
                    unlockValue = 0.5f,
                    unlockDescription = "50% des dégâts ennemis sont absorbés. Effet doublé quand tu viens de cliquer sur Défense.",
                    newPath = "chemin_de_la_sagesse"
                },
                "curieux" => new LlmUnlock
                {
                    behaviorLabel = "Explorateur",
                    description = "La connaissance est ton arme la plus puissante.",
                    unlockType = "path",
                    unlockName = "Œil de l'Explorateur",
                    unlockEffect = "special",
                    unlockValue = 1.5f,
                    unlockDescription = "Les clics sur Explorer révèlent des bonus cachés 2x plus souvent. Nouveaux chemins débloqués !",
                    newPath = "chemin_de_la_decouverte"
                },
                "avare" => new LlmUnlock
                {
                    behaviorLabel = "Collectionneur",
                    description = "Chaque ressource compte. Tu ne laisses rien passer.",
                    unlockType = "upgrade",
                    unlockName = "Multiplication des Ressources",
                    unlockEffect = "resource_mult",
                    unlockValue = 3.0f,
                    unlockDescription = "Les ressources minées sont triplées. Les clics sur Mine ont 20% de chance de doubler.",
                    newPath = "chemin_de_l_abondance"
                },
                "téméraire" => new LlmUnlock
                {
                    behaviorLabel = "Téméraire",
                    description = "Le danger est ton terrain de jeu !",
                    unlockType = "synergy",
                    unlockName = "Pacte Sanglant",
                    unlockEffect = "click_damage",
                    unlockValue = 5.0f,
                    unlockDescription = "Quand ta vie est sous 30%, tes clics infligent 5x plus de dégâts mais tu perds 2% de vie par clic.",
                    newPath = "chemin_du_danger"
                },
                _ => new LlmUnlock
                {
                    behaviorLabel = "Équilibré",
                    description = "La polyvalence est ta force. Tu t'adaptes à tout.",
                    unlockType = "upgrade",
                    unlockName = "Harmonie des Éléments",
                    unlockEffect = "special",
                    unlockValue = 1.2f,
                    unlockDescription = "Tous les types de clics sont 20% plus efficaces. Bonus de synergie entre les nœuds.",
                    newPath = "chemin_de_l_harmonie"
                }
            };

            return Task.FromResult(unlock);
        }

        public Task<string> GenerateNarration(string behaviorLabel, string unlockName)
        {
            string text = $"⚡ « {behaviorLabel} ! » ⚡\n" +
                          $"L'IA observe ton style et débloque : {unlockName} !\n" +
                          $"Le chemin évolue...";
            return Task.FromResult(text);
        }

        public void UnloadModel()
        {
            Debug.Log("[DummyLLM] UnloadModel called (ignored)");
        }
    }
}
