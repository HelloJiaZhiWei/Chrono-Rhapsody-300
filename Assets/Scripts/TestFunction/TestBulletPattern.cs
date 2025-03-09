using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBulletPattern : MonoBehaviour
{
    public FireConfig bulletConfig;
    private void Start()
    {

    }
    private void OnEnable()
    {

    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(AttackRoutine(bulletConfig.fireInterval * (bulletConfig.bulletCount + 1) + bulletConfig.waveInterval));
        }
    }
    IEnumerator AttackRoutine(float intervalTime)
    {
        int i = 0;
        while (i < bulletConfig.waveCount)
        {
            i++;
            BulletPatternManager.Instance.FirePattern(bulletConfig, transform.position, Vector2.right);
            yield return new WaitForSeconds(intervalTime);
        }


        yield return null;

    }
}
