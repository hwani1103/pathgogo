using UnityEngine;
using UnityEngine.UI;

public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect safeArea;
    private Vector2 minAnchor;
    private Vector2 maxAnchor;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        safeArea = Screen.safeArea;

        // 안전 영역을 정규화된 좌표로 변환
        minAnchor = safeArea.position;
        maxAnchor = minAnchor + safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;
        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        // RectTransform에 적용
        rectTransform.anchorMin = minAnchor;
        rectTransform.anchorMax = maxAnchor;
    }

    void Update()
    {
        // 실시간으로 안전영역 변경 감지 (기기 회전 등)
        if (safeArea != Screen.safeArea)
        {
            Start(); // 다시 계산
        }
    }
}