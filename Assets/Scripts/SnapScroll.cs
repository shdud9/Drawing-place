using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shared.UI
{
    public enum ScrollDirection
    {
        Horizontal,
        Vertical
    }

    public class SnapScroll : MonoBehaviour,
        IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [Header("Mode")]
        public ScrollDirection direction = ScrollDirection.Horizontal;
        public bool snapEnabled = true;  

        [Header("References")]
        public ScrollRect scrollRect;
        public RectTransform content;

        [Header("Buttons")]
        public Button nextButton;
        public Button prevButton;

        [Header("Pagination dots")]
        public PaginationDot[] dots;

        [Header("Settings")]
        public float snapSpeed = 12f;
        public float minSwipeVelocity = 800f;
        public float snapThresholdPercent = 0.4f;

        [Header("Start settings")]
        public int startPage = 0;

        public int CurrentPage => currentPage;

        public event Action<int> OnPageChanged;

        int pageCount;
        int currentPage = 0;

        bool dragging = false;
        Vector2 lastDragPos;
        float lastDragTime;
        float velocity;

        Coroutine snapRoutine;

        void Start()
        {
            // Якщо треба авто-лейаут — можеш викликати
            RebuildResponsiveLayout(0); 
            // RebuildResponsiveLayout(0);

            pageCount = content.childCount;

            startPage = Mathf.Clamp(startPage, 0, pageCount - 1);
            currentPage = startPage;

            SetNormalizedPosToPage(startPage);

            if (nextButton) nextButton.onClick.AddListener(() => GoToPage(currentPage + 1));
            if (prevButton) prevButton.onClick.AddListener(() => GoToPage(currentPage - 1));

            SetupDots();
        }

        // ------------------------- RESPONSIVE LAYOUT -------------------------
        // Якщо хочеш, щоб елементи займали весь viewport по одній сторінці:
        public void RebuildResponsiveLayout(float spacing)
        {
            int count = content.childCount;
            if (count == 0) return;

            var viewportRect = (RectTransform)scrollRect.viewport;
            float viewportWidth = viewportRect.rect.width;
            float viewportHeight = viewportRect.rect.height;

            if (direction == ScrollDirection.Horizontal)
            {
                float x = 0f;

                for (int i = 0; i < count; i++)
                {
                    RectTransform rt = content.GetChild(i) as RectTransform;

                    // Сторінка на всю висоту viewport
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, viewportHeight);

                    float width = rt.rect.width;

                    rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);

                    x += width + spacing;
                }

                content.sizeDelta = new Vector2(x - spacing, viewportHeight);
            }
            else // Vertical
            {
                float y = 0f;

                for (int i = 0; i < count; i++)
                {
                    RectTransform rt = content.GetChild(i) as RectTransform;

                    // Сторінка на всю ширину viewport
                    rt.sizeDelta = new Vector2(viewportWidth, rt.sizeDelta.y);

                    float height = rt.rect.height;

                    // Вгору-вниз (Unity Y вниз негативний у anchoredPosition)
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -y);

                    y += height + spacing;
                }

                content.sizeDelta = new Vector2(viewportWidth, y - spacing);
            }

            pageCount = count;
            SetupDots();
        }

        // ------------------------- INIT PAGE POS -------------------------
        void SetNormalizedPosToPage(int index)
        {
            if (pageCount <= 1)
                return;

            float pos = (float)index / (pageCount - 1);
            SetNormalizedPosition(pos);
        }

        float GetNormalizedPosition()
        {
            return direction == ScrollDirection.Horizontal
                ? scrollRect.horizontalNormalizedPosition
                : scrollRect.verticalNormalizedPosition;
        }

        void SetNormalizedPosition(float value)
        {
            if (direction == ScrollDirection.Horizontal)
                scrollRect.horizontalNormalizedPosition = value;
            else
                scrollRect.verticalNormalizedPosition = value;
        }

        // ------------------------- DRAG HANDLERS -------------------------
        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            lastDragPos = eventData.position;
            lastDragTime = Time.unscaledTime;
            velocity = 0;

            if (snapRoutine != null)
                StopCoroutine(snapRoutine);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // тут ми тільки рахуємо velocity, сам ScrollRect рухається своєю логікою
            float dt = Time.unscaledTime - lastDragTime;
            if (dt > 0.001f)
            {
                float d = (direction == ScrollDirection.Horizontal)
                    ? (eventData.position.x - lastDragPos.x)
                    : (eventData.position.y - lastDragPos.y);

                velocity = d / dt;

                lastDragPos = eventData.position;
                lastDragTime = Time.unscaledTime;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;

            if (!snapEnabled || pageCount <= 1)
                return;

            float normalized = GetNormalizedPosition();
            float targetPageFromPos = normalized * (pageCount - 1);

            int predictedPage;

            // Velocity swipe → короткий, але швидкий свайп
            if (Mathf.Abs(velocity) > minSwipeVelocity)
            {
                if (direction == ScrollDirection.Horizontal)
                {
                    predictedPage = (velocity < 0) ? currentPage + 1 : currentPage - 1;
                }
                else // Vertical (Y вниз у екрані)
                {
                    // якщо тягнемо вгору (velocity > 0) → переходимо на наступну сторінку
                    predictedPage = (velocity > 0) ? currentPage + 1 : currentPage - 1;
                }
            }
            else
            {
                // Звичайний snap по позиції (як було у тебе)
                float fractional = targetPageFromPos - Mathf.Floor(targetPageFromPos);
                if (fractional > snapThresholdPercent)
                    predictedPage = Mathf.FloorToInt(targetPageFromPos) + 1;
                else
                    predictedPage = Mathf.FloorToInt(targetPageFromPos);
            }

            GoToPage(predictedPage);
        }

        // ------------------------- GO TO PAGE -------------------------
        public void GoToPage(int index)
        {
            index = Mathf.Clamp(index, 0, pageCount - 1);
            currentPage = index;

            float targetPos = (float)index / (pageCount - 1);

            if (snapRoutine != null)
                StopCoroutine(snapRoutine);

            snapRoutine = StartCoroutine(SmoothSnap(targetPos));
            OnPageChanged?.Invoke(currentPage);
            UpdateDots();
        }

        IEnumerator SmoothSnap(float target)
        {
            while (Mathf.Abs(GetNormalizedPosition() - target) > 0.0001f)
            {
                float current = GetNormalizedPosition();
                float next = Mathf.Lerp(current, target, Time.unscaledDeltaTime * snapSpeed);
                SetNormalizedPosition(next);
                yield return null;
            }

            SetNormalizedPosition(target);
        }

        // ------------------------- DOTS -------------------------
        void SetupDots()
        {
            if (dots == null || dots.Length != pageCount)
                return;

            for (int i = 0; i < dots.Length; i++)
            {
                int index = i;
                dots[i].dotButton.onClick.RemoveAllListeners();
                dots[i].dotButton.onClick.AddListener(() => GoToPage(index));
            }

            UpdateDots();
        }

        void UpdateDots()
        {
            if (dots == null) return;

            for (int i = 0; i < dots.Length; i++)
            {
                bool active = (i == currentPage);
                dots[i].dotButton.interactable = !active;
                dots[i].SetActive(active);
            }
        }
    }
}
