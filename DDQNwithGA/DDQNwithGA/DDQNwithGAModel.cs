using EvolutionNetwork.DDQNwithGA.DDQN;
using EvolutionNetwork.DDQNwithGA.DDQN.DQNMethods;
using EvolutionNetwork.DDQNwithGA.DDQN.NN;
using EvolutionNetwork.DDQNwithGA.Interfaces;
using EvolutionNetwork.GenAlg;
using EvolutionNetwork.StaticClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EvolutionNetwork.GenAlg.HyperparameterGen;

namespace EvolutionNetwork.DDQNwithGA
{
    public class DDQNwithGAModel
    {
        private readonly Random random = new Random();
        private TDLearningMethod TDLearning = new TDLearningMethod();
        private RSEMethod RSEMethod = new RSEMethod();

        public IDDQNwithGACritic Critic;
        public IDDQNwithGACustomRewardCalculator RewardCalculator;
        public IDDQNwithGACustomRemindExperiencesDefinder RemindExperiencesDefinder;

        //Critic
        public bool IsActionError = false;

        // DQN
        public List<DDQNMemory> Memory { get; set; } = new List<DDQNMemory>();
        public uint MaxMemoryCapacity { get; private set; }
        public uint MinMemoryToStartMemoryReplayLearning { get; private set; }
        public bool IsMemoryEnough { get; private set; }
        public uint BatchMemorySize { get; private set; }

        public double TotalReward { get; private set; } = 0;
        public double TotalActionsNum { get; private set; } = 0;

        public double[] AfterActionState { get; private set; }
        public double[] BeforeActionState { get; private set; }
        public double TargetValueBeforeAction { get; private set; }
        public int Action { get; private set; }

        // NN
        public NNLayers[] OnlineLayers { get; private set; }
        public NNLayers[] TargetLayers { get; private set; }
        public int[] LayersSizes { get; private set; }

        public HyperparameterGen Gen;

        internal NNInference NNInference;
        internal NNTrainWithSGDM NNTrainWithSGDM;

        public DDQNwithGAModel(HyperparameterGen gen, int[] layersSizes, uint maxMemoryCapacity, uint batchMemorySize, uint minMemoryToStartMemoryReplayLearning = 0)
        {
            this.Gen = gen;
            this.LayersSizes = layersSizes;
            InitNN();
            InitDDQN(layersSizes, maxMemoryCapacity, batchMemorySize, minMemoryToStartMemoryReplayLearning);
        }

        // Вспомогательный метод для обновления сети и обучения
        public void UpdateAndTrainIfNeeded()
        {
            UpdateTargetNetwork();
            if (Memory.Count > MinMemoryToStartMemoryReplayLearning)
            {
                TrainFromMemoryReplay();
            }
        }
        public void UpdateMemory(double[] beforeMoveState, int action, double reward, double[] afterMoveState, bool done)
        {
            Memory.Add(new DDQNMemory(beforeMoveState, action, reward, afterMoveState, done));
            if (Memory.Count > MaxMemoryCapacity)
            {
                Memory.RemoveAt(0); // Удаляем самый старый опыт, чтобы не превышать максимальную емкость
            }
        }
        private void UpdateTargetNetwork()
        {
            for (int i = 0; i < OnlineLayers.Length; i++)
            {
                Array.Copy(OnlineLayers[i].weights, TargetLayers[i].weights, OnlineLayers[i].weights.Length);
                Array.Copy(OnlineLayers[i].biases, TargetLayers[i].biases, OnlineLayers[i].biases.Length);
            }
        }

        public int ChooseAction(double[] currentState, double targetValue)
        {
            TargetValueBeforeAction = targetValue;
            BeforeActionState = currentState;
            TotalActionsNum++;
            IsActionError = false;

            if (IsMemoryEnough && random.NextDouble() < Gen.HyperparameterChromosome[GenHyperparameter.remindProbability])
            {
                RSEMethod.DefineMostSimilarExperiences(currentState, Gen.HyperparameterChromosome[GenHyperparameter.percentageOfSimilarExperiences], Memory, RemindExperiencesDefinder);
                RSEMethod.RemindSimilarExperiencesDDQN(OnlineLayers, TargetLayers, NNInference, TeachDDQNModel, TDLearning, Gen.HyperparameterChromosome[GenHyperparameter.discountFactor]);
            }


            int decidedAction = -1;
            //// Эпсилон-жадный выбор

            if (random.NextDouble() < Gen.HyperparameterChromosome[GenHyperparameter.exploration]) // Исследование
            {
                double[] qValuesOutput = NNInference.FeedForward(BeforeActionState, OnlineLayers);
                //// Эпсилон-жадный выбор
                if (decidedAction == -1)
                {
                    int randomIndex = random.Next(LayersSizes[^1]);
                    decidedAction = randomIndex;
                }
                Action = decidedAction;
            }
            else
            {
                double[] qValuesOutput = NNInference.FeedForward(BeforeActionState, OnlineLayers);
                decidedAction = NNInference.FindMaxIndexForFindAction(qValuesOutput);
            }
            if (Critic != null)
            {
                IsActionError = Critic.IsDecidedActionError(decidedAction, BeforeActionState);
            }
            return decidedAction;
        }

