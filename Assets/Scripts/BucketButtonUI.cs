using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class BucketButtonUI : MonoBehaviour
    {
        [SerializeField] private ColoringManager coloringManager;
        [SerializeField] private Button bucketButton;


        private void Awake()
        {
            bucketButton.onClick.AddListener(SelectBucket);    
        }

        private void SelectBucket()
        {
            coloringManager.currentTool = ColoringManager.Tool.Fill;
        }
    }
}