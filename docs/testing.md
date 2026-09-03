# Testing

This document covers the testing setup, how to run tests, and how to write new tests.

## Test Framework

| Library | Version | Purpose |
|---------|---------|---------|
| **xUnit** | 2.8.1 | Test framework |
| **Moq** | 4.16.1 | Mocking library |
| **Microsoft.NET.Test.Sdk** | 17.8.0 | Test host |

All tests target **.NET Framework 4.8** (same as production code).

## Running Tests

### From Command Line

```bash
# Restore and run all tests
dotnet test RommPlugin.Tests/RommPlugin.Tests.csproj --configuration Release --verbosity normal

# Run with detailed output
dotnet test RommPlugin.Tests/RommPlugin.Tests.csproj --configuration Debug --verbosity detailed

# Run specific test
dotnet test RommPlugin.Tests/RommPlugin.Tests.csproj --filter "FullyQualifiedName~RommApiClientTests"

# Run tests matching a trait
dotnet test RommPlugin.Tests/RommPlugin.Tests.csproj --filter "Category=Unit"
```

### From Visual Studio

1. Open Test Explorer (Test → Test Explorer)
2. Build the solution
3. Tests should appear automatically
4. Run all or select specific tests

## Test Project Structure

```
RommPlugin.Tests/
├── LocaleFixture.cs              # xUnit fixture: initializes locale system
├── LocaleCollectionDefinition.cs # Shared collection for locale tests
│
├── Helpers/
│   ├── MockHttpMessageHandler.cs # Reusable HTTP mock for API tests
│   ├── AuthHeaderHelperTests.cs
│   ├── RommGameHelpersTests.cs
│   ├── SafeFileWriterTests.cs
│   └── SecureCredentialStorageTests.cs
│
├── Models/
│   ├── RommPluginSettingsTests.cs
│   ├── RommStatsTests.cs
│   ├── RommScreenshotTests.cs
│   ├── PlaySessionModelsTests.cs
│   ├── LaunchBoxMetadataModelTests.cs
│   └── DownloadItemTests.cs
│
└── Services/
    ├── RommApiClientTests.cs
    ├── RommMetadataMapperTests.cs
    ├── RommConnectionTesterTests.cs
    ├── RommSyncServiceStatsTests.cs
    ├── InstalledGamesServiceTests.cs
    └── LocaleManagerTests.cs
```

## Writing Tests

### Basic Test Structure

```csharp
using Xunit;
using Moq;

namespace RommPlugin.Tests.Services
{
    public class MyServiceTests
    {
        [Fact]
        public void MethodName_WhenCondition_ExpectedResult()
        {
            // Arrange
            var mockDependency = new Mock<IDependency>();
            mockDependency.Setup(x => x.Method()).Returns(expectedValue);
            var service = new MyService(mockDependency.Object);

            // Act
            var result = service.DoSomething();

            // Assert
            Assert.Equal(expectedValue, result);
        }
    }
```

### Naming Convention

Follow the pattern: `MethodName_WhenCondition_ExpectedResult`

```csharp
[Fact]
public void SyncAsync_WhenServerUnavailable_ThrowsException()

[Fact]
public void GetMetadata_WhenKeepLocalDataTrue_OnlyFillsEmptyFields()

[Theory]
[InlineData("")]
[InlineData(null)]
public void ValidateUrl_WhenEmptyOrNullOr_ThrowsArgumentException(string url)
```

### Using Moq for Mocking

```csharp
// Mock a service
var mockSyncService = new Mock<IRommSyncService>();
mockSyncService
    .Setup(x => x.SyncAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(new SyncResult { Success = true });

// Verify a method was called
mockSyncService.Verify(x => x.SyncAsync(It.IsAny<CancellationToken>()), Times.Once);

// Verify with specific argument
mockSyncService.Verify(x => x.SaveState(It.Is<SyncInformation>(
    s => s.CompletedPlatformIds.Count == 3
)), Times.Once);
```

### MockHttpMessageHandler

The test project includes a reusable `MockHttpMessageHandler` for testing HTTP clients without real network calls:

```csharp
var mockHandler = new MockHttpMessageHandler();
mockHandler.SetupRequest("https://api.example.com/data")
    .ReturnsResponse(HttpStatusCode.OK, "{\"key\": \"value\"}");

var client = new HttpClient(mockHandler.Object);
// ... test with client
```

### Locale Fixture

Tests that use the locale system need the `LocaleFixture`:

```csharp
[Collection("Locale")]
public class LocaleAwareTests
{
    [Fact]
    public void GetString_ReturnsCorrectTranslation()
    {
        var result = LocaleManager.GetString("sync_complete");
        Assert.NotNull(result);
    }
}
```

## Test Categories

### Unit Tests

- **Helpers/** - Test utility functions (auth headers, file writing, credential storage)
- **Models/** - Test model properties, serialization, validation
- **Services/** - Test service logic with mocked dependencies

### What to Test

| Area | What to Verify |
|------|---------------|
| API Client | Request construction, authentication headers, response parsing |
| Metadata Mapper | Field mapping, priority rules, null handling |
| Settings Model | Default values, serialization round-trip |
| Download Queue | State persistence, retry logic, concurrent access |
| Installed Games | CRUD operations, file persistence |
| Locale Manager | Key lookup, fallback to English, missing keys |

### What NOT to Test

- UI forms (Windows Forms, hard to unit test)
- LaunchBox API integration (vendored DLL, no interfaces)
- File system operations that are already tested by the framework

## Adding New Tests

1. Create a test file in the appropriate folder (`Helpers/`, `Models/`, or `Services/`)
2. Name the file `{ClassName}Tests.cs`
3. Follow the naming convention for test methods
4. Use Moq for dependencies
5. Ensure tests are independent (no shared state)
6. Run all tests before submitting PR

## Test Configuration

The test project has special build configuration:

- `AutoGenerateBindingRedirects=false` - Prevents automatic binding redirect generation
- `GenerateBindingRedirects=false` - Explicitly disabled
- `FixBindingRedirects` target copies `App.config` to the test output

This is necessary because the test project targets net48 and needs specific binding redirects for `System.Drawing.Common`.
