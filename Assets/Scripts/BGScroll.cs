using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGScroll : MonoBehaviour
{
    [SerializeField] Vector2 speed;

    private RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        transform.position = new Vector2((speed.x * Time.time) % (rt.rect.width/2f),(speed.y * Time.time) % (rt.rect.height/2f));
    }
}
