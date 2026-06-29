using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectLM.Core;

namespace ProjectLM.Core
{
    /// <summary>
    /// Observes player click behavior across all axes.
    /// Accumulates a BehaviorProfile that is read by the LLM bridge
    /// at the end of each wave.
    /// </summary>
    public class BehaviorObserver : MonoBehaviour
    {
        [Header("Decay Settings")]
        [SerializeField] private float decayRate = 0.02f;

        [Header("Axis Weights")]
        [SerializeField] private float aggressionPerAttackClick = 0.15f;
        [SerializeField] private float cautionPerDefendClick = 0.12f;
        [SerializeField] private float greedPerMineClick = 0.10f;
        [SerializeField] private float curiosityPerExploreClick = 0.14f;
        [SerializeField] private float patiencePerSecondIdle = 0.01f;

        private float[] _axes; // index = (int)BehaviorAxis
        private int _totalClicks;
        private float _lastClickTime;
        private float _healthCriticalThreshold = 0.3f;
        private int _lowHealthActions;

        private Queue<ClickEvent> _recentClicks = new Queue<ClickEvent>();
        private const int MaxRecentClicks = 100;

        private void Awake()
        {
            _axes = new float[Enum.GetValues(typeof(BehaviorAxis)).Length];
            _lastClickTime = Time.time;
        }

        private void Update()
        {
            // Idle patience: slowly tick up patience when not clicking
            if (Time.time - _lastClickTime > 2f)
            {
                AddAxis(BehaviorAxis.Patience, patiencePerSecondIdle * Time.deltaTime);
            }

            // Decay all axes slowly toward 0
            for (int i = 0; i < _axes.Length; i++)
            {
                _axes[i] = Mathf.Clamp01(_axes[i] - decayRate * Time.deltaTime);
            }
        }

        /// <summary>
        /// Called by a ClickNode when the player taps it.
        /// </summary>
        public void RecordClick(string nodeId, float currentHealthPercent, int waveNumber)
        {
            float now = Time.time;
            float timeSinceLast = now - _lastClickTime;
            _lastClickTime = now;
            _totalClicks++;

            // Store event
            var evt = new ClickEvent
            {
                nodeId = nodeId,
                gameTime = now,
                playerHealthPercent = currentHealthPercent,
                waveNumber = waveNumber,
                timeSinceLastClick = timeSinceLast
            };
            _recentClicks.Enqueue(evt);
            while (_recentClicks.Count > MaxRecentClicks)
                _recentClicks.Dequeue();

            // Apply axis changes based on node type
            switch (nodeId)
            {
                case "attack":
                    AddAxis(BehaviorAxis.Aggression, aggressionPerAttackClick);
                    break;
                case "defend":
                    AddAxis(BehaviorAxis.Caution, cautionPerDefendClick);
                    break;
                case "mine":
                    AddAxis(BehaviorAxis.Greed, greedPerMineClick);
                    break;
                case "explore":
                    AddAxis(BehaviorAxis.Curiosity, curiosityPerExploreClick);
                    break;
                case "heal":
                    AddAxis(BehaviorAxis.Caution, cautionPerDefendClick * 0.5f);
                    break;
            }

            // Fast clicks = aggression
            if (timeSinceLast < 0.3f)
                AddAxis(BehaviorAxis.Reactivity, 0.05f);

            // Clicking at low health = risk tolerance
            if (currentHealthPercent < _healthCriticalThreshold)
            {
                AddAxis(BehaviorAxis.RiskTolerance, 0.08f);
                _lowHealthActions++;
            }

            // Very steady rhythm = patience
            if (timeSinceLast is > 0.5f and < 2.0f)
                AddAxis(BehaviorAxis.Patience, 0.03f);
        }

        /// <summary>
        /// Called when the player picks a synergy/team upgrade vs selfish.
        /// </summary>
        public void RecordUpgradeChoice(bool isAltruistic)
        {
            AddAxis(BehaviorAxis.Altruism, isAltruistic ? 0.15f : -0.08f);
        }

        /// <summary>
        /// Get the current behavioral profile snapshot.
        /// </summary>
        public BehaviorProfile GetProfile()
        {
            return new BehaviorProfile
            {
                Aggression = _axes[(int)BehaviorAxis.Aggression],
                Caution = _axes[(int)BehaviorAxis.Caution],
                Greed = _axes[(int)BehaviorAxis.Greed],
                Curiosity = _axes[(int)BehaviorAxis.Curiosity],
                Patience = _axes[(int)BehaviorAxis.Patience],
                Reactivity = _axes[(int)BehaviorAxis.Reactivity],
                Altruism = _axes[(int)BehaviorAxis.Altruism],
                RiskTolerance = _axes[(int)BehaviorAxis.RiskTolerance]
            };
        }

        /// <summary>
        /// Build a text summary of the player's behavior for the LLM prompt.
        /// </summary>
        public string BuildBehaviorSummary()
        {
            var p = GetProfile();
            string dominant = p.DominantTrait();
            float avgClickFreq = _totalClicks / Mathf.Max(Time.time, 1f);
            float defensiveRatio = _recentClicks.Count > 0
                ? CountNodeType("defend") / (float)_recentClicks.Count
                : 0;

            return
                $"Profil comportemental du joueur:\n" +
                $"- Axes: {p}\n" +
                $"- Trait dominant: {dominant}\n" +
                $"- Clics totaux: {_totalClicks}\n" +
                $"- Fréquence moyenne: {avgClickFreq:F2} clics/s\n" +
                $"- Ratio défensif: {defensiveRatio:F2}\n" +
                $"- Actions à PV critiques: {_lowHealthActions}\n" +
                $"- Dernier type de nœud: {(GetLastNodeType())}";
        }

        public int TotalClicks => _totalClicks;

        private void AddAxis(BehaviorAxis axis, float amount)
        {
            int idx = (int)axis;
            _axes[idx] = Mathf.Clamp01(_axes[idx] + amount);
        }

        private float CountNodeType(string type)
        {
            int count = 0;
            foreach (var e in _recentClicks)
                if (e.nodeId == type) count++;
            return count;
        }

        private string GetLastNodeType()
        {
            if (_recentClicks.Count == 0) return "aucun";
            return _recentClicks.ToArray()[_recentClicks.Count - 1].nodeId;
        }

        public void Reset()
        {
            for (int i = 0; i < _axes.Length; i++)
                _axes[i] = 0;
            _totalClicks = 0;
            _recentClicks.Clear();
            _lowHealthActions = 0;
            _lastClickTime = Time.time;
        }
    }
}
