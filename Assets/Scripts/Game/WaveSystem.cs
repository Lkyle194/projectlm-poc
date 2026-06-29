using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectLM.LLM;

namespace ProjectLM.Game
{
    /// <summary>
    /// Manages wave progression. Enemies attack the player over time.
    /// Player must click Attack to defeat enemies before the timer runs out.
    /// </summary>
    public class WaveSystem : MonoBehaviour
    {
        [Header("Wave Config")]
        [SerializeField] private float baseEnemyHealth = 10f;
        [SerializeField] private float waveDuration = 30f;
        [SerializeField] private float attackInterval = 2f; // How often enemies attack

        [Header("UI")]
        [SerializeField] private Image enemyHealthBar;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Image enemyIcon;

        [Header("Enemy Visuals")]
        [SerializeField] private Color[] waveColors;
        [SerializeField] private string[] enemyNames;

        // State
        private float _enemyCurrentHealth;
        private float _enemyMaxHealth;
        private float _waveTimer;
        private float _defenseTimer;
        private float _defenseReduction;
        private int _currentWave;
        private bool _waveActive;
        private GameManager _gameManager;

        private void Start()
        {
            _gameManager = FindAnyObjectByType<GameManager>();
        }

        public void StartWave(int waveNumber, float difficulty)
        {
            _currentWave = waveNumber;
            _enemyMaxHealth = baseEnemyHealth * difficulty;
            _enemyCurrentHealth = _enemyMaxHealth;
            _waveTimer = waveDuration;
            _defenseTimer = 0;
            _defenseReduction = 0;
            _waveActive = true;

            // Update UI
            string enemyName = GetEnemyName(waveNumber);
            if (enemyNameText) enemyNameText.text = $"👾 {enemyName}";
            if (enemyHealthBar) enemyHealthBar.fillAmount = 1f;
            if (enemyIcon) enemyIcon.color = GetWaveColor(waveNumber);
            UpdateTimerDisplay();

            // Start enemy attack coroutine
            StopAllCoroutines();
            StartCoroutine(EnemyAttackRoutine());
        }

        private void Update()
        {
            if (!_waveActive) return;

            // Countdown
            _waveTimer -= Time.deltaTime;

            // Defense timer
            if (_defenseTimer > 0)
                _defenseTimer -= Time.deltaTime;
            else
                _defenseReduction = 0;

            UpdateTimerDisplay();

            // Wave timeout = enemy escapes, player takes damage
            if (_waveTimer <= 0)
            {
                _waveActive = false;
                _gameManager?.TakeDamage(15f * _currentWave);
                EndWave();
            }

            // Check if enemy is defeated
            if (_enemyCurrentHealth <= 0 && _waveActive)
            {
                _waveActive = false;
                EndWave(true);
            }
        }

        /// <summary>
        /// Called by GameManager when player clicks Attack.
        /// </summary>
        public void DamageEnemy(float amount)
        {
            if (!_waveActive) return;

            _enemyCurrentHealth -= amount;
            if (enemyHealthBar)
                enemyHealthBar.fillAmount = Mathf.Clamp01(_enemyCurrentHealth / _enemyMaxHealth);

            // Visual feedback
            if (enemyIcon)
                StartCoroutine(FlashEnemyIcon());

            UpdateTimerDisplay(); // Refresh timer to show it's still running
        }

        /// <summary>
        /// Called by GameManager when player clicks Defend.
        /// </summary>
        public void ActivateDefense(float reduction)
        {
            _defenseReduction = Mathf.Max(_defenseReduction, reduction);
            _defenseTimer = 3f; // Defense lasts 3 seconds
        }

        private IEnumerator EnemyAttackRoutine()
        {
            while (_waveActive)
            {
                yield return new WaitForSeconds(attackInterval);

                if (!_waveActive) yield break;

                float baseDamage = 3f + (_currentWave * 1.5f);
                float defenseMitigation = Mathf.Clamp01(1f - _defenseReduction);
                float finalDamage = baseDamage * defenseMitigation;

                _gameManager?.TakeDamage(finalDamage);
            }
        }

        private void EndWave(bool victory = false)
        {
            _waveActive = false;
            StopAllCoroutines();

            if (victory)
            {
                _gameManager?.OnWaveCleared();
            }
            else
            {
                // Timeout: still count as cleared (damage already dealt)
                _gameManager?.OnWaveCleared();
            }
        }

        private string GetEnemyName(int wave)
        {
            if (wave <= enemyNames.Length)
                return enemyNames[wave - 1];
            return $"Ombre Niveau {wave}";
        }

        private Color GetWaveColor(int wave)
        {
            if (waveColors.Length > 0)
                return waveColors[(wave - 1) % waveColors.Length];
            return Color.white;
        }

        private void UpdateTimerDisplay()
        {
            if (timerText)
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(_waveTimer));
                timerText.text = $"{seconds}s";
            }
        }

        private IEnumerator FlashEnemyIcon()
        {
            if (!enemyIcon) yield break;
            Color original = enemyIcon.color;
            enemyIcon.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            enemyIcon.color = original;
        }
    }
}
