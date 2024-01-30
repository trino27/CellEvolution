using System;
using System.Collections.Generic;
using System.Threading;

static class EvolutionSimulator
{
    static Random random = new Random();

    static void Main()
    {
        const int populationSize = 100;
        const int inputSize = 2; // Размер входного слоя
        const int outputSize = 2; // Размер выходного слоя
        const int maxSteps = 10000; // Максимальное количество шагов эволюции
        const int worldSize = 20; // Размер мира (плоскости)

        List<NeuralNetwork> population = InitializePopulation(populationSize, inputSize, outputSize);
        List<Food> foods = GenerateFood(worldSize, 10); // Генерация еды

        for (int step = 0; step < maxSteps; step++)
        {
            Console.Clear();
            Console.WriteLine($"Шаг эволюции: {step + 1}");

            // Появление новой еды
            if (random.NextDouble() < 0.1) // 10% шанс появления еды на каждом шаге
            {
                foods.Add(new Food(random.Next(worldSize), random.Next(worldSize)));
            }

            // Симуляция и оценка каждого агента в популяции
            for (int i = 0; i < population.Count; i++)
            {
                NeuralNetwork agent = population[i];
                double[] inputs = { random.NextDouble() * worldSize, random.NextDouble() * worldSize }; // Пример входных данных

                // Симуляция движения агента и оценка его успеха
                double fitness = SimulateAgent(agent, inputs, foods);

                // Сохранение фитнеса в агенте
                agent.Fitness = fitness;
            }

            // Вывод информации об агентах и еде в консоль
            DisplayWorld(population, foods, worldSize);

            // Выбор лучших агентов на основе фитнеса
            population = SelectBestAgents(population, populationSize / 2);

            // Генетический алгоритм: скрещивание и мутация
            population = CrossoverAndMutate(population, populationSize / 2, inputSize, outputSize);

            Thread.Sleep(100); // Задержка для визуализации
        }

        Console.ReadLine();
    }

    static List<NeuralNetwork> InitializePopulation(int size, int inputSize, int outputSize)
    {
        List<NeuralNetwork> population = new List<NeuralNetwork>();

        for (int i = 0; i < size; i++)
        {
            NeuralNetwork agent = new NeuralNetwork(inputSize, outputSize);
            population.Add(agent);
        }

        return population;
    }

    static double SimulateAgent(NeuralNetwork agent, double[] inputs, List<Food> foods)
    {
        // Пример симуляции: агент движется в сторону ближайшей еды
        double[] outputs = agent.FeedForward(inputs);
        double agentX = inputs[0];
        double agentY = inputs[1];
        double closestFoodDistance = double.MaxValue;

        foreach (var food in foods)
        {
            double distance = Math.Sqrt(Math.Pow(agentX - food.X, 2) + Math.Pow(agentY - food.Y, 2));
            if (distance < closestFoodDistance)
            {
                closestFoodDistance = distance;
            }
        }

        return 1.0 / (1.0 + closestFoodDistance); // Инвертированный коэффициент расстояния в качество фитнеса
    }

    static List<NeuralNetwork> SelectBestAgents(List<NeuralNetwork> population, int count)
    {
        // Сортировка популяции по убыванию фитнеса
        population.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
        return population.GetRange(0, count);
    }

    static List<NeuralNetwork> CrossoverAndMutate(List<NeuralNetwork> population, int count, int inputSize, int outputSize)
    {
        List<NeuralNetwork> newPopulation = new List<NeuralNetwork>();

        for (int i = 0; i < count; i++)
        {
            NeuralNetwork parent1 = population[random.Next(population.Count)];
            NeuralNetwork parent2 = population[random.Next(population.Count)];

            NeuralNetwork child = NeuralNetwork.Crossover(parent1, parent2);
            child.Mutate();

            newPopulation.Add(child);
        }

        return newPopulation;
    }

