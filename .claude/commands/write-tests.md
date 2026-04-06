# /write-tests

Generate an NUnit + Moq test class for a given handler, service, or class.

## Usage
```
/write-tests <ClassName>
```

## Examples
```
/write-tests CreateOrderCommandHandler
/write-tests GetOrderByIdQueryHandler
/write-tests OrderService
/write-tests PricingCalculator
```

## Instructions

You are generating a skeleton NUnit + Moq test class. Follow these steps in order.

---

### Step 1 — Locate the class under test

Find `{ClassName}.cs` in the project. Read its constructor dependencies and public methods before generating anything.

---

### Step 2 — Determine the test project and namespace

- Test file path: `tests/{Project}.UnitTests/{MirroredNamespace}/{ClassName}Tests.cs`
- Namespace mirrors the source: if the class is in `Application.Commands.CreateOrder`, the test is in `{Project}.UnitTests.Application.Commands.CreateOrder`.

---

### Step 3 — Generate the test class skeleton

```csharp
namespace {Project}.UnitTests.{MirroredNamespace};

[TestFixture]
public class {ClassName}Tests
{
    // TODO: declare a Mock<T> for each constructor dependency
    // Example:
    // private Mock<IOrderRepository> _orderRepositoryMock;
    // private Mock<ILogger<{ClassName}>> _loggerMock;

    private {ClassName} _sut; // sut = system under test

    [SetUp]
    public void SetUp()
    {
        // TODO: initialise mocks
        // Example:
        // _orderRepositoryMock = new Mock<IOrderRepository>();
        // _loggerMock = new Mock<ILogger<{ClassName}>>();

        // TODO: construct the sut with mocked dependencies
        // Example:
        // _sut = new {ClassName}(_orderRepositoryMock.Object, _loggerMock.Object);
        throw new NotImplementedException();
    }

    // ---------------------------------------------------------------
    // TODO: generate one test method per scenario per public method.
    // Use the naming convention:
    //   {MethodName}_{Scenario}_{ExpectedOutcome}
    // ---------------------------------------------------------------

    // Example for a command handler:

    [Test]
    public async Task Handle_WhenEntityNotFound_ReturnsFailureResult()
    {
        // Arrange
        // TODO: set up mock to return null / empty
        // var command = new {RelatedCommand}(Guid.NewGuid());

        // Act
        // var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        // Assert.That(result.IsFailure, Is.True);
        // Assert.That(result.Error, Is.EqualTo("expected error message"));
        throw new NotImplementedException();
    }

    [Test]
    public async Task Handle_WhenValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        // TODO: set up mocks to return valid data

        // Act
        // var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        // Assert.That(result.IsSuccess, Is.True);
        throw new NotImplementedException();
    }

    [Test]
    public async Task Handle_WhenValidRequest_CallsRepositoryOnce()
    {
        // Arrange
        // TODO: set up mocks

        // Act
        // await _sut.Handle(command, CancellationToken.None);

        // Assert — verify only interactions that matter
        // _orderRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        throw new NotImplementedException();
    }
}
```

---

### Step 4 — Remind the user what to do next

```
Next steps:
[ ] Replace all throw new NotImplementedException() with real arrange/act/assert
[ ] Add [TestCase] attributes for parameterised edge cases
[ ] Add tests for any remaining public methods not yet covered
[ ] Run: dotnet test --filter FullyQualifiedName~{ClassName}Tests
```

## Rules
- Always read the actual class before generating — base the mocks on real constructor dependencies.
- Never mock types you don't own (e.g. `DbContext`, `HttpClient`). Flag these and suggest a wrapper/fake instead.
- One assertion concept per test — multiple `Assert.That` calls are fine if they verify the same outcome.
- Use `It.IsAny<T>()` for parameters you don't care about in a specific test. Only use `It.Is<T>(x => ...)` when the specific value matters to that test's intent.
- Use `Times.Once`, `Times.Never`, or `Times.Exactly(n)` when verifying calls — not `Times.AtLeastOnce` unless truly required.
- Do not generate tests that only test the mocking framework (e.g. a test that just verifies a mock was set up).
- Use `CancellationToken.None` in tests — never create real cancellation tokens unless testing cancellation behaviour.
