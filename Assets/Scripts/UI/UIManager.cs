using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectLM.LLM;
using ProjectLM.Core;
using ProjectLM.Game;

namespace ProjectLM.UI
{
    /// <summary>
    /// Manages all UI panels and transitions.
    /// Auto-discovers all UI elements by name — zero inspector setup needed.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Screens
        private GameObject _titleScreen;
        private GameObject _gameScreen;
        private GameObject _gameOverScreen;
        private GameObject _victoryScreen;
        private GameObject _upgradeShopScreen;

        // HUD
        private Image _healthBarFill;
        private TMP_Text _healthText;
        private TMP_Text _resourcesText;
        private TMP_Text _waveText;
        private TMP_Text _waveAnnounceText;
        private Animator _waveAnnounceAnimator;

        // LLM Unlock
        private GameObject _unlockCard;
        private TMP_Text _unlockNarrationText;
        private TMP_Text _unlockNameText;
        private TMP_Text _unlockDescriptionText;
        private TMP_Text _unlockBehaviorText;
        private Animator _unlockAnimator;

        // Thinking
        private GameObject _thinkingIndicator;
        private TMP_Text _thinkingText;

        // Game Over
        private TMP_Text _gameOverWaveText;
        private TMP_Text _gameOverTraitText;
        private TMP_Text _gameOverEssenceText;

        // Victory
        private TMP_Text _victoryTraitText;
        private TMP_Text _victoryEssenceText;

        // Shop
        private Transform _upgradeContainer;
        private TMP_Text _essenceTotalText;

        // Audio (optional — loaded from Resources)
        private AudioSource _audioSource;
        private AudioClip _clickSound;
        private AudioClip _unlockSound;
        private AudioClip _waveStartSound;
        private AudioClip _gameOverSound;

        // Systems
        private BehaviorObserver _observer;
        private RogueliteManager _rogueliteManager;

        // Enemy
        private TMP_Text _enemyNameText;
        private Image _enemyHealthBar;

        private void Awake()
        {
            DiscoverAllUI();
        }

        private void Start()
        {
            _observer = FindAnyObjectByType<BehaviorObserver>();
            _rogueliteManager = FindAnyObjectByType<RogueliteManager>();
            LoadAudio();
            ShowTitleScreen();
        }

        private void Update()
        {
            // Behavior panel update — disabled for now (no sliders in scene)
        }

