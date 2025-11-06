# Contributing to Orbit AI Simulation

Thank you for your interest in contributing to Orbit AI Simulation! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Features](#suggesting-features)

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inclusive environment for all contributors, regardless of background, identity, or experience level.

### Expected Behavior

- Be respectful and considerate
- Welcome newcomers and help them get started
- Accept constructive criticism gracefully
- Focus on what is best for the community and the project

### Unacceptable Behavior

- Harassment, discrimination, or offensive comments
- Publishing others' private information
- Trolling, insulting, or derogatory comments
- Any conduct inappropriate in a professional setting

## Getting Started

### Prerequisites

Before contributing, make sure you have:

1. **Unity 6000.0.47f1** installed
2. **Git** configured on your machine
3. A **GitHub account**
4. **API keys** for OpenAI and ElevenLabs (for testing)
5. Optionally, a **Meta Quest** device for VR testing

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/orbitAIsimulation.git
   cd orbitAIsimulation
   ```
3. Add the upstream repository:
   ```bash
   git remote add upstream https://github.com/ORIGINAL_OWNER/orbitAIsimulation.git
   ```

### Set Up Your Development Environment

1. Open the project in Unity
2. Configure your API keys (see [README.md](README.md) for instructions)
3. Verify the project builds without errors

## How to Contribute

### Types of Contributions

We welcome various types of contributions:

- **Bug fixes**: Fix issues reported in GitHub Issues
- **New features**: Implement features from the roadmap or propose new ones
- **Documentation**: Improve README, code comments, or create tutorials
- **Code refactoring**: Improve code quality and organization
- **Testing**: Add unit tests or improve test coverage
- **UI/UX improvements**: Enhance user interface and experience
- **3D models**: Add or improve mission assets
- **Mission content**: Create new missions or improve existing ones

### Areas That Need Help

Check GitHub Issues for:
- Issues labeled `good first issue` - Great for newcomers
- Issues labeled `help wanted` - Community assistance needed
- Issues labeled `bug` - Reported bugs needing fixes

## Development Workflow

### 1. Create a Branch

Always create a new branch for your work:

```bash
git checkout -b feature/your-feature-name
```

Branch naming conventions:
- `feature/` - New features or enhancements
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring
- `test/` - Testing improvements

Examples:
- `feature/add-mars-mission`
- `fix/orbit-calculation-error`
- `docs/improve-setup-guide`

### 2. Make Your Changes

- Write clear, readable code
- Follow existing code style and conventions
- Add comments for complex logic
- Update documentation as needed

### 3. Test Your Changes

- **In-Editor**: Test in Unity Editor Play mode
- **PC Build**: Build and test standalone
- **VR**: If possible, test on Meta Quest device
- **API Integration**: Verify AI conversations work correctly

### 4. Commit Your Changes

Write clear, descriptive commit messages:

```bash
git add .
git commit -m "Add Mars rover mission with orbital mechanics"
```

**Good commit messages:**
- `Fix orbit calculation precision error`
- `Add voice command support for time acceleration`
- `Refactor OrbitController for better performance`
- `Update README with troubleshooting section`

**Bad commit messages:**
- `Update`
- `Fixed stuff`
- `Changes`

### 5. Keep Your Branch Updated

Regularly sync with the upstream repository:

```bash
git fetch upstream
git rebase upstream/main
```

### 6. Push Your Changes

```bash
git push origin feature/your-feature-name
```

## Coding Standards

### C# Style Guide

Follow Unity's C# conventions:

```csharp
// Good: PascalCase for public members
public class OrbitController : MonoBehaviour
{
    // Good: PascalCase for public properties
    public float AltitudeKm { get; set; }

    // Good: camelCase with underscore prefix for private fields
    private float _orbitRadius;

    // Good: Clear, descriptive names
    public void CalculateOrbitalVelocity()
    {
        // Implementation
    }

    // Good: XML doc comments for public APIs
    /// <summary>
    /// Adjusts the orbital altitude to the specified value.
    /// </summary>
    /// <param name="newAltitudeKm">Target altitude in kilometers</param>
    public void SetAltitude(float newAltitudeKm)
    {
        // Implementation
    }
}
```

### File Organization

- One class per file
- File name matches class name
- Place files in appropriate folders:
  - `Assets/Scripts/AI/` - AI-related code
  - `Assets/Scripts/Core/` - Core systems
  - `Assets/Scripts/Orbital/` - Orbital mechanics
  - `Assets/Scripts/UI/` - User interface

### Unity-Specific Guidelines

- Use `[SerializeField]` for inspector-visible private fields
- Avoid `public` fields unless necessary
- Cache component references in `Start()` or `Awake()`
- Use `GameObject.Find()` sparingly (expensive)
- Prefer ScriptableObjects for configuration

Example:
```csharp
public class ExampleScript : MonoBehaviour
{
    [SerializeField] private Transform _target;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        // Cache expensive GetComponent calls
        _rigidbody = GetComponent<Rigidbody>();
    }
}
```

### API Key Security

**CRITICAL**: Never commit API keys!

- Use environment variables or local config files
- Never hardcode keys in source code
- Double-check before committing:
  ```bash
  git diff --cached | grep -i "api.*key"
  ```

## Testing Guidelines

### Manual Testing Checklist

Before submitting a PR, test:

- [ ] Unity Editor Play mode works without errors
- [ ] All scenes load correctly
- [ ] AI conversations respond appropriately
- [ ] Orbital mechanics calculations are accurate
- [ ] UI is responsive and functional
- [ ] No console errors or warnings
- [ ] VR interactions work on Quest (if applicable)

### Testing AI Features

If you don't have API keys, you can:
- Request test keys from maintainers
- Test with mock responses
- Focus on non-AI features

### Performance Testing

- Check frame rate in VR (should be 72+ FPS on Quest)
- Profile with Unity Profiler
- Test on lower-end hardware if possible

## Pull Request Process

### Before Submitting

1. Ensure your code follows the coding standards
2. Test thoroughly in multiple scenarios
3. Update documentation if needed
4. Rebase on the latest `main` branch
5. Verify no merge conflicts exist

### Submitting the PR

1. Go to your fork on GitHub
2. Click **Pull Request**
3. Select your branch and write a clear description

**PR Template:**

```markdown
## Description
Brief description of what this PR does.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Documentation update
- [ ] Code refactoring
- [ ] Other (please describe)

