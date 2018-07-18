using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearedSweet : MonoBehaviour {

    public AnimationClip clearAnimtion;

    private bool isClearing;

    public bool IsClearing
    {
        get
        {
            return isClearing;
        }

        set
        {
            isClearing = value;
        }
    }

    public AudioClip destroyAudio;

    protected GameSweet sweet;

    public virtual void Clear()
    {
        IsClearing = true;
        StartCoroutine(ClearCorourine());
    }

    private void Awake()
    {
        sweet = GetComponent<GameSweet>();
    }

    private IEnumerator ClearCorourine()
    {
        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.Play(clearAnimtion.name);
            //玩家得分+1 播放清除声音
            GameManager.Instance.playerScore++;
            AudioSource.PlayClipAtPoint(destroyAudio, transform.position);
            yield return new WaitForSeconds(clearAnimtion.length);
            Destroy(gameObject);
        }
    }
}
