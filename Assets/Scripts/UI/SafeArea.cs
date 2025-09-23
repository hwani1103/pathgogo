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

        // ���� ������ ����ȭ�� ��ǥ�� ��ȯ
        minAnchor = safeArea.position;
        maxAnchor = minAnchor + safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;
        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        // RectTransform�� ����
        rectTransform.anchorMin = minAnchor;
        rectTransform.anchorMax = maxAnchor;
    }

    void Update()
    {
        // �ǽð����� �������� ���� ���� (��� ȸ�� ��)
        if (safeArea != Screen.safeArea)
        {
            Start(); // �ٽ� ���
        }
    }
}