## Testing
Describe how you tested your changes:
- Unity Editor: ✅
- PC Build: ✅
- VR Device: ✅ (or N/A)

## Screenshots (if applicable)
Add screenshots or videos of your changes.

## Checklist
- [ ] My code follows the project's coding standards
- [ ] I have tested my changes thoroughly
- [ ] I have updated documentation as needed
- [ ] My commits have clear, descriptive messages
- [ ] I have added comments for complex code
- [ ] No API keys or sensitive data in commits
```

### PR Review Process

1. A maintainer will review your PR
2. Address any requested changes
3. Once approved, your PR will be merged
4. Your contribution will be credited in the project!

### After Your PR is Merged

1. Delete your feature branch:
   ```bash
   git branch -d feature/your-feature-name
   git push origin --delete feature/your-feature-name
   ```

2. Update your local main branch:
   ```bash
   git checkout main
   git pull upstream main
   ```

## Reporting Bugs

### Before Reporting

1. Check if the bug has already been reported
2. Verify it's not a configuration issue
3. Test with the latest version of the project

### Bug Report Template

```markdown
**Describe the bug**
A clear description of the bug.

**To Reproduce**
Steps to reproduce:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
What you expected to happen.

**Screenshots/Videos**
If applicable, add media to help explain.

**Environment:**
- Unity Version: [e.g., 6000.0.47f1]
- Platform: [e.g., Meta Quest 3, PC, Mac]
- OS: [e.g., Windows 11, macOS 14]

**Console Errors**
Paste any relevant error messages.

**Additional context**
Any other information about the problem.
```

## Suggesting Features

### Feature Request Template

```markdown
**Is your feature request related to a problem?**
Describe the problem.

**Describe the solution you'd like**
Clear description of what you want to happen.

**Describe alternatives you've considered**
Other solutions or features you've thought about.

**Implementation ideas**
If you have technical ideas, describe them here.

**Additional context**
Screenshots, mockups, or other relevant information.
```

## Questions?

If you have questions about contributing:

- Open a GitHub Discussion
- Contact the maintainers: emzindani@gmail.com
- Check existing Issues and PRs for similar questions

## Recognition

Contributors will be:
- Listed in the project's Contributors section
- Credited in release notes
- Acknowledged in academic publications (if significant contribution)

Thank you for contributing to Orbit AI Simulation! 🚀
