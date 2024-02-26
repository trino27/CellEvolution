using EvolutionNetwork.DDQNwithGA.DDQN.NN;
using EvolutionNetwork.GenAlg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.DDQNwithGA.DDQN.DQNMethods
{
    internal class TDLearningMethod
    {
        public double[] TDLearningDDQN(double discountFactor, double[] state, bool done, double reward, int decidedAction,
            double[] nextState, NNLayers[] onlineLayers, NNLayers[] targetLayers, NNInference inference)
        {
            //TD Learning + DDQN
            double tdTarget;
            if (!done)
            {
                // Получаем Q-значения для следующего состояния с использованием онлайн-сети для выбора действия
                double[] nextQValuesOnline = inference.FeedForward(nextState, onlineLayers);
                int nextAction = Array.IndexOf(nextQValuesOnline, nextQValuesOnline.Max()); // Выбор действия

                double[] nextQValuesTarget = inference.FeedForward(nextState, targetLayers);
                tdTarget = reward + discountFactor * nextQValuesTarget[nextAction]; // Используем уже выбранное действие
            }
            else
            {
                tdTarget = reward; // Если эпизод завершен, цель равна полученной награде
            }

            // Получаем текущие Q-значения для начального состояния с использованием онлайн-сети
            double[] currentQValuesOnline = inference.FeedForwardWithNoise(state, onlineLayers);
            // Подготовка массива целевых Q-значений для обучения
            double[] targetQValues = new double[currentQValuesOnline.Length];
            Array.Copy(currentQValuesOnline, targetQValues, currentQValuesOnline.Length);
            targetQValues[decidedAction] = tdTarget; // Обновляем Q-значение для выбранного действия
            return targetQValues;
        }
    }
}
