# Contributing to Trading System

Thank you for your interest in contributing to the Trading System project! This document provides guidelines and instructions for contributing.

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code.

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in the Issues section
2. If not, create a new issue with:
   - Clear title and description
   - Steps to reproduce
   - Expected vs actual behavior
   - System information
   - Code samples if applicable

### Suggesting Enhancements

1. Check if the enhancement has been suggested in Issues
2. If not, create a new issue with:
   - Clear title and description
   - Rationale for the enhancement
   - Example use cases
   - Potential implementation approach

### Pull Requests

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests and ensure they pass
5. Update documentation as needed
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to your branch (`git push origin feature/amazing-feature`)
8. Create a Pull Request

## Development Setup

1. Install prerequisites:
   - .NET 8 SDK
   - PostgreSQL 14+
   - Docker (optional)

2. Clone the repository:
```bash
git clone https://github.com/yourusername/tradingsystem.git
cd tradingsystem
```

3. Install dependencies:
```bash
dotnet restore
```

4. Set up the database:
```bash
cd src/Infrastructure
dotnet ef database update
```

5. Run tests:
```bash
dotnet test
```

## Coding Standards

### General Guidelines

1. Follow C# coding conventions
2. Use meaningful names for variables, methods, and classes
3. Write clear comments and documentation
4. Keep methods focused and concise
5. Use dependency injection
6. Write unit tests for new code

### Code Style

```csharp
// Use PascalCase for class names and public members
public class TradeManager
{
    // Use readonly where possible
    private readonly ITradeRepository _tradeRepository;

    // Use dependency injection
    public TradeManager(ITradeRepository tradeRepository)
    {
        _tradeRepository = tradeRepository;
    }

    // Use async/await consistently
    public async Task<Trade> ExecuteTrade(TradeRequest request)
    {
        // Validate parameters
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Implement logic
        var trade = await _tradeRepository.AddTrade(request);
        return trade;
    }
}
```

### Testing Standards

1. Write unit tests for all new code
2. Follow Arrange-Act-Assert pattern
3. Use meaningful test names
4. Mock external dependencies
5. Test edge cases and error conditions

```csharp
[Fact]
public async Task ExecuteTrade_WithValidRequest_ReturnsSuccessfulTrade()
{
    // Arrange
    var request = new TradeRequest { /* ... */ };
    var repository = new Mock<ITradeRepository>();
    var manager = new TradeManager(repository.Object);

    // Act
    var result = await manager.ExecuteTrade(request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(TradeStatus.Executed, result.Status);
}
```

## Documentation

1. Update README.md for significant changes
2. Document public APIs with XML comments
3. Include code examples where helpful
4. Update architecture diagrams if needed

## Git Workflow

1. Create feature branches from `develop`
2. Use meaningful commit messages
3. Keep commits focused and atomic
4. Rebase before submitting PR
5. Squash commits if necessary

### Commit Message Format

```
type(scope): subject

body

footer
```

Types:
- feat: New feature
- fix: Bug fix
- docs: Documentation
- style: Formatting
- refactor: Code restructuring
- test: Adding tests
- chore: Maintenance

Example:
```
feat(trading): add stop loss management

Implement automatic stop loss adjustment based on volatility.
Also includes position sizing calculations.

Closes #123
```

## Review Process

1. All code must be reviewed before merging
2. Address review comments promptly
3. Keep discussions professional
4. Update PR based on feedback
5. Ensure CI/CD passes

## Release Process

1. Version numbers follow SemVer
2. Update CHANGELOG.md
3. Tag releases
4. Create release notes
5. Deploy to staging first

## Getting Help

- Join our Discord channel
- Check the documentation
- Ask in GitHub Issues
- Email the maintainers

## Recognition

Contributors will be:
- Listed in CONTRIBUTORS.md
- Mentioned in release notes
- Credited in documentation

Thank you for contributing to Trading System!
