using System;
using UnityEngine;
using UnityEngine.UI;

public class BrushColor : MonoBehaviour
{
    [SerializeField] private Button openColorPickerButton;
    [SerializeField] private Button closeColorPickerButton;
    [SerializeField] private GameObject colorPickerPopUp;
    [SerializeField] private ColoringManager coloringManager;
    [SerializeField] private FlexibleColorPicker colorPicker;
    [SerializeField] private Image selectedColorImage;
    
    
    
    

    private void Awake()
    {
        openColorPickerButton.onClick.AddListener(OpenColorPicker);
        closeColorPickerButton.onClick.AddListener(CloseColorPicker);
        colorPicker.onColorChange.AddListener(OnColorChanged);
        selectedColorImage.color = colorPicker.color;
    }

    private void OnColorChanged(Color arg0)
    {
        selectedColorImage.color = arg0;
    }

    private void CloseColorPicker()
    { 
        colorPickerPopUp.SetActive(false);
        coloringManager.isDrawingEnabled = true;
    }

    private void OpenColorPicker()
    {
    colorPickerPopUp.SetActive(true);   
    coloringManager.isDrawingEnabled = false;
    }
    
    
}