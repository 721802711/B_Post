using UnityEngine;


public class CameraRayTest : MonoBehaviour
{
    public Vector2Int debugPointCount = new Vector2Int(5, 5); // 网格上的点数量
    public float pointRadius = 1.0f; // 圆球半径
    public Color pointColor = Color.white; // 圆球颜色
    // 箭头颜色
    public Color arrowColor = Color.white;


    private void OnDrawGizmos()
    {
        CameraRayTestFunction();
    }

    void CameraRayTestFunction()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Transform camT = cam.transform;

        // 计算投影平面的宽度和高度
        float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;

        // 投影平面的左下角（相对于摄像机的局部空间）
        Vector3 bottomLeftLocal = new Vector3(-planeWidth / 2, -planeHeight / 2, cam.nearClipPlane);

        // 在平面上绘制点网格
        for (int x = 0; x < debugPointCount.x; x++)
        {
            for (int y = 0; y < debugPointCount.y; y++)
            {
                float tx = x / (debugPointCount.x - 1f); // 0 = 平面左边缘, 1 = 右边缘
                float ty = y / (debugPointCount.y - 1f); // 0 = 底边缘, 1 = 顶边缘

                // 计算相机局部空间的点，然后转换到世界空间
                Vector3 pointLocal = bottomLeftLocal + new Vector3(planeWidth * tx, planeHeight * ty);
                Vector3 point = camT.position + camT.right * pointLocal.x + camT.up * pointLocal.y + camT.forward * pointLocal.z;

                Vector3 dir = (point - camT.position).normalized;


                // 可视化
                DrawPoint(point);
                DrawArrow(camT.position, dir);
            }
        }
    }

    void DrawPoint(Vector3 position)
    {
        Gizmos.color = pointColor;
        Gizmos.DrawSphere(position, pointRadius/100); // 在给定位置绘制一个圆球
    }

    void DrawArrow(Vector3 position, Vector3 direction)
    {
        Gizmos.color = arrowColor;

        float arrowHeadLength = 5.0f;
        float arrowHeadAngle = 25.0f;


        // 绘制箭头的主体
        Gizmos.DrawRay(position, direction);

        // 计算箭头尖端的大小和方向
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);

        // 绘制箭头尖端
        Gizmos.DrawRay(position + direction, right * arrowHeadLength/ 100);
        Gizmos.DrawRay(position + direction, left * arrowHeadLength / 100);
    }
}
