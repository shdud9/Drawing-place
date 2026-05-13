using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private UIDocument mainMenuUI;
    [SerializeField] private UIDocument levelSelectorUI;
    
    private Button _startButton;

    private void Awake()
    {
        VisualElement root = mainMenuUI.rootVisualElement;
        _startButton = root.Query<Button>("PlayButton");
    }

    



}
