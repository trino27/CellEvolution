using EvolutionNetwork.DDQNwithGA.DDQN;
using EvolutionNetwork.DDQNwithGA.Interfaces;
using EvolutionNetwork.GenAlg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EvolutionNetwork.GenAlg.HyperparameterGen;

namespace EvolutionNetwork.DDQNwithGA
{
    public class DDQNwithGACreator
    {
        public DDQNwithGAModel CreateDDQNwithGA(int[] layerSizes, Dictionary<GenHyperparameter, double> startHyperparameterScatterDict, uint maxMemoryCapacity, uint batchMemorySize, uint minMemoryToStartMemoryReplayLearning = 0)
        {
            ValidateLayerSizes(layerSizes);
            HyperparameterGen Gen = new HyperparameterGen();
            Gen.StartHyperparameterScatter(startHyperparameterScatterDict);
            DDQNwithGAModel dDQNwithGA = new DDQNwithGAModel(Gen, layerSizes, maxMemoryCapacity, batchMemorySize, minMemoryToStartMemoryReplayLearning);
            return dDQNwithGA;
        }
        public DDQNwithGAModel CreateDDQNwithGA(int[] layerSizes, uint maxMemoryCapacity, uint batchMemorySize, uint minMemoryToStartMemoryReplayLearning = 0, double startHyperparameterScatter = 0)
        {
            ValidateLayerSizes(layerSizes);
            HyperparameterGen Gen = new HyperparameterGen();
            Gen.StartHyperparameterScatter(startHyperparameterScatter);
            DDQNwithGAModel dDQNwithGA = new DDQNwithGAModel(Gen, layerSizes, maxMemoryCapacity, batchMemorySize, minMemoryToStartMemoryReplayLearning);
            return dDQNwithGA;
        }

        public DDQNwithGAModel CloneDDQNwithGA(DDQNwithGAModel original)
        {
            return CopyFrom(original);
        }
        public DDQNwithGAModel CloneDDQNwithGA(DDQNwithGAModel mother, DDQNwithGAModel father)
        {
            Random random = new Random();
            // Выбор родителя, от которого будет взята основа конфигурации
            DDQNwithGAModel baseParent = random.Next(0, 2) == 0 ? mother : father;
            DDQNwithGAModel otherParent = baseParent == mother ? father : mother;

            return MergeFrom(baseParent, otherParent);
        }
        private void ValidateLayerSizes(int[] layerSizes)
        {
            if (layerSizes.Length < 2)
            {
                throw new ArgumentException("You should have at least input and output layers");
            }
        }
        // Вспомогательный метод для копирования
        private DDQNwithGAModel CopyFrom(DDQNwithGAModel original)
        {
            HyperparameterGen Gen = new HyperparameterGen(original.Gen);
            int[] layerSizes = (int[])original.LayersSizes.Clone();

            uint batchMemorySize = original.BatchMemorySize;
            uint maxMemoryCapacity = original.MaxMemoryCapacity;
            uint minMemoryToStartMemoryReplayLearning = original.MinMemoryToStartMemoryReplayLearning;

            DDQNwithGAModel dDQNwithGACopy = new DDQNwithGAModel(Gen, layerSizes, maxMemoryCapacity, batchMemorySize, minMemoryToStartMemoryReplayLearning);
            // Переиспользование существующих методов для копирования состояний
            InheritMemory(dDQNwithGACopy, original);
            InheritVelocities(dDQNwithGACopy, original);
            InheritNetworkLayers(dDQNwithGACopy, original);

            // Копирование ссылок на внешние зависимости
            if (original.Critic != null)
            {
                dDQNwithGACopy.InitCritic(original.Critic);
            }
            if (original.RewardCalculator != null)
            {
                dDQNwithGACopy.InitRewardCalculator(original.RewardCalculator);
            }
            if (original.RemindExperiencesDefinder != null)
            {
                dDQNwithGACopy.InitRemindExperiencesDefinder(original.RemindExperiencesDefinder);
            }
            // Обновление и обучение, если достаточно памяти
            dDQNwithGACopy.UpdateAndTrainIfNeeded();

            return dDQNwithGACopy;
        }
        // Вспомогательный метод для слияния
        private DDQNwithGAModel MergeFrom(DDQNwithGAModel baseParent, DDQNwithGAModel otherParent)
        {
            // Слияние генетических параметров
            HyperparameterGen Gen = new HyperparameterGen(baseParent.Gen, otherParent.Gen);
            int[] layerSizes = (int[])baseParent.LayersSizes.Clone();

            uint batchMemorySize = baseParent.BatchMemorySize;
            uint maxMemoryCapacity = baseParent.MaxMemoryCapacity;
            uint minMemoryToStartMemoryReplayLearning = baseParent.MinMemoryToStartMemoryReplayLearning;

            DDQNwithGAModel dDQNwithGACopy = new DDQNwithGAModel(Gen, layerSizes, maxMemoryCapacity, batchMemorySize, minMemoryToStartMemoryReplayLearning);

            // Слияние памяти от обоих родителей
            InheritMemory(dDQNwithGACopy, baseParent, otherParent);
            // Наследование сети и скоростей
            InheritVelocities(dDQNwithGACopy, baseParent);
            InheritNetworkLayers(dDQNwithGACopy, baseParent);

            // Копирование ссылок на внешние зависимости
            if (baseParent.Critic != null)
            {
                dDQNwithGACopy.InitCritic(baseParent.Critic);
            }
            if (baseParent.RewardCalculator != null)
            {
                dDQNwithGACopy.InitRewardCalculator(baseParent.RewardCalculator);
            }
            if (baseParent.RemindExperiencesDefinder != null)
            {
                dDQNwithGACopy.InitRemindExperiencesDefinder(baseParent.RemindExperiencesDefinder);
            }
            // Обновление и обучение, если достаточно памяти
            dDQNwithGACopy.UpdateAndTrainIfNeeded();

            return dDQNwithGACopy;
        }

