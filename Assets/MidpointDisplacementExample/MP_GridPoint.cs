using System;
using TMPro;
using UnityEngine;
public class MP_GridPoint : MonoBehaviour
{
    [NonSerialized] public Vector2 CurrentPos;
    [NonSerialized] public float Height = 0;
    public void UpdateHeight(float _h)
    {
        if(Height == 0)
        {
        Height = _h;
        transform.position = new Vector3(transform.position.x, Height, transform.position.z);
        }
    }
}

