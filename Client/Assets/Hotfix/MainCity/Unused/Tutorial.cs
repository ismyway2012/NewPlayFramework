using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class Tutorial : MonoBehaviour
    {
        [SerializeField] private List<StateMessage> stateMessages;
        [SerializeField] private TextMeshProUGUI tutorialMessage;
        [SerializeField] private PlayerController player;
        [SerializeField] private Seating firstSeating;
        [SerializeField] private CounterTable counterTable;
        [SerializeField] private FoodMachine foodMachine;
        [SerializeField] private Seating secondSeating;
        [SerializeField] private Unlockable officeHR;
        [SerializeField] private Unlockable officeGM;
        [SerializeField] private GameObject arrowPrefab;

        private TutorialState currentState;
        private TutorialState messageState;

        private Dictionary<TutorialState, string> messageLookup;

        private GameObject arrow;

        private void Start()
        {
            currentState = (TutorialState)PlayerPrefs.GetInt("Tutorial", 1);

            if (currentState == TutorialState.Ended)
            {
                DestroyTutorial();
            }
            else
            {
                messageLookup = stateMessages.ToDictionary(m => m.State, m => m.Message);
                StartCoroutine(BeginTutorial());
            }
        }

        private void DestroyTutorial()
        {
            Destroy(tutorialMessage.transform.parent.gameObject);
            Destroy(gameObject);
        }

        private IEnumerator BeginTutorial()
        {
            while (currentState == TutorialState.Started)
            {
                Vector3 playerPos = player.transform.position;
                Vector3 firstSeatingPos = firstSeating.transform.position;

                if (Vector3.Distance(playerPos, firstSeatingPos) < 8f)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                yield return null;
            }

            while (currentState == TutorialState.FirstSeating)
            {
                if (firstSeating.gameObject.activeSelf)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(firstSeating.BuyingPoint + Vector3.up * 3);
                yield return null;
            }

            while (currentState == TutorialState.CounterTable)
            {
                if (counterTable.gameObject.activeSelf)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(counterTable.BuyingPoint + Vector3.up * 3);
                yield return null;
            }

            while (currentState == TutorialState.FoodMachine)
            {
                if (foodMachine.gameObject.activeSelf)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(foodMachine.BuyingPoint + Vector3.up * 3);
                yield return null;
            }

            while (currentState == TutorialState.DeliverToCounter)
            {
                if (counterTable.FoodCount >= 5)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(counterTable.FoodPoint + Vector3.up * 3);
                yield return null;
            }

            float serveTime = 0f;

            while (currentState == TutorialState.SellFood)
            {
                if (counterTable.HasWorker) serveTime += Time.deltaTime;

                if (serveTime >= 8f)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(counterTable.WorkingPoint + Vector3.up * 3);
                yield return null;
            }

            long initialMoney = RestaurantManager.Instance.GetMoney();

            while (currentState == TutorialState.CollectRevenue)
            {
                if (RestaurantManager.Instance.GetMoney() != initialMoney)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(counterTable.MoneyPoint + Vector3.up * 3);
                yield return null;
            }

            while (currentState == TutorialState.MoreSeating)
            {
                if (secondSeating.gameObject.activeSelf)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(secondSeating.BuyingPoint + Vector3.up * 3);
                yield return null;
            }

            while (currentState == TutorialState.HireHR)
            {
                if (officeHR.gameObject.activeSelf)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(officeHR.BuyingPoint + Vector3.up * 3);
                yield return null;
            }

            while (currentState == TutorialState.HireEmployee)
            {
                if (RestaurantManager.Instance.GetUpgradeLevel(Upgrade.EmployeeAmount) > 0)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(officeHR.BuyingPoint + Vector3.up * 3);
                yield return null;
            }

            while (currentState == TutorialState.HireGM)
            {
                if (officeGM.gameObject.activeSelf)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(officeGM.BuyingPoint + Vector3.up * 3);
                yield return null;
            }

            while (currentState == TutorialState.UpgradeCounter)
            {
                if (counterTable.UnlockLevel > 1)
                {
                    AdvanceCurrentState();
                }

                TryUpdateTutorialMessage();
                UpdateArrowPosition(counterTable.BuyingPoint + Vector3.up * 3);
                yield return null;
            }

            Destroy(arrow.gameObject);

            yield return new WaitForSeconds(5f);

            DestroyTutorial();
        }

        private void TryUpdateTutorialMessage()
        {
            if (messageState != currentState)
            {
                if (messageLookup.TryGetValue(currentState, out string message))
                {
                    tutorialMessage.text = message;
                    tutorialMessage.color = Color.black;
                }
                else
                {
                    tutorialMessage.text = $"[ERROR] Tutorial message not found for state: {currentState}.";
                    tutorialMessage.text += $" Please add a message to the 'stateMessages' list in the Inspector.";
                    tutorialMessage.color = Color.red;
                }

                tutorialMessage.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);

                messageState = currentState;
            }
        }

        private void UpdateArrowPosition(Vector3 position)
        {
            if (arrow == null)
            {
                arrow = Instantiate(arrowPrefab);
            }

            float verticalOffset = Mathf.Sin(Time.time * 5f) * 0.5f;
            Vector3 oscillatingPosition = position;
            oscillatingPosition.y += verticalOffset;

            arrow.transform.position = oscillatingPosition;
        }

        private void AdvanceCurrentState()
        {
            currentState = (TutorialState)((int)currentState + 1);
            PlayerPrefs.SetInt("Tutorial", (int)currentState);
        }
    }

    public enum TutorialState
    {
        None,
        Started,
        FirstSeating,
        CounterTable,
        FoodMachine,
        DeliverToCounter,
        SellFood,
        CollectRevenue,
        MoreSeating,
        HireHR,
        HireEmployee,
        HireGM,
        UpgradeCounter,
        Ended
    }

    [System.Serializable]
    public struct StateMessage
    {
        public TutorialState State;
        [TextArea] public string Message;
    }
}
