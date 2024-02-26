using EvolutionNetwork.DDQNwithGA.DDQN.NN;
using EvolutionNetwork.DDQNwithGA.Interfaces;
using EvolutionNetwork.GenAlg;

namespace EvolutionNetwork.DDQNwithGA.DDQN.DQNMethods
{
    internal class RSEMethod
    {
        List<DDQNMemory> mostSimilarExperiences = new List<DDQNMemory>();

        // Метод для расчёта Евклидова расстояния между текущим и предыдущим состояниями
        private double CalculateEuclideanDistance(double[] currentState, double[] previousState)
        {
            double sum = 0.0;
            for (int i = 0; i < currentState.Length; i++)
            {
                sum += Math.Pow(currentState[i] - previousState[i], 2);
            }
            return Math.Sqrt(sum);
        }

        public void DefineMostSimilarExperiences(double[] currentState, double percentageOfSimilarExperiences, List<DDQNMemory> memory, IDDQNwithGACustomRemindExperiencesDefinder remindExperiencesDefinder = null)
        {
            int experiencesToConsider = (int)(memory.Count * percentageOfSimilarExperiences);

            if (experiencesToConsider > 0)
            {

                if (remindExperiencesDefinder != null)
                {
                    mostSimilarExperiences = remindExperiencesDefinder.CustomRemindExperiencesDefinder(memory, currentState)
                        .OrderBy(experience => remindExperiencesDefinder.CustomCalculateSimilarityState(currentState, experience.BeforeActionState))
                        .Take(experiencesToConsider)
                        .ToList();
                }
                else
                {
                    mostSimilarExperiences = memory
                        .OrderBy(experience => CalculateEuclideanDistance(currentState, experience.BeforeActionState))
                        .Take(experiencesToConsider)
                        .ToList();
                }
            }
        }

        public void RemindSimilarExperiencesDDQN(NNLayers[] onlineLayers, NNLayers[] targetLayers, NNInference inference, Action<double[], double[]> teachModel, TDLearningMethod TDLearning = null, double discountFactor = 0.95)
        {
            Random random = new Random();
            if (mostSimilarExperiences.Count > 0)
            {
                double[] targetQValues;
                int i = random.Next(0, mostSimilarExperiences.Count);
                if (TDLearning != null)
                {
                    // Вычисляем и применяем обучение на основе целевых Q-значений, полученных из выбранных воспоминаний
                    targetQValues =
                        TDLearning.TDLearningDDQN(discountFactor, mostSimilarExperiences[i].BeforeActionState,
                    mostSimilarExperiences[i].Done, mostSimilarExperiences[i].Reward,
                        mostSimilarExperiences[i].DecidedAction, mostSimilarExperiences[i].AfterActionState, onlineLayers, targetLayers, inference);
                    // Обучаем модель, используя состояние до действия из воспоминания и вычисленные целевые Q-значения
                }
                else
                {
                    targetQValues = inference.FeedForwardWithNoise(mostSimilarExperiences[i].BeforeActionState, onlineLayers);
                }
                teachModel(mostSimilarExperiences[i].BeforeActionState, targetQValues);
            }

        }

    }
}