        // ======================================================================
        // AUTO-DISCOVERY: Finds all UI elements by GameObject name
        // ======================================================================
        private void DiscoverAllUI()
        {
            // Helper: find GO by name, get component
            GameObject G(string n) => GameObject.Find(n);
            
            // Helper: find child by name path
            Transform C(Transform parent, string childName)
            {
                if (parent == null) return null;
                foreach (Transform t in parent)
                {
                    if (t.name == childName) return t;
                    var deeper = C(t, childName);
                    if (deeper != null) return deeper;
                }
                return null;
            }

            // Screens
            _titleScreen = G("TitleScreen");
            _gameScreen = G("GameScreen");
            _gameOverScreen = G("GameOverScreen");
            _victoryScreen = G("VictoryScreen");
            _upgradeShopScreen = G("UpgradeShop");

            // HUD
            var hbBg = G("HealthBarBg");
            if (hbBg)
            {
                // Children of HealthBarBg
                var fillT = C(hbBg.transform, "HealthBarFill");
                _healthBarFill = fillT?.GetComponent<Image>();
                var hpT = C(hbBg.transform, "HealthText");
                _healthText = hpT?.GetComponent<TMP_Text>();
            }
            _resourcesText = G("ResourcesText")?.GetComponent<TMP_Text>();
            _waveText = G("WaveText")?.GetComponent<TMP_Text>();

            var announce = G("WaveAnnounce");
            if (announce)
            {
                _waveAnnounceText = announce.GetComponent<TMP_Text>();
                _waveAnnounceAnimator = announce.GetComponent<Animator>();
            }

            // LLM Unlock
            _unlockCard = G("UnlockCard");
            if (_unlockCard)
            {
                var t = _unlockCard.transform;
                _unlockBehaviorText = C(t, "UnlockBehaviorText")?.GetComponent<TMP_Text>();
                _unlockNameText = C(t, "UnlockNameText")?.GetComponent<TMP_Text>();
                _unlockDescriptionText = C(t, "UnlockDescriptionText")?.GetComponent<TMP_Text>();
                _unlockNarrationText = C(t, "UnlockNarrationText")?.GetComponent<TMP_Text>();
                _unlockAnimator = _unlockCard.GetComponent<Animator>();
            }

            // Thinking
            _thinkingIndicator = G("ThinkingIndicator");
            if (_thinkingIndicator)
                _thinkingText = _thinkingIndicator.GetComponent<TMP_Text>();

            // Enemy info
            var enemy = G("EnemyInfo");
            if (enemy)
            {
                var t = enemy.transform;
                _enemyNameText = C(t, "EnemyNameText")?.GetComponent<TMP_Text>();
                _enemyHealthBar = C(t, "EnemyHealthBar")?.GetComponent<Image>();
                // TimerText is also here but not used in code currently
            }

            // Game Over
            var go = _gameOverScreen;
            if (go)
            {
                var t = go.transform;
                _gameOverWaveText = C(t, "GOWaveText")?.GetComponent<TMP_Text>();
                _gameOverTraitText = C(t, "GOTraitText")?.GetComponent<TMP_Text>();
                _gameOverEssenceText = C(t, "GOEssenceText")?.GetComponent<TMP_Text>();
            }

            // Victory
            var vic = _victoryScreen;
            if (vic)
            {
                var t = vic.transform;
                _victoryTraitText = C(t, "VTraitText")?.GetComponent<TMP_Text>();
                _victoryEssenceText = C(t, "VEssenceText")?.GetComponent<TMP_Text>();
            }

            // Shop
            var shop = _upgradeShopScreen;
            if (shop)
            {
                var t = shop.transform;
                _essenceTotalText = C(t, "ShopEssenceTotal")?.GetComponent<TMP_Text>();
                var cont = C(t, "UpgradeContainer");
                _upgradeContainer = cont;
            }

            // AudioSource on this GameObject
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.volume = 0.5f;
            }
        }

        private void LoadAudio()
        {
            // Try to load audio clips from Resources
            _clickSound = Resources.Load<AudioClip>("Audio/click");
            _unlockSound = Resources.Load<AudioClip>("Audio/unlock");
            _waveStartSound = Resources.Load<AudioClip>("Audio/wave_start");
            _gameOverSound = Resources.Load<AudioClip>("Audio/game_over");
            
            // If Resources load fails, try finding via type (null is fine, audio is optional)
            if (_clickSound == null)
                Debug.Log("[UIManager] Audio clips not found in Resources — game will run without audio");
        }

        // ======================================================================
        // PUBLIC API
        // ======================================================================

        public void ShowTitleScreen()
        {
            SetAllScreensOff();
            if (_titleScreen) _titleScreen.SetActive(true);
        }

        public void ShowGameScreen()
        {
            SetAllScreensOff();
            if (_gameScreen) _gameScreen.SetActive(true);
        }

        public void ShowGameOverScreen(int wave, string dominantTrait)
        {
            SetAllScreensOff();
            if (_gameOverScreen) _gameOverScreen.SetActive(true);
            if (_gameOverWaveText) _gameOverWaveText.text = $"Vague atteinte: {wave}";
            if (_gameOverTraitText) _gameOverTraitText.text = $"Style: {dominantTrait}";
            if (_gameOverEssenceText) _gameOverEssenceText.text = $"Essence: +{wave * 10}";
            PlaySound(_gameOverSound);
        }

        public void ShowVictoryScreen(string dominantTrait)
        {
            SetAllScreensOff();
            if (_victoryScreen) _victoryScreen.SetActive(true);
            if (_victoryTraitText) _victoryTraitText.text = $"Style dominant: {dominantTrait}";
            if (_victoryEssenceText) _victoryEssenceText.text = "Essence: +100";
        }

        public void ShowUpgradeShop()
        {
            SetAllScreensOff();
            if (_upgradeShopScreen) _upgradeShopScreen.SetActive(true);
            PopulateUpgradeShop();
        }

        public void UpdateUI(float health, float maxHealth, float resources, int wave)
        {
            if (_healthBarFill) _healthBarFill.fillAmount = health / maxHealth;
            if (_healthText) _healthText.text = $"{Mathf.CeilToInt(health)}/{Mathf.CeilToInt(maxHealth)}";
            if (_resourcesText) _resourcesText.text = $"💰 {resources:F0}";
            if (_waveText) _waveText.text = $"Vague {wave}";
        }

        public void ShowWaveStart(int wave)
        {
            if (_waveAnnounceText) _waveAnnounceText.text = $"VAGUE {wave}";
            if (_waveAnnounceAnimator) _waveAnnounceAnimator.SetTrigger("Show");
            PlaySound(_waveStartSound);
        }

        public void ShowThinkingAnimation(bool show)
        {
            if (_thinkingIndicator) _thinkingIndicator.SetActive(show);
        }

        public void ShowUnlockMessage(string message)
        {
            if (_thinkingText) _thinkingText.text = message;
        }

        public void ShowUnlockCard(string narration, LlmUnlock unlock)
        {
            if (_unlockCard)
            {
                _unlockCard.SetActive(true);
                if (_unlockNarrationText) _unlockNarrationText.text = narration;
                if (_unlockNameText) _unlockNameText.text = $"✨ {unlock.unlockName} ✨";
                if (_unlockDescriptionText) _unlockDescriptionText.text = unlock.unlockDescription;
                if (_unlockBehaviorText) _unlockBehaviorText.text = $"🧠 {unlock.behaviorLabel}";
                if (_unlockAnimator) _unlockAnimator.SetTrigger("Reveal");
                PlaySound(_unlockSound);
            }
        }

        public void ShowUnlockCard(bool show)
        {
            if (_unlockCard) _unlockCard.SetActive(show);
        }

        public void OnStartGameClicked()
        {
            var gm = FindAnyObjectByType<GameManager>();
            if (gm) gm.StartNewRun();
        }

        public void OnRetryClicked()
        {
            var gm = FindAnyObjectByType<GameManager>();
            if (gm) gm.StartNewRun();
        }

        public void OnUpgradeShopClicked()
        {
            ShowUpgradeShop();
        }

        // ======================================================================
        // PRIVATE
        // ======================================================================

        private void SetAllScreensOff()
        {
            if (_titleScreen) _titleScreen.SetActive(false);
            if (_gameScreen) _gameScreen.SetActive(false);
            if (_gameOverScreen) _gameOverScreen.SetActive(false);
            if (_victoryScreen) _victoryScreen.SetActive(false);
            if (_upgradeShopScreen) _upgradeShopScreen.SetActive(false);
        }

        private void PopulateUpgradeShop()
        {
            if (_upgradeContainer == null || _rogueliteManager == null) return;

            // Clear
            foreach (Transform child in _upgradeContainer)
                Destroy(child.gameObject);

            if (_essenceTotalText)
                _essenceTotalText.text = $"Essence: {_rogueliteManager.Essence}";

            var available = _rogueliteManager.GetAvailableUpgrades();
            foreach (var ug in available)
            {
                var item = new GameObject("UpgradeItem");
                item.transform.SetParent(_upgradeContainer, false);

                var rt = item.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(700, 80);

                var nameText = item.AddComponent<TextMeshProUGUI>();
                nameText.text = ug.name;
                nameText.fontSize = 18;
                nameText.alignment = TextAlignmentOptions.Left;
                nameText.color = Color.white;
                nameText.rectTransform.anchoredPosition = new Vector2(-300, 10);
                nameText.rectTransform.sizeDelta = new Vector2(500, 30);
                nameText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

                var descText = item.AddComponent<TextMeshProUGUI>();
                descText.text = ug.description;
                descText.fontSize = 14;
                descText.alignment = TextAlignmentOptions.Left;
                descText.color = new Color(0.7f, 0.7f, 0.9f);
                descText.rectTransform.anchoredPosition = new Vector2(-300, -15);
                descText.rectTransform.sizeDelta = new Vector2(500, 25);
                descText.font = nameText.font;

                // Simple button via raycast on the item
                var btn = item.AddComponent<Button>();
                string capturedId = ug.id;
                btn.onClick.AddListener(() =>
                {
                    if (_rogueliteManager.PurchaseUpgrade(capturedId))
                        PopulateUpgradeShop();
                });

                // Buy text overlay
                var buyText = item.AddComponent<TextMeshProUGUI>();
                buyText.text = $"Acheter ({ug.cost})";
                buyText.fontSize = 16;
                buyText.alignment = TextAlignmentOptions.Right;
                buyText.color = new Color(1, 0.84f, 0);
                buyText.rectTransform.anchoredPosition = new Vector2(300, 0);
                buyText.rectTransform.sizeDelta = new Vector2(150, 30);
                buyText.font = nameText.font;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip && _audioSource) _audioSource.PlayOneShot(clip);
        }
    }
}
