# 🎮 ProjectLM — POC Mobile

**LLM-powered Clicker/Roguelite pour Android — avec inference IA locale sur le téléphone**

> Un jeu où une IA (Gemma 4 E2B) observe ta façon de cliquer et débloque des compétences uniques adaptées à ton style. Aucune méta possible — chaque partie est différente parce que *toi* tu es différent.

---

## 🧠 Concept

Mélange de **clicker** (Cookie Clicker) et de **roguelite** :
- Tape sur différents nœuds (Attaque, Mine, Défense, Soin, Exploration)
- L'IA analyse tes habitudes de clic en temps réel
- Toutes les 2 vagues, le LLM local génère une compétence/un chemin adapté à ton comportement
- Entre les runs, dépense ton Essence pour des améliorations permanentes

### Comportements détectés par l'IA

| Axe | Si tu clics surtout sur... | Compétence débloquée |
|-----|---------------------------|---------------------|
| **Berserker** | Attaque rapidement | Dégâts x2 mais perte de vie |
| **Gardien** | Défense, Soin | Bouclier protecteur |
| **Explorateur** | Exploration | Chemins cachés, bonus |
| **Collectionneur** | Mine en rythme | Ressources triplées |
| **Téméraire** | Risque à basse vie | Pacte sanglant (dégâts x5 sous 30% PV) |

---

## 📱 Stack technique

| Composant | Technologie |
|-----------|-----------|
| **Moteur** | Unity 6000.1.11f1 (Unity 6) |
| **Ciblage** | Android ARM64 (Pixel 7 Pro minimum) |
| **LLM** | **Gemma 4 E2B QAT Mobile** (1.1 GB, text-only: 0.84 GB) via LLamaSharp |
| **Mobile UI** | Canvas + TextMeshPro + New Input System |
| **Rendu** | Universal Render Pipeline (URP) |
| **Sauvegarde** | PlayerPrefs (persistance roguelite) |
| **Inférence** | LLamaSharp 0.27.0 + llama.cpp (compilé ARM64 via NDK) |

### Pourquoi Gemma 4 E2B ?

Google a sorti **Gemma 4** en avril 2026 avec des modèles spécialement conçus pour le mobile :
- E2B (Effective 2B) en **QAT Mobile** = seulement **1.1 GB** de RAM
- Version **text-only** = **0.84 GB**
- Licence **Apache 2.0**
- Quantization-aware training : qualité préservée même en 4-bit
- Parfait pour un Pixel 7 Pro (12 GB RAM, Tensor G2)

---

## 🏗️ Architecture

```
Projet Unity
├── Assets/Scripts/
│   ├── Core/
│   │   ├── BehaviorEnums.cs      — Définition des axes comportementaux
│   │   └── BehaviorObserver.cs    — Observe, tracke et résume le comportement
│   ├── Game/
│   │   ├── GameManager.cs         — Boucle principale du jeu
│   │   ├── ClickNode.cs           — Nœud cliquable (IPointerClickHandler)
│   │   ├── WaveSystem.cs          — Vagues d'ennemis, timer, dégâts
│   │   ├── NodeSpawner.cs         — Génère les nœuds cliquables
│   │   ├── SkillUnlocker.cs       — Applique les déblocages LLM
│   │   └── RogueliteManager.cs    — Progression entre les runs
│   ├── LLM/
│   │   ├── ILLMBridge.cs          — Interface LLM (abstraction)
│   │   ├── LLamaSharpBridge.cs    — Bridge Gemma 4 via LLamaSharp
│   │   ├── DummyLLMBridge.cs      — Bridge factice (test PC)
│   │   └── LLMFactory.cs          — Factory (Android → real, PC → dummy)
│   └── UI/
│       └── UIManager.cs           — Tous les écrans et HUD
├── Assets/StreamingAssets/models/ — Emplacement du modèle GGUF
└── docs/Build_Android.md          — Guide de build APK
```

### Flux de données

```
Joueur clique → ClickNode → BehaviorObserver → BehaviorProfile
                                                      ↓
                                               LLMBridge (Gemma 4)
                                                      ↓
    SkillUnlocker ← LlmUnlock (JSON) ← LLM analyse le comportement
         ↓
    Nouvelle compétence ! → UI affiche → Gameplay modifié
```

---

## 🚀 Build Android (résumé)

### Prérequis
- Unity 6000.1.11f1
- Android SDK + NDK (via Unity Hub)
- Modèle Gemma 4 E2B QAT Mobile GGUF (~1.1 GB)

### Étapes rapides

1. **Ouvrir le projet** dans Unity
2. **Installer LLamaSharp** via NuGetForUnity (ou Assets/Packages/)
3. **Compiler llama.cpp pour Android ARM64** (script fourni)
4. **Télécharger le modèle** Gemma 4 E2B QAT Mobile GGUF dans `Assets/StreamingAssets/models/`
5. **Build Settings** → Switch Platform → Android
6. **Build** → APK

> 🔧 Voir [docs/Build_Android.md](docs/Build_Android.md) pour le guide complet

---

## 🎮 Gameplay

### Dans une partie
1. **5 nœuds** cliquables : ⚔️ Attaque, ⛏️ Mine, 🛡️ Défense, ✨ Exploration, 💚 Soin
2. **Vagues d'ennemis** de 30 secondes : clique sur Attaque pour les vaincre
3. **Timer** : si le temps expire, tu prends des dégâts
4. **Toutes les 2 vagues** : l'IA analyse ton comportement et débloque une compétence
5. **8 vagues** par run → Victoire ou Défaite

### Entre les runs
- Gagne de l'**Essence** selon ta performance
- Achète des **améliorations permanentes** (ressources bonus, vie max, etc.)
- Chaque run est unique : le LLM adapte les déblocages à TON style

---

## 📄 License

Apache 2.0 — ouvert à tous
