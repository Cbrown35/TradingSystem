using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface ITheoryGenerator
{
    Task<Theory> GenerateTheory(string[] symbols);
    Task<List<Theory>> GenerateTheories(string[] symbols, int count);
    Task<Theory> MutateTheory(Theory theory);
    Task<Theory> CrossoverTheories(Theory theory1, Theory theory2);
}
