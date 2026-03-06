using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace NewPlay.ArcadeIdle
{
    public class RoleController : MonoBehaviour
    {
        private MoreMountains.Tools.AIBrain aiBrain;
        public MoreMountains.Tools.AIBrain AIBrain
        {
            get 
            {
                if (aiBrain == null)
                {
                    aiBrain = GetComponent<MoreMountains.Tools.AIBrain>();
                }
                return aiBrain;
            } 
        }
    }
}
