using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Domain.Common;
using Auth.Domain.Exceptions;
using Auth.Domain.Models;
using Auth.Infrastructure.DataAccess;
using Auth.Infrastructure.Repositories.UserRepository;
using Auth.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Auth.IntegrationTests.Repositories;

public class UserRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task RegisterUser_ValidUser_PersistsHashAndRole()
    {
        //Arrange
        await ResetDatabaseAsync();
        var user = new CreatingUserDTO("registered-user", "password", UserRole.Admin);
        var repository = CreateRepository(CreateContext());

        //Act
        await repository.RegisterUserAsync(user);

        //Assert
        await using var context = CreateContext();
        var savedUser = await context.Users.SingleAsync();
        Assert.Equal(user.Login, savedUser.Login);
        Assert.Equal(UserRole.Admin, savedUser.Role);
        Assert.Equal(new Hasher().GetHash(user.Password), savedUser.PasswordHash);
    }

    [Fact]
    public async Task LoginUser_ValidCredentials_ReturnsGeneratedToken()
    {
        //Arrange
        await ResetDatabaseAsync();
        var password = "password";
        var user = new User("existing-user", new Hasher().GetHash(password), UserRole.User);
        await using (var context = CreateContext())
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
        }

        const string token = "generated-token";
        var repository = CreateRepository(CreateContext(), new StubTokenGenerator(token));

        //Act
        var result = await repository.LoginUserAsync(new LoginUserDTO
        {
            Login = user.Login,
            Password = password
        });

        //Assert
        Assert.Equal(token, result);
    }

    [Fact]
    public async Task LoginUser_InvalidCredentials_UsesGenericErrorMessage()
    {
        //Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository(CreateContext());

        //Act
        var exception = await Assert.ThrowsAsync<UserValidationException>(
            () => repository.LoginUserAsync(new LoginUserDTO
            {
                Login = "missing-user",
                Password = "password"
            }));

        //Assert
        Assert.Equal("Неправильные логин или пароль", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Database_UserRequiredFields_RejectsNullValue(bool nullLogin)
    {
        //Arrange
        await ResetDatabaseAsync();
        var user = nullLogin
            ? new User(null!, "hash", UserRole.User)
            : new User("valid-user", null!, UserRole.User);
        await using var context = CreateContext();

        //Act
        await context.Users.AddAsync(user);

        //Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_UserLoginLongerThanMaximum_RejectsValue()
    {
        //Arrange
        await ResetDatabaseAsync();
        var user = new User(new string('a', 251), "hash", UserRole.User);
        await using var context = CreateContext();

        //Act
        await context.Users.AddAsync(user);

        //Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_UserPasswordHashLongerThanMaximum_RejectsValue()
    {
        //Arrange
        await ResetDatabaseAsync();
        var user = new User("valid-user", new string('a', 251), UserRole.User);
        await using var context = CreateContext();

        //Act
        await context.Users.AddAsync(user);

        //Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE users RESTART IDENTITY CASCADE");
    }

    private static UserRepository CreateRepository(AppDbContext context, ITokenGenerator? tokenGenerator = null)
    {
        return new UserRepository(context, new Hasher(), tokenGenerator ?? new StubTokenGenerator("token"));
    }

    private sealed class StubTokenGenerator(string token) : ITokenGenerator
    {
        public string GenerateToken(UserDTO user)
        {
            return token;
        }
    }
}
