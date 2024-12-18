using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.StrategySearch.Strategies;

namespace TradingSystem.StrategySearch.Services;

public class StrategyOptimizer : IStrategyOptimizer
{
    private readonly IBacktester _backtester;
    private readonly Random _random = new();
    private const int PopulationSize = 50;
    private const int Generations = 100;
    private const decimal MutationRate = 0.1m;

    public StrategyOptimizer(IBacktester backtester)
    {
        _backtester = backtester;
    }

    public async Task<OptimizationResult> Optimize(Theory baseTheory, DateTime startDate, DateTime endDate)
    {
        var population = InitializePopulation(baseTheory);
        var bestTheory = baseTheory;
        var bestFitness = decimal.MinValue;
        var generationResults = new List<GenerationResult>();

        for (int generation = 0; generation < Generations; generation++)
        {
            var fitnessScores = new Dictionary<Theory, decimal>();

            // Evaluate fitness for each theory
            foreach (var theory in population)
            {
                var strategy = new TheoryBasedStrategy(theory);
                var result = await _backtester.RunBacktestAsync(strategy, theory.Symbols.First(), startDate, endDate);
                var fitness = CalculateFitness(result);
                fitnessScores[theory] = fitness;

                if (fitness > bestFitness)
                {
                    bestFitness = fitness;
                    bestTheory = theory;
                }
            }

            // Record generation results
            generationResults.Add(new GenerationResult
            {
                GenerationNumber = generation,
                BestFitness = bestFitness,
                AverageFitness = fitnessScores.Values.Average(),
                WorstFitness = fitnessScores.Values.Min()
            });

            // Create next generation
            var nextGeneration = new List<Theory>();

            while (nextGeneration.Count < PopulationSize)
            {
                var parent1 = SelectParent(population, fitnessScores);
                var parent2 = SelectParent(population, fitnessScores);
                var (child1, child2) = Crossover(parent1, parent2);

                Mutate(child1);
                Mutate(child2);

                nextGeneration.Add(child1);
                nextGeneration.Add(child2);
            }

            population = nextGeneration.Take(PopulationSize).ToList();
        }

        var finalStrategy = new TheoryBasedStrategy(bestTheory);
        var finalResult = await _backtester.RunBacktestAsync(finalStrategy, bestTheory.Symbols.First(), startDate, endDate);

        return new OptimizationResult
        {
            BestTheory = bestTheory,
            BacktestResult = finalResult,
            GenerationResults = generationResults
        };
    }

    public async Task<OptimizationResult> OptimizeParameters(Theory theory, OptimizationSettings settings)
    {
        var population = InitializePopulationFromTheory(theory, settings.InitialPopulationSize, settings.ParameterRanges);
        var bestTheory = theory;
        var bestFitness = decimal.MinValue;
        var generationResults = new List<GenerationResult>();

        for (int generation = 0; generation < settings.Generations; generation++)
        {
            var fitnessScores = new Dictionary<Theory, decimal>();

            // Evaluate fitness for each theory
            foreach (var candidate in population)
            {
                var strategy = new TheoryBasedStrategy(candidate);
                var result = await _backtester.RunBacktestAsync(strategy, candidate.Symbols.First(), settings.StartDate, settings.EndDate);
                var fitness = CalculateFitness(result);
                fitnessScores[candidate] = fitness;

                if (fitness > bestFitness)
                {
                    bestFitness = fitness;
                    bestTheory = candidate;
                }
            }

            // Record generation results
            generationResults.Add(new GenerationResult
            {
                GenerationNumber = generation,
                BestFitness = bestFitness,
                AverageFitness = fitnessScores.Values.Average(),
                WorstFitness = fitnessScores.Values.Min()
            });

            // Create next generation
            var nextGeneration = new List<Theory>();

            while (nextGeneration.Count < settings.PopulationSize)
            {
                var parent1 = SelectParent(population, fitnessScores);
                var parent2 = SelectParent(population, fitnessScores);
                var (child1, child2) = Crossover(parent1, parent2);

                Mutate(child1, settings.MutationRate, settings.ParameterRanges);
                Mutate(child2, settings.MutationRate, settings.ParameterRanges);

                nextGeneration.Add(child1);
                nextGeneration.Add(child2);
            }

            population = nextGeneration.Take(settings.PopulationSize).ToList();
        }

        return new OptimizationResult
        {
            BestTheory = bestTheory,
            GenerationResults = generationResults
        };
    }

