using UnityEngine;

namespace ProjectLM.LLM
{
    /// <summary>
    /// Factory that creates the appropriate LLM bridge based on platform.
    /// - Android: LLamaSharpBridge (Gemma 4 via LLamaSharp + llama.cpp)
    /// - Editor / other: DummyLLMBridge (deterministic, no model needed)
    /// </summary>
    public static class LLMFactory
    {
        public static ILLMBridge CreateBridge()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("[LLMFactory] Android detected — creating LLamaSharpBridge");
            return new LLamaSharpBridge();
#else
            Debug.Log("[LLMFactory] Editor/PC detected — creating DummyLLMBridge");
            return new DummyLLMBridge();
#endif
        }
    }
}
