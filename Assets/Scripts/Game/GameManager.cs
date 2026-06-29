using System.Collections.Generic;
using System.IO;
using ProjectLM.LLM;
using ProjectLM.Core;
using ProjectLM.UI;
using UnityEngine;

namespace ProjectLM.Game
{
    /// <summary>
    /// Central game manager. Owns the game state, wave progression,
    /// health/resources, and coordinates all systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Balance")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float startingResources = 10f;
        [SerializeField] private float enemyDamagePerWave = 5f;
        [SerializeField] private float healthRegenPerClick = 0.5f;
        [SerializeField] private int wavesPerRun = 8;

        [Header("LLM Settings")]
        [SerializeField] private string modelFileName = "gemma-4-E2B-it-qat-mobile-Q4_0.gguf";
        [SerializeField] private bool useLLM = false; // Toggle in inspector for testing

        // Systems
        private BehaviorObserver _observer;
        private ILLMBridge _llmBridge;
        private WaveSystem _waveSystem;
        private SkillUnlocker _skillUnlocker;
        private NodeSpawner _nodeSpawner;
        private RogueliteManager _rogueliteManager;
        private UIManager _uiManager;
        private ModelDeployer _modelDeployer;

        // State
        private float _currentHealth;
        private float _currentResources;
        private int _currentWave;
        private bool _isGameOver;
        private bool _isInUnlockPhase;
        private List<string> _unlockedUpgrades = new List<string>();
        private Dictionary<string, float> _nodeClickValues = new Dictionary<string, float>();

        // Public accessors
        public float HealthPercent => _currentHealth / maxHealth;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public float CurrentResources => _currentResources;
        public int CurrentWave => _currentWave;
        public bool IsGameOver => _isGameOver;
        public bool IsInUnlockPhase => _isInUnlockPhase;
        public int WavesPerRun => wavesPerRun;
        public List<string> UnlockedUpgrades => _unlockedUpgrades;
        public ILLMBridge LLMBridge => _llmBridge;

        private void Awake()
        {
            _observer = FindAnyObjectByType<BehaviorObserver>();
            _waveSystem = FindAnyObjectByType<WaveSystem>();
            _skillUnlocker = FindAnyObjectByType<SkillUnlocker>();
            _nodeSpawner = FindAnyObjectByType<NodeSpawner>();
            _rogueliteManager = FindAnyObjectByType<RogueliteManager>();
            _uiManager = FindAnyObjectByType<UIManager>();
            _modelDeployer = FindAnyObjectByType<ModelDeployer>();
        }

        private async void Start()
        {
            // Create LLM bridge
            _llmBridge = LLMFactory.CreateBridge();

            if (useLLM)
            {
                string modelPath = _modelDeployer != null
                    ? _modelDeployer.GetModelPath()
                    : Path.Combine(Application.streamingAssetsPath, "models", modelFileName);
                Debug.Log($"[GameManager] Loading model: {modelPath}");
                bool loaded = await _llmBridge.LoadModel(modelPath);
                if (!loaded)
                {
                    Debug.LogWarning("[GameManager] Model load failed, falling back to dummy");
                    _llmBridge = new DummyLLMBridge();
                }
            }

            // Load roguelite save data
            _rogueliteManager?.LoadProgress();

            // Start first wave
            StartNewRun();
        }

        public void StartNewRun()
        {
            _currentHealth = maxHealth;
            _currentResources = startingResources + (_rogueliteManager?.BonusStartingResources ?? 0);
            _currentWave = 0;
            _isGameOver = false;
            _isInUnlockPhase = false;
            _unlockedUpgrades.Clear();
            _nodeClickValues.Clear();
            _observer?.Reset();

            Debug.Log("[GameManager] New run started");
            _uiManager?.ShowGameScreen();

            // Apply permanent upgrades from previous runs
            _rogueliteManager?.ApplyPermanentUpgrades(this);

            StartNextWave();
        }

        public void StartNextWave()
        {
            _currentWave++;

            if (_currentWave > wavesPerRun)
            {
                // Victory! Player completed the run
                OnRunComplete();
                return;
            }

            float difficulty = 1f + (_currentWave - 1) * 0.2f;
            _waveSystem?.StartWave(_currentWave, difficulty);
            _nodeSpawner?.SpawnNodesForWave(_currentWave);

            _isInUnlockPhase = false;
            _uiManager?.ShowWaveStart(_currentWave);
            _uiManager?.UpdateUI(_currentHealth, maxHealth, _currentResources, _currentWave);

            Debug.Log($"[GameManager] Wave {_currentWave} started (difficulty: {difficulty:F2}x)");
        }

