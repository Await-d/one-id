# OneID Tests

This directory contains unit tests and integration tests for the OneID platform.

## Test Projects

### OneID.Identity.Tests
Unit and integration tests for the Identity Server:
- **Services Tests**: Email service, database seeder
- **Controller Tests**: Account controller, email confirmation
- **Domain Tests**: User entity, validation logic

### OneID.AdminApi.Tests
Integration tests for the Admin API:
- **Controller Tests**: Roles, users, clients, email configuration
- **Service Tests**: Role service, user query/command services
- **Integration Tests**: Full API endpoint testing with test database

## Test Coverage

The test suite covers:
- ✅ **Email Service**: Database-backed configuration, SMTP, SendGrid
- ✅ **Role Management**: CRUD operations, user-role assignments
- ✅ **Email Verification**: Registration, confirmation, resend
- ✅ **Authentication**: Register, login, password reset
- ✅ **API Endpoints**: All major Admin API endpoints
- ✅ **Security**: Password encryption, data protection

## Running Tests

### Quick Start

**Linux/macOS:**
```bash
cd backend/tests
chmod +x run-tests.sh
./run-tests.sh
```

**Windows:**
```powershell
cd backend\tests
.\run-tests.ps1
```

### Manual Testing

**Run all tests:**
```bash
cd backend
dotnet test
```

**Run specific test project:**
```bash
cd backend/tests/OneID.Identity.Tests
dotnet test

cd backend/tests/OneID.AdminApi.Tests
dotnet test
```

**Run with code coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Run specific test class:**
```bash
dotnet test --filter "FullyQualifiedName~EmailServiceTests"
```

**Run specific test method:**
```bash
dotnet test --filter "FullyQualifiedName~EmailServiceTests.SendEmailAsync_WithNoConfiguration_ShouldLogWarning"
```

## Code Coverage

To generate a detailed code coverage report:

1. Install ReportGenerator:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

2. Run tests with coverage:
```bash
cd backend
dotnet test --collect:"XPlat Code Coverage"
```

3. Generate HTML report:
```bash
reportgenerator \
  -reports:**/coverage.cobertura.xml \
  -targetdir:coverage-report \
  -reporttypes:Html
```

4. Open the report:
```bash
# Linux/macOS
open coverage-report/index.html

# Windows
start coverage-report\index.html
```

## Test Database

Tests use **in-memory SQLite databases** for isolation and speed. Each test class gets its own database instance that is automatically cleaned up after tests complete.

## Continuous Integration

The test suite is designed to run in CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Tests
  run: |
    cd backend
    dotnet test --configuration Release \
      --logger "console;verbosity=normal" \
      --collect:"XPlat Code Coverage"
```

## Writing New Tests

### Unit Test Example

```csharp
[Fact]
public async Task MyService_Method_ShouldReturnExpectedResult()
{
    // Arrange
    var service = new MyService(_dependencies);
    
    // Act
    var result = await service.MethodAsync();
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedValue, result.Property);
}
```

### Integration Test Example

```csharp
[Collection("Integration")]
public class MyControllerTests : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client;
    
    public MyControllerTests(AdminApiFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/myendpoint");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

## Test Naming Convention

- **Method Under Test**: `MethodName_Scenario_ExpectedBehavior`
- **Examples**:
  - `CreateRole_WithValidData_ShouldReturnCreated`
  - `UpdateUser_WithInvalidId_ShouldReturnNotFound`
  - `SendEmail_WithNoConfig_ShouldLogWarning`

## Troubleshooting

### Tests fail with "Database is locked"
- This can happen with SQLite in-memory databases
- Each test class should use a unique database name
- Solution: Use `Guid.NewGuid()` in database name

### Tests fail with "Port already in use"
- Integration tests may conflict if run in parallel
- Solution: Use `[Collection("Integration")]` attribute to run sequentially

### Missing dependencies
- Run `dotnet restore` in the test project directory
- Check that all NuGet packages are installed

## Test Statistics

- **Total Test Projects**: 2
- **Test Categories**:
  - Unit Tests: Services, domain logic
  - Integration Tests: API endpoints, database operations
  - Security Tests: Authentication, authorization
  
- **Key Test Files**:
  - `EmailServiceTests.cs`: Email service unit tests
  - `RoleServiceTests.cs`: Role management unit tests
  - `AccountControllerTests.cs`: Authentication integration tests
  - `RolesControllerTests.cs`: Role API integration tests
  - `EmailConfigurationControllerTests.cs`: Email config API tests

## Contributing

When adding new features, please:
1. Write unit tests for business logic
2. Write integration tests for API endpoints
3. Ensure all tests pass before committing
4. Aim for >80% code coverage
5. Follow existing test patterns and naming conventions

