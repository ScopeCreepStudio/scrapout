using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class worm_anim : MonoBehaviour
{
    Animator my_Animator;
    public float anim_speed = 1;
    float randomIdleStart;
    public string name_anim;
    // Start is called before the first frame update
    void Start()
    {
       
        my_Animator = gameObject.GetComponent<Animator>();
        my_Animator.speed = anim_speed;
        randomIdleStart = Random.Range(0, my_Animator.GetCurrentAnimatorStateInfo(0).length);
        my_Animator.Play(name_anim, 0, randomIdleStart);
    }

    // Update is called once per frame
    void Update()
    {
        my_Animator.speed = anim_speed;
        
        
    }
}
