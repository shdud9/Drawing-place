using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class EraserButtonUI : MonoBehaviour
    {
        [SerializeField] private ColoringManager coloringManager;
        [SerializeField] private Button eraserButton;


        private void Awake()
        {
        eraserButton.onClick.AddListener(SelectEraser);    
        }

        private void SelectEraser()
        {
            coloringManager.currentTool = ColoringManager.Tool.Eraser;
        }
    }
}