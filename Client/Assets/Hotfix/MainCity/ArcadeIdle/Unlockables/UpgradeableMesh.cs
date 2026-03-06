using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    [RequireComponent(typeof(MeshFilter))]
    public class UpgradeableMesh : MonoBehaviour
    {
        [SerializeField, Tooltip("Array of meshes to represent the upgraded versions of this object.")]
        Mesh[] upgradeMeshes;

        private MeshFilter meshFilter;  // The MeshFilter component to apply upgrades to

        void Awake()
        {
            // Get the MeshFilter component attached to the object
            meshFilter = GetComponent<MeshFilter>();
        }

        /// <summary>
        /// Applies the appropriate mesh based on the provided unlock level.
        /// </summary>
        /// <param name="unlockLevel">The level at which the upgrade is applied.</param>
        public void ApplyUpgrade(int unlockLevel)
        {
            // Ensure that the unlock level is within the range of available upgrade meshes
            if (unlockLevel >= upgradeMeshes.Length + 2)
            {
                Debug.LogWarning("The unlock level exceeds the available upgrade meshes." +
                    " Please ensure that the unlock level is within the valid range.");

                return;
            }

            // Set the mesh to the one corresponding to the unlock level
            meshFilter.mesh = upgradeMeshes[unlockLevel - 2];
        }
    }
}
