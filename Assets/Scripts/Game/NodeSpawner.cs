using System.Collections.Generic;
using UnityEngine;

namespace ProjectLM.Game
{
    /// <summary>
    /// Spawns and manages clickable nodes on the screen.
    /// Starts with basic nodes (attack, mine, defend) and
    /// adds new path nodes based on LLM unlocks.
    /// </summary>
    public class NodeSpawner : MonoBehaviour
    {
        [Header("Node Prefabs")]
        [SerializeField] private GameObject clickNodePrefab;
        [SerializeField] private Transform nodesContainer;

        [Header("Layout")]
        [SerializeField] private Vector2 gridStart = new Vector2(100, -200);
        [SerializeField] private float nodeSpacingX = 160f;
        [SerializeField] private float nodeSpacingY = 160f;
        [SerializeField] private int columns = 3;

        [Header("Default Nodes (wave 1)")]
        [SerializeField] private string[] basicNodeIds = { "attack", "mine", "defend", "heal", "explore" };

        [Header("Node Visual Config")]
        [SerializeField] private ClickNodeConfig[] nodeConfigs;

        private List<ClickNode> _activeNodes = new List<ClickNode>();
        private SkillUnlocker _skillUnlocker;

        [System.Serializable]
        public struct ClickNodeConfig
        {
            public string nodeId;
            public string displayName;
            public Color color;
            public float baseValue;
        }

        private void Start()
        {
            _skillUnlocker = FindAnyObjectByType<SkillUnlocker>();
        }

        /// <summary>
        /// Spawn the appropriate nodes for a given wave.
        /// Wave 1: basic nodes. Later waves: progressively add more.
        /// </summary>
        public void SpawnNodesForWave(int waveNumber)
        {
            // Only spawn on wave 1 (nodes persist between waves)
            if (waveNumber == 1)
            {
                ClearNodes();
                SpawnBasicNodes();
            }
        }

        private void SpawnBasicNodes()
        {
            if (clickNodePrefab == null || nodesContainer == null)
            {
                Debug.LogWarning("[NodeSpawner] Missing prefab or container reference");
                return;
            }

            for (int i = 0; i < basicNodeIds.Length; i++)
            {
                string id = basicNodeIds[i];
                var config = GetConfig(id);

                var go = Instantiate(clickNodePrefab, nodesContainer);
                go.name = $"Node_{id}";

                var rect = go.GetComponent<RectTransform>();
                if (rect)
                {
                    int row = i / columns;
                    int col = i % columns;
                    rect.anchoredPosition = new Vector2(
                        gridStart.x + col * nodeSpacingX,
                        gridStart.y + row * nodeSpacingY
                    );
                }

                var node = go.GetComponent<ClickNode>();
                if (node)
                {
                    // Set via reflection is a pain; just initialize via public access
                    _skillUnlocker?.RegisterNode(node);
                    _activeNodes.Add(node);
                }
                else
                {
                    Debug.LogError($"[NodeSpawner] Prefab missing ClickNode component!");
                }
            }
        }

        /// <summary>
        /// Spawn a new path node when the LLM unlocks a new path.
        /// </summary>
        public void SpawnPathNode(string pathName, string unlockName)
        {
            if (clickNodePrefab == null) return;

            var go = Instantiate(clickNodePrefab, nodesContainer);
            go.name = $"Node_{pathName}";

            // Place in next available grid slot
            int totalNodes = _activeNodes.Count;
            int row = totalNodes / columns;
            int col = totalNodes % columns;

            var rect = go.GetComponent<RectTransform>();
            if (rect)
            {
                rect.anchoredPosition = new Vector2(
                    gridStart.x + col * nodeSpacingX,
                    gridStart.y + row * nodeSpacingY
                );
            }

            var node = go.GetComponent<ClickNode>();
            if (node)
            {
                _skillUnlocker?.RegisterNode(node);
                _activeNodes.Add(node);
            }
        }

        private void ClearNodes()
        {
            foreach (var node in _activeNodes)
            {
                if (node && node.gameObject)
                    Destroy(node.gameObject);
            }
            _activeNodes.Clear();
        }

        private ClickNodeConfig GetConfig(string nodeId)
        {
            foreach (var config in nodeConfigs)
            {
                if (config.nodeId == nodeId)
                    return config;
            }
            // Default config
            return new ClickNodeConfig
            {
                nodeId = nodeId,
                displayName = nodeId,
                color = Color.gray,
                baseValue = 1f
            };
        }
    }
}