        /// <summary>
        /// Called by ClickNode when a node is clicked.
        /// </summary>
        public void OnNodeClicked(string nodeId, float value)
        {
            if (_isGameOver || _isInUnlockPhase) return;

            // Track click value
            if (!_nodeClickValues.ContainsKey(nodeId))
                _nodeClickValues[nodeId] = 0;
            _nodeClickValues[nodeId] += value;

            switch (nodeId)
            {
                case "attack":
                    // Deal damage to current wave enemy
                    _waveSystem?.DamageEnemy(value);
                    break;
                case "mine":
                    _currentResources += value;
                    break;
                case "defend":
                    // Block incoming damage for a short time
                    _waveSystem?.ActivateDefense(value * 0.1f);
                    break;
                case "explore":
                    // Chance to find bonus resources or health
                    float bonus = value * (Random.value > 0.5f ? 2f : 0.5f);
                    _currentResources += bonus * 0.5f;
                    break;
                case "heal":
                    _currentHealth = Mathf.Min(maxHealth, _currentHealth + value * healthRegenPerClick);
                    break;
            }

            _uiManager?.UpdateUI(_currentHealth, maxHealth, _currentResources, _currentWave);
        }

        /// <summary>
        /// Called by WaveSystem when the current wave ends.
        /// </summary>
        public void OnWaveCleared()
        {
            Debug.Log($"[GameManager] Wave {_currentWave} cleared!");

            // Give base reward
            float waveReward = 5f * _currentWave;
            _currentResources += waveReward;

            // Check if it's time for an LLM unlock (every 2 waves, plus first wave)
            if (_currentWave % 2 == 1 || _currentWave == 1)
            {
                TriggerLLMUnlock();
            }
            else
            {
                // Short delay then next wave
                Invoke(nameof(StartNextWave), 1.5f);
            }
        }

        /// <summary>
        /// Called by WaveSystem when an enemy deals damage to the player.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (_isGameOver) return;

            float defense = _skillUnlocker?.GetDefenseMultiplier() ?? 1f;
            _currentHealth -= damage / defense;

            _uiManager?.UpdateUI(_currentHealth, maxHealth, _currentResources, _currentWave);

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                OnPlayerDeath();
            }
        }

        private void OnPlayerDeath()
        {
            _isGameOver = true;
            Debug.Log("[GameManager] Player defeated!");

            // Unload LLM model to free memory
            if (useLLM) _llmBridge?.UnloadModel();

            // Roguelite: offer permanent upgrades based on this run
            _rogueliteManager?.OnRunEnded(_currentWave, _observer?.GetProfile() ?? default);
            _uiManager?.ShowGameOverScreen(_currentWave, _observer?.GetProfile().DominantTrait() ?? "none");
        }

        private void OnRunComplete()
        {
            _isGameOver = true;
            Debug.Log("[GameManager] Run completed! Victory!");

            if (useLLM) _llmBridge?.UnloadModel();

            _rogueliteManager?.OnRunEnded(_currentWave, _observer?.GetProfile() ?? default);
            _uiManager?.ShowVictoryScreen(_observer?.GetProfile().DominantTrait() ?? "none");
        }

        private async void TriggerLLMUnlock()
        {
            _isInUnlockPhase = true;
            _uiManager?.ShowThinkingAnimation(true);
            _uiManager?.ShowUnlockMessage("🧠 L'IA analyse ton comportement...");

            var profile = _observer?.GetProfile() ?? default;
            string summary = _observer?.BuildBehaviorSummary() ?? "Aucune donnée";
            string[] owned = _unlockedUpgrades.ToArray();

            // Generate unlock via LLM
            var unlock = await _llmBridge.GenerateUnlock(profile, summary, _currentWave, owned);
            string narration = await _llmBridge.GenerateNarration(unlock.behaviorLabel, unlock.unlockName);

            _uiManager?.ShowThinkingAnimation(false);

            // Apply unlock
            _skillUnlocker?.ApplyUnlock(unlock);
            _unlockedUpgrades.Add(unlock.unlockName + " (" + unlock.unlockType + ")");

            // Spawn a new node if it's a new path
            if (!string.IsNullOrEmpty(unlock.newPath))
            {
                _nodeSpawner?.SpawnPathNode(unlock.newPath, unlock.unlockName);
            }

            _uiManager?.ShowUnlockCard(narration, unlock);
            Debug.Log($"[GameManager] LLM unlock: {unlock.unlockName} ({unlock.behaviorLabel})");

            // Resume after player dismisses the unlock card (auto after delay)
            Invoke(nameof(ResumeAfterUnlock), 4f);
        }

        private void ResumeAfterUnlock()
        {
            _uiManager?.ShowUnlockCard(false);
            StartNextWave();
        }

        private void OnDestroy()
        {
            if (useLLM) _llmBridge?.UnloadModel();
        }
    }
}
