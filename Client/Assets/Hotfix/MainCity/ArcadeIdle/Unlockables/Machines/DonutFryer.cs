using System.Collections.Generic;
using System.Linq;

namespace NewPlay.ArcadeIdle
{
    public class DonutFryer : FoodMachine
    {
        private List<FlippingObject> flippingDonuts; // List of objects responsible for flipping donuts.
        private List<TravelingObject> travelingDonuts; // List of objects responsible for traveling donuts.
        private MovingObject movingDonut; // A single object responsible for moving a donut.

        protected override void Awake()
        {
            base.Awake();

            // Retrieve all flipping objects within the fryer (even inactive ones).
            flippingDonuts = GetComponentsInChildren<FlippingObject>(true).ToList();

            // Retrieve all traveling objects within the fryer (even inactive ones).
            travelingDonuts = GetComponentsInChildren<TravelingObject>(true).ToList();

            // Retrieve a single moving object within the fryer.
            movingDonut = GetComponentInChildren<MovingObject>();
        }

        /// <summary>
        /// Updates the active state of the donut fryer components based on the unlock level.
        /// </summary>
        protected override void UpdateStats()
        {
            base.UpdateStats();

            // Activate flipping donuts only when the unlock level is 1.
            flippingDonuts.ForEach(donut => donut.gameObject.SetActive(unlockLevel == 1));

            // Activate traveling donuts only when the unlock level is 2.
            travelingDonuts.ForEach(donut => donut.gameObject.SetActive(unlockLevel == 2));

            // Activate the single moving donut only when the unlock level is 3.
            movingDonut.gameObject.SetActive(unlockLevel == 3);
        }
    }
}
