using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;


public class RoleBase : MonoBehaviour
{
    public Character Character { get; set; }

    private AIBrain m_AIBrain;
    public AIBrain AIBrain
    {
        get
        {
            if (m_AIBrain == null)
            {
                m_AIBrain = GetComponent<AIBrain>();
            }
            return m_AIBrain;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

}
