using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectLM.LLM;
using ProjectLM.Core;

namespace ProjectLM.UI
{
    /// <summary>
    /// Manages all UI panels and transitions.
    /// Works with Unity Canvas + TextMeshPro.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject titleScreen;
        [SerializeField] private GameObject gameScreen;
        [SerializeField] private GameObject gameOverScreen;
        [SerializeField] private GameObject victoryScreen;
        [SerializeField] private GameObject upgradeShopScreen;

        [Header("Game HUD")]
        [SerializeField] private Image healthBar;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text resourcesText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text waveAnnounceText;
        [SerializeField] private Animator waveAnnounceAnimator;

        [Header("LLM Unlock UI")]
        [SerializeField] private GameObject unlockCard;
        [SerializeField] private TMP_Text unlockNarrationText;
        [SerializeField] private TMP_Text unlockNameText;
        [SerializeField] private TMP_Text unlockDescriptionText;
        [SerializeField] private TMP_Text unlockBehaviorText;
        [SerializeField] private Animator unlockAnimator;

        [Header("Behavior Display")]
        [SerializeField] private RectTransform behaviorPanel;
        [SerializeField] private Slider[] behaviorSliders; // 8 sliders for 8 axes

        [Header("Thinking Indicator")]
        [SerializeField] private GameObject thinkingIndicator;
        [SerializeField] private TMP_Text thinkingText;

        [Header("Game Over")]
        [SerializeField] private TMP_Text gameOverWaveText;
        [SerializeField] private TMP_Text gameOverTraitText;
        [SerializeField] private TMP_Text gameOverEssenceText;

        [Header("Victory")]
        [SerializeField] private TMP_Text victoryTraitText;
        [SerializeField] private TMP_Text victoryEssenceText;

        [Header("Upgrade Shop")]
        [SerializeField] private Transform upgradeContainer;
        [SerializeField] private GameObject upgradeItemPrefab;
        [SerializeField] private TMP_Text essenceTotalText;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip unlockSound;
        [SerializeField] private AudioClip waveStartSound;
        [SerializeField] private AudioClip gameOverSound;

        private BehaviorObserver _observer;
        private RogueliteManager _rogueliteManager;

        private void Start()
        {
            _observer = FindAnyObjectByType<BehaviorObserver>();
            _rogueliteManager = FindAnyObjectByType<RogueliteManager>();

            ShowTitleScreen();
        }

        private void Update()
        {
            // Update behavior sliders in real-time if visible
            if (behaviorPanel != null && behaviorPanel.gameObject.activeInHierarchy && _observer != null)
            {
                UpdateBehaviorSliders();
            }
        }

        public void ShowTitleScreen()
        {
            SetAllScreensOff();
            if (titleScreen) titleScreen.SetActive(true);
        }

        public void ShowGameScreen()
        {
            SetAllScreensOff();
            if (gameScreen) gameScreen.SetActive(true);
        }

        public void ShowGameOverScreen(int wave, string dominantTrait)
        {
            SetAllScreensOff();
            if (gameOverScreen) gameOverScreen.SetActive(true);
            if (gameOverWaveText) gameOverWaveText.text = $"Vague atteinte: {wave}";
            if (gameOverTraitText) gameOverTraitText.text = $"Style: {dominantTrait}";
            if (gameOverEssenceText) gameOverEssenceText.text = $"Essence: +{wave * 10}";

            if (gameOverSound && audioSource) audioSource.PlayOneShot(gameOverSound);
        }

        public void ShowVictoryScreen(string dominantTrait)
        {
            SetAllScreensOff();
            if (victoryScreen) victoryScreen.SetActive(true);
            if (victoryTraitText) victoryTraitText.text = $"Style dominant: {dominantTrait}";
            if (victoryEssenceText) victoryEssenceText.text = "Essence: +100";
        }

        public void ShowUpgradeShop()
        {
            SetAllScreensOff();
            if (upgradeShopScreen) upgradeShopScreen.SetActive(true);
            PopulateUpgradeShop();
        }

        public void UpdateUI(float health, float maxHealth, float resources, int wave)
        {
            if (healthBar) healthBar.fillAmount = health / maxHealth;
            if (healthText) healthText.text = $"{Mathf.CeilToInt(health)}/{Mathf.CeilToInt(maxHealth)}";
            if (resourcesText) resourcesText.text = $"💰 {resources:F0}";
            if (waveText) waveText.text = $"Vague {wave}";
        }

