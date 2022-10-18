using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace InfinityPBR.Demo
{
    public class InfinityDemoCharacter : MonoBehaviour
    {
        
        [Header("Components")]
        public Animator animator;
        public SkinnedMeshRenderer skinnedMeshRenderer;

        [Header("Options")]
        public bool automateStyles = true;
        public KeyCode superRandomKey = KeyCode.R;
        public Button superRandomButton;
        public Button resetButton;
        
        [Header("Demo Stuff")] 
        public string[] animationTriggers;
        public GameObject animationFloatPrefab;
        public GameObject animationButtonPrefab;
        public GameObject animationButtonContainer;
        public GameObject automateStyleToggle;
        public string[] styleKeys; // Keys of float styles that will be automated.
        public string[] keysToExclude; // These keys will not load in the demo scene.
        public bool usingAutomatedStyles = true;
        
        // Privates
        private static readonly int Locomotion = Animator.StringToHash("Locomotion");
        private int _animationTriggerIndex;
        
        // Start is called before the first frame update
        public void Start()
        {
            Debug.Log($"Start");
            if (automateStyles) CreateStyleToggle();
            PopulateAnimationButtons();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) SetAnimation(_animationTriggerIndex += 1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) SetAnimation(_animationTriggerIndex -= 1);
            if (Input.GetKeyDown(KeyCode.Space)) TriggerAnimation();
            if (Input.GetKeyDown(superRandomKey) && !ShiftIsDown()) superRandomButton.onClick.Invoke();
            if (Input.GetKeyDown(superRandomKey) && ShiftIsDown()) resetButton.onClick.Invoke();
        }
        
        private void SetAnimation(int newIndex, bool trigger = true)
        {
            if (TriggerParameters().Length == 0) return;
            if (newIndex < 0) newIndex = 0;
            if (newIndex >= TriggerParameters().Length) newIndex = TriggerParameters().Length - 1;
            _animationTriggerIndex = newIndex;
            
            Debug.Log($"Animation trigger is <color=#ff00ff>{TriggerParameters()[newIndex].name}</color>");
            
            if (!trigger || ShiftIsDown()) return;
            TriggerAnimation();
        }

        private bool ShiftIsDown() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        private void TriggerAnimation()
        {
            if (animator == null) return;
            animator.SetTrigger(TriggerParameters()[_animationTriggerIndex].name);
        }
        
        // Animator Component Stuff
        public void SetLocomotion(float value) => animator.SetFloat(Locomotion, value);

        // This adds the animation trigger buttons to the demo scene
        private void PopulateAnimationButtons()
        {
            foreach (var parameter in FloatParameters())
                CreateFloatSlider(parameter.name);
            foreach (var parameter in TriggerParameters())
                CreateTriggerButton(parameter.name);
        }

        private AnimatorControllerParameter[] TriggerParameters() => animator.parameters
            .Where(x => !keysToExclude.Contains(x.name))
            .Where(x => x.type == AnimatorControllerParameterType.Trigger)
            .ToArray();
        
        private AnimatorControllerParameter[] FloatParameters() => animator.parameters
            .Where(x => !keysToExclude.Contains(x.name))
            .Where(x => x.type == AnimatorControllerParameterType.Float)
            .ToArray();

        // Adds a single button
        private void CreateStyleToggle()
        {
            if (automateStyleToggle == null) return;
            var newToggle = Instantiate(automateStyleToggle, animationButtonContainer.transform);
            newToggle.GetComponent<InfinityDemoAutomateStyles>().Setup(this);
        }

        
        // Adds a single button
        private void CreateTriggerButton(string trigger)
        {
            if (animationButtonPrefab == null) return;
            var newTrigger = Instantiate(animationButtonPrefab, animationButtonContainer.transform);
            newTrigger.name = trigger;
            newTrigger.GetComponent<InfinityDemoAnimationButton>().Setup(trigger, animator);
        }
        
        // Adds a single float slider
        private void CreateFloatSlider(string key)
        {
            if (animationFloatPrefab == null) return;
            var newSlider = Instantiate(animationFloatPrefab, animationButtonContainer.transform);
            newSlider.name = key;
            newSlider.GetComponent<InfinityDemoFloatSlider>().Setup(key, animator);
        }
    }
}