        private void InheritVelocities(DDQNwithGAModel clone,DDQNwithGAModel original)
        {
            clone.NNTrainWithSGDM.VelocitiesWeights = new double[original.NNTrainWithSGDM.VelocitiesWeights.Length][][];
            for (int layer = 0; layer < original.NNTrainWithSGDM.VelocitiesWeights.Length; layer++)
            {
                clone.NNTrainWithSGDM.VelocitiesWeights[layer] = new double[original.NNTrainWithSGDM.VelocitiesWeights[layer].Length][];
                for (int neuron = 0; neuron < original.NNTrainWithSGDM.VelocitiesWeights[layer].Length; neuron++)
                {
                    clone.NNTrainWithSGDM.VelocitiesWeights[layer][neuron] = new double[original.NNTrainWithSGDM.VelocitiesWeights[layer][neuron].Length];
                    Array.Copy(original.NNTrainWithSGDM.VelocitiesWeights[layer][neuron], clone.NNTrainWithSGDM.VelocitiesWeights[layer][neuron], original.NNTrainWithSGDM.VelocitiesWeights[layer][neuron].Length);
                }
            }

            clone.NNTrainWithSGDM.VelocitiesBiases = new double[original.NNTrainWithSGDM.VelocitiesBiases.Length][];
            for (int layer = 0; layer < original.NNTrainWithSGDM.VelocitiesBiases.Length; layer++)
            {
                clone.NNTrainWithSGDM.VelocitiesBiases[layer] = new double[original.NNTrainWithSGDM.VelocitiesBiases[layer].Length];
                Array.Copy(original.NNTrainWithSGDM.VelocitiesBiases[layer], clone.NNTrainWithSGDM.VelocitiesBiases[layer], original.NNTrainWithSGDM.VelocitiesBiases[layer].Length);
            }
        }
        private void InheritNetworkLayers(DDQNwithGAModel clone, DDQNwithGAModel original)
        {
            clone.InitNetworkLayers();
            CopyNNLayers(clone, original);
        }
        private void InheritMemory(DDQNwithGAModel clone, DDQNwithGAModel original)
        {
            clone.Memory = original.Memory.Select(m => (DDQNMemory)m.Clone()).ToList();
        }
        private void InheritMemory(DDQNwithGAModel clone, DDQNwithGAModel mother, DDQNwithGAModel father)
        {
            Random random = new Random();
            if (mother.Memory.Count > father.Memory.Count)
            {
                clone.Memory = mother.Memory.Select(m => (DDQNMemory)m.Clone()).ToList();
                for (int i = 0; i < father.Memory.Count; i++)
                {
                    if (random.Next(0, 2) == 0)
                    {
                        if (clone.Memory.Count < i)
                        {
                            clone.Memory[i] = (DDQNMemory)father.Memory[i].Clone();
                        }
                        else
                        {
                            DDQNMemory tamp = (DDQNMemory)father.Memory[i].Clone();
                            clone.UpdateMemory(tamp.BeforeActionState, tamp.DecidedAction, tamp.Reward, tamp.AfterActionState, tamp.Done);
                        }
                    }
                }
            }
            else
            {
                clone.Memory = father.Memory.Select(m => (DDQNMemory)m.Clone()).ToList();
                for (int i = 0; i < mother.Memory.Count; i++)
                {
                    if (random.Next(0, 2) == 0)
                    {
                        if (clone.Memory.Count < i)
                        {
                            clone.Memory[i] = (DDQNMemory)mother.Memory[i].Clone();
                        }
                        else
                        {
                            DDQNMemory tamp = (DDQNMemory)mother.Memory[i].Clone();
                            clone.UpdateMemory(tamp.BeforeActionState, tamp.DecidedAction, tamp.Reward, tamp.AfterActionState, tamp.Done);
                        }
                    }
                }
            }
        }
        private void CopyNNLayers(DDQNwithGAModel clone, DDQNwithGAModel original)
        {
            for (int k = 0; k < clone.OnlineLayers.Length; k++)
            {
                Array.Copy(original.OnlineLayers[k].weights, clone.OnlineLayers[k].weights, original.OnlineLayers[k].weights.Length);
                Array.Copy(original.OnlineLayers[k].neurons, clone.OnlineLayers[k].neurons, original.OnlineLayers[k].neurons.Length);
                Array.Copy(original.OnlineLayers[k].biases, clone.OnlineLayers[k].biases, original.OnlineLayers[k].biases.Length);

                clone.OnlineLayers[k].size = original.OnlineLayers[k].size;
                clone.OnlineLayers[k].nextSize = original.OnlineLayers[k].nextSize;
            }

            for (int k = 0; k < clone.TargetLayers.Length; k++)
            {
                Array.Copy(original.TargetLayers[k].weights, clone.TargetLayers[k].weights, original.TargetLayers[k].weights.Length);
                Array.Copy(original.TargetLayers[k].neurons, clone.TargetLayers[k].neurons, original.TargetLayers[k].neurons.Length);
                Array.Copy(original.TargetLayers[k].biases, clone.TargetLayers[k].biases, original.TargetLayers[k].biases.Length);

                clone.TargetLayers[k].size = original.TargetLayers[k].size;
                clone.TargetLayers[k].nextSize = original.TargetLayers[k].nextSize;
            }
        }
    }
}
