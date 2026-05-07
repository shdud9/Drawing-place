using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ColoringManager : MonoBehaviour
{
    [SerializeField]
    private FlexibleColorPicker colorPicker;

    public bool isDrawingEnabled = true;

    public RawImage rawImage;
    public Texture2D sourceTexture;

    [Range(1, 1000)]
    public int brushSize = 10;

    public enum Tool { Brush, Fill, Eraser }
    public Tool currentTool = Tool.Brush;

    private Texture2D drawingTexture;
    private bool[,] allowedZone;
    private bool[,] isContour;
    private bool zoneActive = false;

    // Flag to track if the current stroke started on the drawing canvas
    private bool isValidStroke = false;

    void Start()
    {
        // Initialize working texture
        drawingTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        drawingTexture.SetPixels(sourceTexture.GetPixels());
        drawingTexture.Apply();

        rawImage.texture = drawingTexture;
        rawImage.rectTransform.sizeDelta = new Vector2(drawingTexture.width, drawingTexture.height);

        // Create contour mask
        int width = drawingTexture.width;
        int height = drawingTexture.height;
        isContour = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color c = sourceTexture.GetPixel(x, y);
                isContour[x, y] = c.r < 0.1f && c.g < 0.1f && c.b < 0.1f;
            }
        }
    }

    private void Update()
    {
        if (!isDrawingEnabled) return;

        Vector2 pointerPos = Vector2.zero;
        bool isPointerDown = false;
        bool isPointerPressed = false;

#if UNITY_EDITOR || UNITY_STANDALONE
        isPointerDown = Input.GetMouseButtonDown(0);
        isPointerPressed = Input.GetMouseButton(0);
        pointerPos = Input.mousePosition;
#elif UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            isPointerDown = touch.phase == TouchPhase.Began;
            isPointerPressed = (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Began);
            pointerPos = touch.position;
        }
#endif

        if (isPointerDown)
        {
            // Check if the click started specifically on the RawImage
            isValidStroke = IsPointerOverCanvas(pointerPos);
        }

        if (isPointerPressed && isValidStroke)
        {
            Vector2Int pxpy = ScreenToTexture(pointerPos);
            int px = pxpy.x;
            int py = pxpy.y;

            if (!zoneActive && currentTool != Tool.Fill)
            {
                InitZone(px, py);
            }

            switch (currentTool)
            {
                case Tool.Brush:
                    Paint(px, py, colorPicker.color);
                    break;
                case Tool.Eraser:
                    Paint(px, py, Color.white);
                    break;
                case Tool.Fill:
                    Color targetColor = drawingTexture.GetPixel(px, py);
                    FloodFill(px, py, targetColor, colorPicker.color);
                    break;
            }
        }

        if (!isPointerPressed)
        {
            zoneActive = false;
            isValidStroke = false;
        }
    }

    /// <summary>
    /// Checks if the pointer is over the specific RawImage assigned to the canvas.
    /// </summary>
    private bool IsPointerOverCanvas(Vector2 screenPos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // If the first UI object hit is our RawImage, then it's a valid click
            if (result.gameObject == rawImage.gameObject)
            {
                return true;
            }
            
            // If we hit another UI object (like a button) before the RawImage, block it
            if (result.gameObject != rawImage.gameObject)
            {
                return false;
            }
        }

        return false;
    }

    Vector2Int ScreenToTexture(Vector2 screenPos)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform,
            screenPos,
            null,
            out localPoint
        );

        float x = (localPoint.x + rawImage.rectTransform.rect.width * 0.5f) * drawingTexture.width / rawImage.rectTransform.rect.width;
        float y = (localPoint.y + rawImage.rectTransform.rect.height * 0.5f) * drawingTexture.height / rawImage.rectTransform.rect.height;

        return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(x), 0, drawingTexture.width - 1),
                              Mathf.Clamp(Mathf.FloorToInt(y), 0, drawingTexture.height - 1));
    }

    void InitZone(int startX, int startY)
    {
        int width = drawingTexture.width;
        int height = drawingTexture.height;
        allowedZone = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            if (p.x < 0 || p.y < 0 || p.x >= width || p.y >= height) continue;
            if (allowedZone[p.x, p.y]) continue;
            if (isContour[p.x, p.y]) continue;

            allowedZone[p.x, p.y] = true;

            queue.Enqueue(new Vector2Int(p.x + 1, p.y));
            queue.Enqueue(new Vector2Int(p.x - 1, p.y));
            queue.Enqueue(new Vector2Int(p.x, p.y + 1));
            queue.Enqueue(new Vector2Int(p.x, p.y - 1));
        }

        zoneActive = true;
    }

    void Paint(int cx, int cy, Color color)
    {
        if (!zoneActive) return;

        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                if (x * x + y * y > brushSize * brushSize) continue;

                int px = cx + x;
                int py = cy + y;

                if (px < 0 || py < 0 || px >= drawingTexture.width || py >= drawingTexture.height) continue;
                if (!allowedZone[px, py]) continue;

                drawingTexture.SetPixel(px, py, color);
            }
        }
        drawingTexture.Apply();
    }

    bool ColorsAreClose(Color a, Color b, float tolerance = 0.15f)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return dr * dr + dg * dg + db * db < tolerance * tolerance;
    }

    void FloodFill(int x, int y, Color targetColor, Color replacementColor)
    {
        if (ColorsAreClose(targetColor, replacementColor)) return;

        int width = drawingTexture.width;
        int height = drawingTexture.height;
        Queue<Vector2Int> pixels = new Queue<Vector2Int>();
        pixels.Enqueue(new Vector2Int(x, y));

        while (pixels.Count > 0)
        {
            Vector2Int p = pixels.Dequeue();
            if (p.x < 0 || p.y < 0 || p.x >= width || p.y >= height) continue;

            Color current = drawingTexture.GetPixel(p.x, p.y);
            if (ColorsAreClose(current, targetColor) && !isContour[p.x, p.y])
            {
                drawingTexture.SetPixel(p.x, p.y, replacementColor);

                pixels.Enqueue(new Vector2Int(p.x + 1, p.y));
                pixels.Enqueue(new Vector2Int(p.x - 1, p.y));
                pixels.Enqueue(new Vector2Int(p.x, p.y + 1));
                pixels.Enqueue(new Vector2Int(p.x, p.y - 1));
            }
        }

        drawingTexture.Apply();
    }

    public void SaveTexture()
    {
        byte[] bytes = drawingTexture.EncodeToPNG();
        string path = Application.persistentDataPath + "/colored.png";
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log("Saved: " + path);
    }

    public void SetBrushTool() => currentTool = Tool.Brush;
    public void SetEraserTool() => currentTool = Tool.Eraser;
    public void SetFillTool() => currentTool = Tool.Fill;
    public void SetBrushSize(int size) => brushSize = size;
}