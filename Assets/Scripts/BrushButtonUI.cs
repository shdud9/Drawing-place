using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class BrushButtonUI : MonoBehaviour
    {
        [SerializeField] private ColoringManager coloringManager;
        [SerializeField] private Button brushButton;


        private void Awake()
        {
            brushButton.onClick.AddListener(SelectBrush);    
        }

        private void SelectBrush()
        {
            coloringManager.currentTool = ColoringManager.Tool.Brush;
        }
    }
}