# 🔧 Guide de build Android pour ProjectLM POC

Ce guide détaille comment compiler le projet en APK et faire tourner le LLM (Gemma 4 E2B) directement sur Android.

---

## 1. Prérequis

| Outil | Version | Rôle |
|-------|---------|------|
| **Unity** | 6000.1.11f1 | Moteur de jeu |
| **Android SDK** | 34+ | Compilation Android |
| **Android NDK** | r27+ | Compilation des .so natives |
| **CMake** | 3.22+ | Build llama.cpp |
| **Python 3** | 3.10+ | Scripts de conversion |
| **Espace disque** | 10 GB+ | Modèle + build |

### Installation SDK/NDK via Unity Hub
1. Ouvrir Unity Hub → Installs → Unity 6000.1.11f1
2. cliquer sur "Add modules" → cocher :
   - ✅ **Android Build Support**
   - ✅ **Android SDK & NDK Tools**
   - ✅ **OpenJDK**

---

## 2. Compilation de llama.cpp pour Android ARM64

### Méthode A : Script automatisé (recommandé)

```bash
# Depuis la racine du projet
chmod +x scripts/build-llama-android.sh
./scripts/build-llama-android.sh
```

Cela génère `Assets/Plugins/Android/libs/arm64-v8a/libllama.so`.

### Méthode B : Manuelle

```bash
git clone https://github.com/ggml-org/llama.cpp
cd llama.cpp

# Config NDK
export NDK=$ANDROID_NDK_HOME  # Définir le chemin de votre NDK
export API=34
export TOOLCHAIN=$NDK/toolchains/llvm/prebuilt/linux-x86_64
export TARGET=armv8a-linux-androideabi$API
export CC=$TOOLCHAIN/bin/$TARGET-clang
export CXX=$TOOLCHAIN/bin/$TARGET-clang++

# Compilation
mkdir build-android && cd build-android
cmake .. \
  -DCMAKE_TOOLCHAIN_FILE=$NDK/build/cmake/android.toolchain.cmake \
  -DANDROID_ABI=arm64-v8a \
  -DANDROID_PLATFORM=android-34 \
  -DLLAMA_STATIC=ON \
  -DLLAMA_NATIVE=OFF
make -j4

# Copier le .so dans le projet Unity
cp libllama.a ../../Assets/Plugins/Android/libs/arm64-v8a/
```

### Méthode C : Utiliser les binaires précompilés de LLamaSharp

LLamaSharp 0.27.0 inclut des backends CPU. Sur Android, il faut :
1. Installer le package NuGet `LLamaSharp.Backend.Cpu` via NuGetForUnity
2. Copier les .so depuis le package vers `Assets/Plugins/Android/`
3. Renommer/copier le backend correspondant

---

## 3. Installation de LLamaSharp dans Unity

NuGetForUnity est déjà configuré dans le projet. Dans Unity :

1. **Window** → **NuGetForUnity** → **Manage Packages**
2. Rechercher **LLamaSharp** → Installer **v0.27.0**
3. **Important** : Cocher "Include DLLs for Android" dans les options du package
4. Ajouter aussi **LLamaSharp.Backend.Cpu**

