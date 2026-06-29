using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ProjectLM.Core;

namespace ProjectLM.Game
{
    /// <summary>
    /// A clickable node in the game. Each node has a type (attack, defend, mine, explore, heal).
    /// Tracks individual stats and reports clicks to the BehaviorObserver.
    /// </summary>
    public class ClickNode : MonoBehaviour, IPointerClickHandler
    {
        [Header("Config")]
        [SerializeField] private string nodeId = "attack";
        [SerializeField] private string nodeName = "⚔️ Attaque";
        [SerializeField] private Color nodeColor = Color.red;
        [SerializeField] private float baseClickValue = 1f;

        [Header("UI References")]
        [SerializeField] private Image nodeImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Image cooldownOverlay;

        [Header("Animation")]
        [SerializeField] private float punchScale = 1.2f;
        [SerializeField] private float punchDuration = 0.15f;

        // Runtime
        private BehaviorObserver _observer;
        private GameManager _gameManager;
        private float _clickMultiplier = 1f;
        private float _cooldownTimer;
        private float _cooldownDuration;
        private int _clickCount;
        private bool _isUnlocked = true;

        public string NodeId => nodeId;
        public int ClickCount => _clickCount;
        public float ClickMultiplier { get => _clickMultiplier; set => _clickMultiplier = value; }
        public bool IsUnlocked { get => _isUnlocked; set => _isUnlocked = value; }
        public float CooldownDuration { get => _cooldownDuration; set => _cooldownDuration = value; }

        private void Start()
        {
            _observer = FindAnyObjectByType<BehaviorObserver>();
            _gameManager = FindAnyObjectByType<GameManager>();

            if (nodeImage) nodeImage.color = nodeColor;
            if (nameText) nameText.text = nodeName;
            UpdateValueDisplay();

            // Reset cooldown overlay
            if (cooldownOverlay) cooldownOverlay.fillAmount = 0;
        }

        private void Update()
        {
            // Handle cooldown
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
                if (cooldownOverlay)
                    cooldownOverlay.fillAmount = _cooldownTimer / Mathf.Max(_cooldownDuration, 0.01f);
            }
            else
            {
                if (cooldownOverlay) cooldownOverlay.fillAmount = 0;
            }

            // Update value display periodically
            if (valueText && Time.frameCount % 30 == 0)
                UpdateValueDisplay();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isUnlocked || _cooldownTimer > 0) return;

            _clickCount++;
            _cooldownTimer = _cooldownDuration;

            // Report to observer
            float healthPercent = _gameManager ? _gameManager.HealthPercent : 1f;
            _observer?.RecordClick(nodeId, healthPercent, _gameManager?.CurrentWave ?? 0);

            // Apply click effect
            float value = baseClickValue * _clickMultiplier;
            _gameManager?.OnNodeClicked(nodeId, value);

            // Visual feedback
            if (nodeImage)
                StartCoroutine(PunchAnimation());

            UpdateValueDisplay();
        }

        private void UpdateValueDisplay()
        {
            if (valueText)
            {
                float displayValue = baseClickValue * _clickMultiplier;
                valueText.text = $"+{displayValue:F1}";
            }
        }

        private System.Collections.IEnumerator PunchAnimation()
        {
            Vector3 original = transform.localScale;
            float elapsed = 0;
            while (elapsed < punchDuration * 0.5f)
            {
                float t = elapsed / (punchDuration * 0.5f);
                transform.localScale = Vector3.Lerp(original, Vector3.one * punchScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            elapsed = 0;
            while (elapsed < punchDuration * 0.5f)
            {
                float t = elapsed / (punchDuration * 0.5f);
                transform.localScale = Vector3.Lerp(Vector3.one * punchScale, original, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = original;
        }

        /// <summary>
        /// Apply an upgrade to this node.
        /// </summary>
        public void ApplyUpgrade(string effectType, float value)
        {
            switch (effectType)
            {
                case "click_damage":
                    _clickMultiplier *= value;
                    break;
                case "defense":
                    // Defense reduces incoming damage globally, handled by GameManager
                    break;
                case "resource_mult":
                    // Resource multiplier increases mine output
                    if (nodeId == "mine")
                        _clickMultiplier *= value;
                    break;
                case "special":
                    _clickMultiplier *= value;
                    break;
            }

            UpdateValueDisplay();
        }
    }
}
