using UnityEngine;
using UnityEngine.UI;

namespace Shared.UI
{
    public class PaginationDot : MonoBehaviour
    {
        public Button dotButton;
        public Image dotImage;
        public Sprite activeSprite;
        public Sprite inactiveSprite;
        public void SetActive(bool isActive)
        {
            if (dotImage != null)
            {
                dotImage.sprite = isActive ? activeSprite : inactiveSprite;
            }
        }
    }
}