        public void ActionResultHandler(double[] afterActionState, double episodeRewardTarget, double rewardTarget, double bonusReward = 0)
        {
            this.AfterActionState = afterActionState;

            bool done = false;
            if (episodeRewardTarget > 0)
            {
                done = true;
            }


            double reward = 0;
            if (RewardCalculator == null)
            {
                reward += CalculateReward(done, episodeRewardTarget, rewardTarget, bonusReward);
            }
            else
            {
                reward += RewardCalculator.CalculateReward(done, episodeRewardTarget, rewardTarget, bonusReward);
            }
            reward = Normalizer.TanhNormalize(reward);


            if (Memory.Count > MinMemoryToStartMemoryReplayLearning)
            {
                IsMemoryEnough = true;
            }

            TrainOnline(reward, done);

            if (done)
            {

                if (!IsMemoryEnough)
                {
                    UpdateTargetNetwork();
                }
            }

            RegisterActionResult(reward, done);

        }
        private void RegisterActionResult(double reward, bool done)
        {
            UpdateMemory(BeforeActionState, Action, reward, AfterActionState, done);
        }

        private double CalculateReward(bool done, double episodeSuccessValue, double targetValue, double bonus)
        {
            double reward = 0;
            if (done)
            {
                reward = TotalReward / TotalActionsNum;
                if (reward > 0)
                {
                    double episodeReward = 0;
                    for (int i = 0; i < episodeSuccessValue; i++)
                    {
                        episodeReward += reward * Gen.HyperparameterChromosome[GenHyperparameter.genDoneBonusA] / Math.Pow(Gen.HyperparameterChromosome[GenHyperparameter.genDoneBonusB], i);
                    }
                    reward = episodeReward;
                }
                else
                {
                    reward = 0;
                }
                reward += bonus;
                TotalReward = 0;
                TotalActionsNum = 0;
            }
            else
            {
                reward = targetValue - TargetValueBeforeAction;

                if (IsActionError)
                {

                    reward -= Gen.HyperparameterChromosome[GenHyperparameter.errorFine];
                }
                else
                {
                    reward += Gen.HyperparameterChromosome[GenHyperparameter.correctBonus];
                }

                reward += bonus;

                TotalReward += reward;
            }
            return reward;
        }

        private void TeachDDQNModel(double[] stateInput, double[] targetQValues)
        {
            double[] predictedQValues = NNInference.FeedForward(stateInput, OnlineLayers);
            NNTrainWithSGDM.BackPropagationSGDM(predictedQValues, targetQValues, OnlineLayers);
        }
        private void TrainFromMemoryReplay()
        {
            var random = new Random();
            var sampledExperiences = new List<DDQNMemory>();

            for (int i = 0; i < BatchMemorySize; i++)
            {
                int index = random.Next(Memory.Count);
                sampledExperiences.Add(Memory[index]);
            }

            foreach (var experience in sampledExperiences)
            {
                double[] state = experience.BeforeActionState;

                double[] targetQValues = TDLearning.TDLearningDDQN(Gen.HyperparameterChromosome[GenHyperparameter.discountFactor],
                    experience.BeforeActionState, experience.Done, experience.Reward,
                    experience.DecidedAction, experience.AfterActionState, OnlineLayers, TargetLayers, NNInference);

                TeachDDQNModel(state, targetQValues);
            }
        }
        private void TrainOnline(double reward, bool done)
        {
            double[] targetQValues = TDLearning.TDLearningDDQN(Gen.HyperparameterChromosome[GenHyperparameter.discountFactor],
                BeforeActionState, done, reward, Action, AfterActionState, OnlineLayers, TargetLayers, NNInference);
            TeachDDQNModel(BeforeActionState, targetQValues);
        }

        // Вспомогательный метод для инициализации модели
        private void InitDDQN(int[] layerSizes, uint maxMemoryCapacity, uint batchMemorySize, uint minMemoryToStartMemoryReplayLearning)
        {
            LayersSizes = new int[layerSizes.Length];
            Array.Copy(layerSizes, LayersSizes, layerSizes.Length);
            InitNetworkLayers();
            NNTrainWithSGDM.InitVelocities(OnlineLayers);
            this.MaxMemoryCapacity = maxMemoryCapacity;
            this.BatchMemorySize = batchMemorySize;
            this.MinMemoryToStartMemoryReplayLearning = minMemoryToStartMemoryReplayLearning;
        }
        private void InitNN()
        {
            NNInference = new NNInference(Gen, LayersSizes);
            NNTrainWithSGDM = new NNTrainWithSGDM(Gen);
        }
        public void InitNetworkLayers()
        {
            OnlineLayers = new NNLayers[LayersSizes.Length];
            for (int i = 0; i < LayersSizes.Length; i++)
            {
                OnlineLayers[i] = new NNLayers(LayersSizes[i], i < LayersSizes.Length - 1 ? LayersSizes[i + 1] : 0);
                OnlineLayers[i].InitializeWeightsHe(); // Инициализация весов для онлайн слоев
            }

            TargetLayers = new NNLayers[LayersSizes.Length];
            for (int i = 0; i < LayersSizes.Length; i++)
            {
                TargetLayers[i] = new NNLayers(LayersSizes[i], i < LayersSizes.Length - 1 ? LayersSizes[i + 1] : 0);
                TargetLayers[i].InitializeWeightsHe(); // Инициализация весов для целевых слоев
            }
        }

        public void InitCritic(IDDQNwithGACritic critic)
        {
            this.Critic = critic ?? throw new ArgumentNullException(nameof(critic));
        }
        public void InitRewardCalculator(IDDQNwithGACustomRewardCalculator rewardCalculator)
        {
            this.RewardCalculator = rewardCalculator ?? throw new ArgumentNullException(nameof(rewardCalculator));
        }
        public void InitRemindExperiencesDefinder(IDDQNwithGACustomRemindExperiencesDefinder remindExperiencesDefinder)
        {
            this.RemindExperiencesDefinder = remindExperiencesDefinder ?? throw new ArgumentNullException(nameof(remindExperiencesDefinder));
        }
    }
}
