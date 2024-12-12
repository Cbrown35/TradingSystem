using System;
using System.Collections.Generic;
using TradingSystem.Common.Models;

namespace TradingSystem.Common.Models;

public class OptimizationResult
{
    public Theory BestTheory { get; set; } = null!;
    public BacktestResult BacktestResult { get; set; } = null!;
    public List<GenerationResult> GenerationResults { get; set; } = new();
    public TimeSpan OptimizationTime { get; set; }
    public decimal InitialFitness { get; set; }
    public decimal FinalFitness { get; set; }
    public decimal ImprovementPercentage { get; set; }
}

public class GenerationResult
{
    public int GenerationNumber { get; set; }
    public decimal BestFitness { get; set; }
    public decimal AverageFitness { get; set; }
    public decimal WorstFitness { get; set; }
}

public class OptimizationSettings
{
    public int Generations { get; set; }
    public int PopulationSize { get; set; }
    public int InitialPopulationSize { get; set; }
    public decimal MutationRate { get; set; }
    public Dictionary<string, ParameterRange> ParameterRanges { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class ParameterRange
{
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Variation { get; set; }
}
