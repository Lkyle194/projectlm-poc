namespace ProjectLM.Core
{
    /// <summary>
    /// All behavioral axes tracked by the observer.
    /// Each represents an orthogonal dimension of playstyle.
    /// </summary>
    public enum BehaviorAxis
    {
        /// Aggressive: clicks on Attack, high frequency
        Aggression,
        /// Cautious: clicks on Defend/Heal, defensive reactions
        Caution,
        /// Greedy: clicks on Mine, hoards resources
        Greed,
        /// Curious: clicks on Explore, varied targeting
        Curiosity,
        /// Patient: steady rhythm, planned upgrades
        Patience,
        /// Reactive: responds to threats quickly
        Reactivity,
        /// Social: prefers synergy upgrades over selfish
        Altruism,
        /// Risky: plays with low HP, takes chances
        RiskTolerance
    }

    /// <summary>
    /// Snapshot of a player's behavioral profile at a given time.
    /// Each axis is 0.0–1.0 (normalized).
    /// </summary>
    [System.Serializable]
    public struct BehaviorProfile
    {
        public float Aggression;
        public float Caution;
        public float Greed;
        public float Curiosity;
        public float Patience;
        public float Reactivity;
        public float Altruism;
        public float RiskTolerance;

        /// <summary>
        /// Returns the dominant axis name.
        /// </summary>
        public string DominantTrait()
        {
            float max = 0;
            string trait = "balanced";
            if (Aggression > max) { max = Aggression; trait = "aggressif"; }
            if (Caution > max) { max = Caution; trait = "prudent"; }
            if (Greed > max) { max = Greed; trait = "avare"; }
            if (Curiosity > max) { max = Curiosity; trait = "curieux"; }
            if (Patience > max) { max = Patience; trait = "patient"; }
            if (Reactivity > max) { max = Reactivity; trait = "réactif"; }
            if (Altruism > max) { max = Altruism; trait = "altruiste"; }
            if (RiskTolerance > max) { trait = "téméraire"; }
            return trait;
        }

        public override string ToString()
        {
            return $"A:{Aggression:F2} C:{Caution:F2} G:{Greed:F2} " +
                   $"Cu:{Curiosity:F2} P:{Patience:F2} R:{Reactivity:F2} " +
                   $"Al:{Altruism:F2} Ri:{RiskTolerance:F2}";
        }
    }

    /// <summary>
    /// Describes a single click action recorded by the observer.
    /// </summary>
    [System.Serializable]
    public struct ClickEvent
    {
        public string nodeId;
        public float gameTime;
        public float playerHealthPercent;
        public int waveNumber;
        public float timeSinceLastClick;
    }
}
