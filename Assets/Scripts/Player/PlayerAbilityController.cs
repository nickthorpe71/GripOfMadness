using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    private StarterAssetsInputs _input;
    
    private Animator _animator;
    private int _animIDCast1;
    private bool _hasAnimator;
    
    void Start()
    {
        _hasAnimator = TryGetComponent(out _animator);
        _input = GetComponent<StarterAssetsInputs>();
        
        AssignAnimationIDs();
    }
    
    void Update()
    {
        UseSkill();
    }

    private void AssignAnimationIDs()
    {
        _animIDCast1 = Animator.StringToHash("cast1");

    }
    
    private void UseSkill()
    {
        if (_input.useSkill)
        {
            _animator.SetBool(_animIDCast1, true);
            _input.useSkill = false;
        }
    }
}
