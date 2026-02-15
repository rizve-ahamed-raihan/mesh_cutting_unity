using UnityEngine;
using UnityEngine.Playables;

namespace DefaultNamespace
{
    public class OnAttackActiveComponent : MonoBehaviour
    {
        public PlayableDirector playableDirector;
        
        public void OnAttack()
        {
            Debug.Log("Attack");
            playableDirector.time = 0;
            playableDirector.Play();
        }
    }
}