### Alternative : Installation manuelle
Si NuGetForUnity ne fonctionne pas sur Android :
1. Télécharger LLamaSharp depuis [NuGet](https://www.nuget.org/packages/LLamaSharp)
2. Extraire les DLLs dans `Assets/Plugins/`
3. Télécharger `LLamaSharp.Backend.Cpu` et extraire les .so Android dans `Assets/Plugins/Android/libs/arm64-v8a/`

---

## 4. Téléchargement du modèle Gemma 4

### Où trouver le modèle

Gemma 4 E2B QAT Mobile — version **GGUF quantifié Q4** :

| Source | Format | Taille | URL |
|--------|--------|--------|-----|
| Unsloth (recommandé) | GGUF Q4_0 | ~1.1 GB | [HuggingFace](https://huggingface.co/unsloth/gemma-4-E2B-it-qat-mobile-GGUF) |
| Google (officiel) | LiteRT-LM | ~1.1 GB | [HuggingFace](https://huggingface.co/litert-community/gemma-4-E2B-it-litert-lm) |
| Google (text-only) | Mobile Transformers | **~0.84 GB** | [HuggingFace](https://huggingface.co/google/gemma-4-E2B-it-qat-mobile-transformers) |

**Recommandé pour Android :** La version Unsloth GGUF — compatible avec LLamaSharp.

### Installation dans le projet

```bash
# Créer le dossier models
mkdir -p Assets/StreamingAssets/models/

# Télécharger le modèle (GGUF)
wget https://huggingface.co/unsloth/gemma-4-E2B-it-qat-mobile-GGUF/resolve/main/gemma-4-E2B-it-qat-mobile-Q4_0.gguf \
  -O Assets/StreamingAssets/models/gemma-4-E2B-it-qat-mobile-Q4_0.gguf
```

> ⚠️ Le modèle fait ~1.1 GB. Vérifie que ton APK pourra contenir ce poids.
> Option : proposer le téléchargement au premier lancement via un écran de chargement.

---

## 5. Configuration Unity pour Android

### Player Settings

| Setting | Valeur |
|---------|--------|
| **Scripting Backend** | IL2CPP |
| **Target Architectures** | ARM64 ✅ |
| **Min API Level** | 34 (Android 14) |
| **Target API Level** | 34 |
| **Internet Access** | Not required (tout est local) |
| **Graphics API** | Vulkan |
| **Multithreaded Rendering** | ✅ |
| **Static Batching** | ✅ |
| **Texture Compression** | ASTC |

### Optimization Settings

| Setting | Valeur |
|---------|--------|
| **Managed Stripping Level** | Medium |
| **Enable Internal Profiler** | ❌ |
| **Strip Engine Code** | ✅ |
| **Vertex Compression** | Everything |

Note : Si le modèle LLM est inclus dans l'APK, la taille sera ~1.5 GB. Pour réduire :
- Utiliser **Android App Bundle** (Google Play)
- Ou proposer le téléchargement du modèle au premier lancement via un `UnityWebRequest`

---

## 6. Build APK

### Via l'éditeur Unity
1. **File** → **Build Settings** → **Android**
2. Cliquer **Switch Platform** (si pas déjà fait)
3. Clic droit sur `Assets/Scenes/SampleScene` → **Include in Build**
4. **Player Settings** → vérifier les settings ci-dessus
5. **Build** → choisir `./Builds/projectlm-poc.apk`
6. Attendre ~5-10 minutes

### Via CLI (headless)
```bash
/path/to/Unity6000.1.11f1/Editor/Unity \
  -batchmode \
  -projectPath . \
  -buildTarget Android \
  -outputPath ./Builds/projectlm-poc.apk \
  -quit
```

---

## 7. Test sur Pixel 7 Pro

```bash
# Installer l'APK
adb install -r Builds/projectlm-poc.apk

# Lancer
adb shell am start -n com.ProjectLM.POC/.MainActivity

# Voir les logs LLM
adb logcat -s Unity LLamaSharp
```

### Première exécution
1. L'app charge le modèle (~5-15 secondes selon CPU/GPU)
2. Écran titre → "Nouvelle Partie"
3. Le jeu commence avec la vague 1
4. Le LLM n'est appelé qu'aux moments de déblocage

### Performances attendues (Pixel 7 Pro)
- **Temps de chargement modèle** : 8-15 secondes
- **Inférence LLM (par déblocage)** : 2-5 secondes
- **FPS en jeu** : 60 FPS (constant)
- **Consommation RAM** : ~2.5 GB (dont ~1.1 GB pour le modèle)
- **Batterie** : Usage modéré (LLM n'est pas H24)

---

## 8. Dépannage

### "LLamaSharp native library not found"
→ Vérifier les .so dans `Assets/Plugins/Android/libs/arm64-v8a/`
→ Vérifier que IL2CPP est sélectionné

### "Model load failed"
→ Vérifier que le GGUF est bien dans `StreamingAssets/models/`
→ Vérifier le chemin dans GameManager.cs (modelFileName)
→ Tester avec DummyLLMBridge d'abord (useLLM = false)

### "Out of memory"
→ Pixel 7 Pro a 12 GB RAM, ça devrait passer
→ Vider les apps en arrière-plan
→ Essayer le modèle text-only (0.84 GB au lieu de 1.1 GB)

### "Build takes too long"
→ Première build avec IL2CPP = lente. Les builds suivantes sont plus rapides.
→ Utiliser "Development Build" pour les tests (désactiver pour release)

---

## 9. Architecture des Plugins Android

```
Assets/Plugins/Android/
├── libs/
│   └── arm64-v8a/
│       ├── libllama.so          — llama.cpp compilé pour ARM64
│       └── libllamaSharp.so     — Wrapper LLamaSharp
├── AndroidManifest.xml          — Personnalisation manifeste
└── mainTemplate.gradle          — Configuration Gradle
```

Pour LLamaSharp, le backend CPU suffit (pas de GPU CUDA sur mobile).
Le Pixel 7 Pro peut utiliser le backend Vulkan pour accélérer l'inférence.
