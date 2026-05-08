using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class LevelSelector : MonoBehaviour
    {
        [SerializeField] private List<LevelData> levelDatas;
        [SerializeField] private LevelSelectorElement levelPrefab;
        [SerializeField] private Transform contentParent;

        private void Awake()
        {
            foreach (var levelData in levelDatas)
            {
            Instantiate(levelPrefab, contentParent);    
            }
        }
    }
    [System.Serializable]
    public class LevelData
    {
        public Sprite levelPreview;
    }
}