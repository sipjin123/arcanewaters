using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationRepeater : MonoBehaviour
{
    Animator anim;
    public float loopDelayMin = 1, loopDelayMax = 10;
    public float timer;

    void Start()
    {
        anim = GetComponent<Animator>();

        anim.Update(Random.Range(0f, 1f));
        timer = Random.Range(loopDelayMin, loopDelayMax);
    }

    private void Update()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).length >= anim.GetCurrentAnimatorClipInfo(0)[0].clip.length)
        {
            if (timer <= 0)
            {
                timer = Random.Range(loopDelayMin, loopDelayMax);
            }
            else
            {
                timer -= Time.deltaTime;

                if (timer <= 0)
                {
                    anim.Play(anim.GetCurrentAnimatorClipInfo(0)[0].clip.name, 0, 0);
                }
            }
        }
    }
}
