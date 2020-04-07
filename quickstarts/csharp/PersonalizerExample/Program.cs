using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PersonalizerExample
{
    class Program
    {
        private const string ServiceEndpoint = "https://loop.ppe.cognitiveservices.azure.com/"; private const string ApiKey = "";

        static void Main(string[] _)
        {
            int totalCalls = 500;
            int experimentalUnitDuration = 1;
            int noRewardFrequency = 3; // Every nth reward = 0
            int danglingRewardFrequency = 5; // Every nth reward = 0
            int rewardWaitTime = (int)((experimentalUnitDuration + 1) * TimeSpan.FromMinutes(1).TotalMilliseconds);

            // Get the actions list to choose from personalizer with their features.
            IList<RankableAction> actions = GetActions();

            // Initialize Personalizer client.
            PersonalizerClient client = InitializePersonalizerClient(ServiceEndpoint);
            int iteration = 0;
            do
            {
                Console.WriteLine("\nIteration: " + ++iteration);

                // Get context information from the user.
                string timeOfDayFeature = GetUsersTimeOfDay(iteration);
                string tasteFeature = GetUsersTastePreference(iteration);

                // Create current context from user specified data.
                IList<object> currentContext = new List<object>() {
                    new { time = timeOfDayFeature },
                    new { taste = tasteFeature }
                };

                // Exclude an action for personalizer ranking. This action will be held at its current position.
                IList<string> excludeActions = new List<string> { "juice" };

                // Generate an ID to associate with the request.
                string eventId = iteration.ToString();

                // Rank the actions
                var request = new RankRequest(actions, currentContext, excludeActions, eventId);
                RankResponse response = client.Rank(request);

                Console.WriteLine("\nPersonalizer service thinks you would like to have: " + response.RewardActionId + ". Is this correct? (y/n)");

                float reward = 0.0f;

                if (iteration % noRewardFrequency != 0)
                {
                    reward = 1;
                    Console.WriteLine("\nGreat! Enjoy your food.");
                }

                Console.WriteLine("\nPersonalizer service ranked the actions with the probabilities as below:");
                foreach (var rankedResponse in response.Ranking)
                {
                    Console.WriteLine(rankedResponse.Id + " " + rankedResponse.Probability);
                }

                // Send the reward for the action based on user response.
                SendReward(response.EventId, reward, iteration % danglingRewardFrequency == 0 ? rewardWaitTime : 1000, client);

            } while (iteration < totalCalls);
        }

        private static async void SendReward(string eventId, float rewardValue, int rewardWaitTimeInMillis, PersonalizerClient client)
        {
            await Task.Delay(rewardWaitTimeInMillis);
            client.Reward(eventId, new RewardRequest(rewardValue));
        }

        /// <summary>
        /// Initializes the personalizer client.
        /// </summary>
        /// <param name="url">Azure endpoint</param>
        /// <returns>Personalizer client instance</returns>
        static PersonalizerClient InitializePersonalizerClient(string url)
        {
            PersonalizerClient client = new PersonalizerClient(
                new ApiKeyServiceClientCredentials(ApiKey))
            { Endpoint = url };

            return client;
        }

        /// <summary>
        /// Get users time of the day context.
        /// </summary>
        /// <returns>Time of day feature selected by the user.</returns>
        static string GetUsersTimeOfDay(int iteration)
        {
            string[] timeOfDayFeatures = new string[] { "morning", "afternoon", "evening", "night" };
            return timeOfDayFeatures[iteration % timeOfDayFeatures.Length];
        }

        /// <summary>
        /// Gets user food preference.
        /// </summary>
        /// <returns>Food taste feature selected by the user.</returns>
        static string GetUsersTastePreference(int iteration)
        {
            string[] tasteFeatures = new string[] { "salty", "sweet" };
            return tasteFeatures[iteration % tasteFeatures.Length];
        }

        /// <summary>
        /// Creates personalizer actions feature list.
        /// </summary>
        /// <returns>List of actions for personalizer.</returns>
        static IList<RankableAction> GetActions()
        {
            IList<RankableAction> actions = new List<RankableAction>
            {
                new RankableAction
                {
                    Id = "pasta",
                    Features =
                    new List<object>() { new { taste = "salty", spiceLevel = "medium" }, new { nutritionLevel = 5, cuisine = "italian" } }
                },

                new RankableAction
                {
                    Id = "ice cream",
                    Features =
                    new List<object>() { new { taste = "sweet", spiceLevel = "none" }, new { nutritionalLevel = 2 } }
                },

                new RankableAction
                {
                    Id = "juice",
                    Features =
                    new List<object>() { new { taste = "sweet", spiceLevel = "none" }, new { nutritionLevel = 5 }, new { drink = true } }
                },

                new RankableAction
                {
                    Id = "salad",
                    Features =
                    new List<object>() { new { taste = "salty", spiceLevel = "low" }, new { nutritionLevel = 8 } }
                }
            };

            return actions;
        }
    }
}