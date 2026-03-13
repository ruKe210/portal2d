using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crosshair : MonoBehaviour
{
    void Start()
    {
        // 隐藏系统鼠标光标
        Cursor.visible = false;
    }

    void Update()
    {
        // 准星跟随鼠标位置
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // 确保在2D平面上
        transform.position = mousePos;
    }

    void OnDestroy()
    {
        // 销毁时恢复系统光标
        Cursor.visible = true;
    }
}