        public void ShowWaveStart(int wave)
        {
            if (waveAnnounceText) waveAnnounceText.text = $"VAGUE {wave}";
            if (waveAnnounceAnimator) waveAnnounceAnimator.SetTrigger("Show");
            if (waveStartSound && audioSource) audioSource.PlayOneShot(waveStartSound);
        }

        public void ShowThinkingAnimation(bool show)
        {
            if (thinkingIndicator) thinkingIndicator.SetActive(show);
        }

        public void ShowUnlockMessage(string message)
        {
            if (thinkingText) thinkingText.text = message;
        }

        public void ShowUnlockCard(bool show)
        {
            if (unlockCard) unlockCard.SetActive(show);
        }

        public void ShowUnlockCard(string narration, LlmUnlock unlock)
        {
            if (unlockCard)
            {
                unlockCard.SetActive(true);
                if (unlockNarrationText) unlockNarrationText.text = narration;
                if (unlockNameText) unlockNameText.text = $"✨ {unlock.unlockName} ✨";
                if (unlockDescriptionText) unlockDescriptionText.text = unlock.unlockDescription;
                if (unlockBehaviorText) unlockBehaviorText.text = $"🧠 {unlock.behaviorLabel}";

                if (unlockAnimator) unlockAnimator.SetTrigger("Reveal");
                if (unlockSound && audioSource) audioSource.PlayOneShot(unlockSound);
            }
        }

        public void ToggleBehaviorPanel()
        {
            if (behaviorPanel) behaviorPanel.gameObject.SetActive(!behaviorPanel.gameObject.activeSelf);
        }

        public void OnClickSound()
        {
            if (clickSound && audioSource) audioSource.PlayOneShot(clickSound);
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

        private void SetAllScreensOff()
        {
            if (titleScreen) titleScreen.SetActive(false);
            if (gameScreen) gameScreen.SetActive(false);
            if (gameOverScreen) gameOverScreen.SetActive(false);
            if (victoryScreen) victoryScreen.SetActive(false);
            if (upgradeShopScreen) upgradeShopScreen.SetActive(false);
        }

        private void UpdateBehaviorSliders()
        {
            if (_observer == null || behaviorSliders == null) return;
            var profile = _observer.GetProfile();
            if (behaviorSliders.Length > 0) behaviorSliders[0].value = profile.Aggression;
            if (behaviorSliders.Length > 1) behaviorSliders[1].value = profile.Caution;
            if (behaviorSliders.Length > 2) behaviorSliders[2].value = profile.Greed;
            if (behaviorSliders.Length > 3) behaviorSliders[3].value = profile.Curiosity;
            if (behaviorSliders.Length > 4) behaviorSliders[4].value = profile.Patience;
            if (behaviorSliders.Length > 5) behaviorSliders[5].value = profile.Reactivity;
            if (behaviorSliders.Length > 6) behaviorSliders[6].value = profile.Altruism;
            if (behaviorSliders.Length > 7) behaviorSliders[7].value = profile.RiskTolerance;
        }

        private void PopulateUpgradeShop()
        {
            // Clear existing items
            foreach (Transform child in upgradeContainer)
                Destroy(child.gameObject);

            if (_rogueliteManager == null) return;

            if (essenceTotalText)
                essenceTotalText.text = $"Essence: {_rogueliteManager.Essence}";

            var available = _rogueliteManager.GetAvailableUpgrades();
            foreach (var ug in available)
            {
                var item = Instantiate(upgradeItemPrefab, upgradeContainer);
                var texts = item.GetComponentsInChildren<TMP_Text>();
                if (texts.Length > 0) texts[0].text = ug.name;
                if (texts.Length > 1) texts[1].text = ug.description;

                var buyBtn = item.GetComponentInChildren<Button>();
                if (buyBtn)
                {
                    string capturedId = ug.id;
                    buyBtn.onClick.AddListener(() =>
                    {
                        if (_rogueliteManager.PurchaseUpgrade(capturedId))
                        {
                            PopulateUpgradeShop(); // Refresh
                        }
                    });
                    var btnText = buyBtn.GetComponentInChildren<TMP_Text>();
                    if (btnText) btnText.text = $"Acheter ({ug.cost} essence)";
                }
            }
        }
    }
}
