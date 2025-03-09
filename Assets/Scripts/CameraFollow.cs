using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform target;
    //private float speed = 50f;
    void Start()
    {
        target = GameManager.Instance.player.transform;
    }
    void LateUpdate()
    {
        // transform.position = Vector3.Lerp(
        //     transform.position,
        //     new Vector3(
        //         target.position.x,
        //         target.position.y,
        //         transform.position.z),
        //     Time.deltaTime * speed);
        transform.position = new Vector3(
                target.position.x,
                target.position.y,
                transform.position.z);
    }
}