    private List<Theory> InitializePopulation(Theory baseTheory)
    {
        var population = new List<Theory> { baseTheory };

        while (population.Count < PopulationSize)
        {
            var theory = new Theory
            {
                Name = $"{baseTheory.Name}_variant_{population.Count}",
                Symbols = baseTheory.Symbols,
                Indicators = baseTheory.Indicators.Select(i => new Indicator
                {
                    Name = i.Name,
                    Type = i.Type,
                    Parameters = i.Parameters.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value * (1 + ((decimal)_random.NextDouble() * 0.4m - 0.2m)) // ±20% variation
                    )
                }).ToList()
            };

            population.Add(theory);
        }

        return population;
    }

    private List<Theory> InitializePopulationFromTheory(Theory theory, int populationSize, Dictionary<string, ParameterRange> parameterRanges)
    {
        var population = new List<Theory> { theory };

        while (population.Count < populationSize)
        {
            var candidate = new Theory
            {
                Name = $"{theory.Name}_variant_{population.Count}",
                Symbols = theory.Symbols,
                Indicators = theory.Indicators.Select(i => new Indicator
                {
                    Name = i.Name,
                    Type = i.Type,
                    Parameters = i.Parameters.ToDictionary(
                        kvp => kvp.Key,
                        kvp => RandomizeParameter(kvp.Value, parameterRanges[kvp.Key])
                    )
                }).ToList()
            };

            population.Add(candidate);
        }

        return population;
    }

    private decimal RandomizeParameter(decimal value, ParameterRange range)
    {
        var normalizedValue = (value - range.Min) / (range.Max - range.Min);
        var randomizedValue = normalizedValue * (1 + ((decimal)_random.NextDouble() * range.Variation * 2 - range.Variation));
        return range.Min + randomizedValue * (range.Max - range.Min);
    }

    private Theory SelectParent(List<Theory> population, Dictionary<Theory, decimal> fitnessScores)
    {
        // Tournament selection
        var tournament = population.OrderBy(_ => _random.Next()).Take(3).ToList();
        return tournament.OrderByDescending(t => fitnessScores[t]).First();
    }

    private (Theory child1, Theory child2) Crossover(Theory parent1, Theory parent2)
    {
        var child1 = new Theory
        {
            Name = $"{parent1.Name}_offspring",
            Symbols = parent1.Symbols,
            Indicators = new List<Indicator>()
        };

        var child2 = new Theory
        {
            Name = $"{parent2.Name}_offspring",
            Symbols = parent2.Symbols,
            Indicators = new List<Indicator>()
        };

        for (int i = 0; i < parent1.Indicators.Count; i++)
        {
            var indicator1 = parent1.Indicators[i];
            var indicator2 = parent2.Indicators[i];

            child1.Indicators.Add(new Indicator
            {
                Name = indicator1.Name,
                Type = indicator1.Type,
                Parameters = new Dictionary<string, decimal>()
            });

            child2.Indicators.Add(new Indicator
            {
                Name = indicator2.Name,
                Type = indicator2.Type,
                Parameters = new Dictionary<string, decimal>()
            });

            foreach (var param in indicator1.Parameters)
            {
                var crossoverPoint = (decimal)_random.NextDouble();
                child1.Indicators[i].Parameters[param.Key] = crossoverPoint * param.Value + (1 - crossoverPoint) * indicator2.Parameters[param.Key];
                child2.Indicators[i].Parameters[param.Key] = crossoverPoint * indicator2.Parameters[param.Key] + (1 - crossoverPoint) * param.Value;
            }
        }

        return (child1, child2);
    }

    private void Mutate(Theory theory)
    {
        foreach (var indicator in theory.Indicators)
        {
            foreach (var param in indicator.Parameters.Keys.ToList())
            {
                if (_random.NextDouble() < (double)MutationRate)
                {
                    indicator.Parameters[param] *= 1 + ((decimal)_random.NextDouble() * 0.2m - 0.1m); // ±10% mutation
                }
            }
        }
    }

    private void Mutate(Theory theory, decimal mutationRate, Dictionary<string, ParameterRange> parameterRanges)
    {
        foreach (var indicator in theory.Indicators)
        {
            foreach (var param in indicator.Parameters.Keys.ToList())
            {
                if (_random.NextDouble() < (double)mutationRate)
                {
                    indicator.Parameters[param] = RandomizeParameter(indicator.Parameters[param], parameterRanges[param]);
                }
            }
        }
    }

    private decimal CalculateFitness(BacktestResult result)
    {
        // Multi-objective fitness function
        var returnComponent = (result.FinalEquity - 10000) / 10000; // Normalized returns
        var drawdownPenalty = result.MaxDrawdown * 2; // Penalize large drawdowns
        var consistencyBonus = result.SharpeRatio / 2; // Reward consistency
        var profitFactorBonus = Math.Min(result.ProfitFactor, 3) / 3; // Reward good profit factor, capped at 3

        return returnComponent - drawdownPenalty + consistencyBonus + profitFactorBonus;
    }
}