    static List<Food> GenerateFood(int worldSize, int foodCount)
    {
        List<Food> foods = new List<Food>();

        for (int i = 0; i < foodCount; i++)
        {
            foods.Add(new Food(random.Next(worldSize), random.Next(worldSize)));
        }

        return foods;
    }

    static void DisplayWorld(List<NeuralNetwork> agents, List<Food> foods, int worldSize)
    {
        Console.WriteLine("Мир:");

        for (int y = 0; y < worldSize; y++)
        {
            for (int x = 0; x < worldSize; x++)
            {
                char symbol = ' ';

                // Отображение агентов
                foreach (var agent in agents)
                {
                    double[] agentPosition = agent.FeedForward(new double[] { x, y });
                    if (Math.Abs(agentPosition[0] - x) < 0.5 && Math.Abs(agentPosition[1] - y) < 0.5)
                    {
                        symbol = 'A';
                        break;
                    }
                }

                // Отображение еды
                foreach (var food in foods)
                {
                    if (food.X == x && food.Y == y)
                    {
                        symbol = 'F';
                        break;
                    }
                }

                Console.Write(symbol);
            }

            Console.WriteLine();
        }
    }
}

class NeuralNetwork
{
    public double[][] Weights { get; set; }
    public double Fitness { get; set; }

    public NeuralNetwork(int inputSize, int outputSize)
    {
        // Инициализация весов нейронной сети
        Random random = new Random();
        Weights = new double[inputSize + 1][]; // +1 для смещения (bias)

        for (int i = 0; i < inputSize + 1; i++)
        {
            Weights[i] = new double[outputSize];

            for (int j = 0; j < outputSize; j++)
            {
                Weights[i][j] = random.NextDouble() * 2 - 1; // Случайные веса от -1 до 1
            }
        }
    }

    public double[] FeedForward(double[] inputs)
    {
        // Прямой проход через нейронную сеть
        double[] outputs = new double[Weights[0].Length];

        for (int i = 0; i < Weights[0].Length; i++)
        {
            outputs[i] = 0;

            for (int j = 0; j < inputs.Length; j++)
            {
                outputs[i] += inputs[j] * Weights[j][i];
            }

            outputs[i] += Weights[inputs.Length][i]; // Добавление смещения (bias)
            outputs[i] = Sigmoid(outputs[i]); // Применение функции активации (сигмоид)
        }

        return outputs;
    }

    public static NeuralNetwork Crossover(NeuralNetwork parent1, NeuralNetwork parent2)
    {
        // Реализация скрещивания двух родителей
        Random random = new Random();
        NeuralNetwork child = new NeuralNetwork(parent1.Weights.Length - 1, parent1.Weights[0].Length);

        for (int i = 0; i < parent1.Weights.Length; i++)
        {
            for (int j = 0; j < parent1.Weights[i].Length; j++)
            {
                // Простое скрещивание: берем веса от случайного родителя
                child.Weights[i][j] = (random.Next(2) == 0) ? parent1.Weights[i][j] : parent2.Weights[i][j];
            }
        }

        return child;
    }

    public void Mutate()
    {
        // Реализация мутации весов нейронной сети
        Random random = new Random();
        const double mutationRate = 0.1;

        for (int i = 0; i < Weights.Length; i++)
        {
            for (int j = 0; j < Weights[i].Length; j++)
            {
                if (random.NextDouble() < mutationRate)
                {
                    // Мутация: добавляем случайное значение к весу
                    Weights[i][j] += (random.NextDouble() * 2 - 1) * 0.1; // Мутационное значение от -0.1 до 0.1
                }
            }
        }
    }

    private double Sigmoid(double x)
    {
        // Функция сигмоида
        return 1.0 / (1.0 + Math.Exp(-x));
    }
}

class Food
{
    public double X { get; }
    public double Y { get; }

    public Food(double x, double y)
    {
        X = x;
        Y = y;
    }
}
