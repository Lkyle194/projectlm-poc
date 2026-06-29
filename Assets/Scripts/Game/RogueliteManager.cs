using System.Collections.Generic;
using UnityEngine;
using ProjectLM.Core;

namespace ProjectLM.Game
{
    /// <summary>
    /// Roguelite progression manager.
    /// Between runs, the player can choose permanent upgrades
    /// based on their performance and dominant trait.
    /// </summary>
    public class RogueliteManager : MonoBehaviour
    {
        [System.Serializable]
        public struct PermanentUpgrade
        {
            public string id;
            public string name;
            public string description;
            public string effect; // "starting_resources", "max_health", "click_mult", etc.
            public float value;
            public int cost; // waves cleared required
        }

        [Header("Available Permanent Upgrades")]
        [SerializeField] private PermanentUpgrade[] allUpgrades;

        // Saved data
        private HashSet<string> _purchasedUpgrades = new HashSet<string>();
        private int _totalWavesCleared;
        private int _bestWave;
        private int _essence; // Currency for permanent upgrades

        public int Essence => _essence;
        public int TotalWavesCleared => _totalWavesCleared;
        public int BestWave => _bestWave;
        public float BonusStartingResources
        {
            get
            {
                float bonus = 0;
                foreach (var id in _purchasedUpgrades)
                {
                    foreach (var ug in allUpgrades)
                    {
                        if (ug.id == id && ug.effect == "starting_resources")
                            bonus += ug.value;
                    }
                }
                return bonus;
            }
        }

        public void LoadProgress()
        {
            _purchasedUpgrades = new HashSet<string>(
                PlayerPrefs.GetString("Roguelite_Upgrades", "").Split(',')
            );
            _purchasedUpgrades.Remove("");
            _totalWavesCleared = PlayerPrefs.GetInt("Roguelite_Waves", 0);
            _bestWave = PlayerPrefs.GetInt("Roguelite_BestWave", 0);
            _essence = PlayerPrefs.GetInt("Roguelite_Essence", 0);
        }

        public void SaveProgress()
        {
            PlayerPrefs.SetString("Roguelite_Upgrades", string.Join(",", _purchasedUpgrades));
            PlayerPrefs.SetInt("Roguelite_Waves", _totalWavesCleared);
            PlayerPrefs.SetInt("Roguelite_BestWave", _bestWave);
            PlayerPrefs.SetInt("Roguelite_Essence", _essence);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Called when a run ends (victory or death).
        /// </summary>
        public void OnRunEnded(int wavesCleared, Core.BehaviorProfile profile)
        {
            _totalWavesCleared += wavesCleared;
            if (wavesCleared > _bestWave) _bestWave = wavesCleared;

            // Earn essence based on performance + behavioral diversity
            float diversityBonus = 1f + (profile.Aggression + profile.Caution +
                profile.Curiosity + profile.Greed) * 0.5f;
            int earned = Mathf.RoundToInt(wavesCleared * 10 * diversityBonus);
            _essence += earned;

            SaveProgress();
            Debug.Log($"[Roguelite] Run ended: wave {wavesCleared}, earned {earned} essence (total: {_essence})");
        }

        /// <summary>
        /// Get available upgrades that the player can afford.
        /// </summary>
        public PermanentUpgrade[] GetAvailableUpgrades()
        {
            var available = new List<PermanentUpgrade>();
            foreach (var ug in allUpgrades)
            {
                if (!_purchasedUpgrades.Contains(ug.id) && _essence >= ug.cost)
                    available.Add(ug);
            }
            return available.ToArray();
        }

        public bool PurchaseUpgrade(string id)
        {
            foreach (var ug in allUpgrades)
            {
                if (ug.id == id && !_purchasedUpgrades.Contains(id) && _essence >= ug.cost)
                {
                    _purchasedUpgrades.Add(id);
                    _essence -= ug.cost;
                    SaveProgress();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Apply permanent upgrades when starting a new run.
        /// </summary>
        public void ApplyPermanentUpgrades(GameManager gm)
        {
            // These are applied via BonusStartingResources and other modifiers
            // Additional effects can be added here
        }
    }
}
