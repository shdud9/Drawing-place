using System;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace DefaultNamespace
{
    public class BrushSizeButton : MonoBehaviour
    {
        [SerializeField] private ColoringManager coloringManager;
        
        
        public void increaseBrushSize()
        {
            if (coloringManager.brushSize < 100)
            {
                coloringManager.brushSize += 5;
            }
            
        }

        public void decreaseBrushSize()
        {
            if (coloringManager.brushSize > 10)
            {
                coloringManager.brushSize -= 5;
            } 
        }
        
    }
}