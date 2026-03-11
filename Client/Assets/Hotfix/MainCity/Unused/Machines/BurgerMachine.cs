using System.Collections.Generic;
using System.Linq;

namespace NewPlay.ArcadeIdle
{
    public class BurgerMachine : FoodMachine
    {
        private List<FlippingObject> flippingPatties;
        private List<MovingObject> movingBurgers;

        /// <summary>
        /// Initializes references to flipping patties and moving burgers.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            flippingPatties = GetComponentsInChildren<FlippingObject>(true).ToList();
            movingBurgers = GetComponentsInChildren<MovingObject>(true).ToList();
        }

        /// <summary>
        /// Updates the active state of components based on the machine's unlock level.
        /// </summary>
        protected override void UpdateStats()
        {
            base.UpdateStats();

            flippingPatties.ForEach(p => p.gameObject.SetActive(unlockLevel <= 2));
            movingBurgers.ForEach(b => b.gameObject.SetActive(unlockLevel > 2));
        }
    }
}
