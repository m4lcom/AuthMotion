using Moq;
using FluentAssertions;
using AuthMotion.Application.Services;
using AuthMotion.Application.Interfaces;
using AuthMotion.Application.DTOs;
using AuthMotion.Application.Exceptions;
using AuthMotion.Domain.Entities;

namespace AuthMotion.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock = new();

    private AuthService CreateSut() => new(
        _userRepositoryMock.Object,
        _jwtTokenGeneratorMock.Object,
        _emailServiceMock.Object,
        _twoFactorServiceMock.Object);

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        // Arrange
        var sut = CreateSut();
        var password = "SecurePassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Email = "valid@test.com",
            PasswordHash = hashedPassword,
            IsEmailVerified = true
        };

        var request = new LoginRequest { Email = user.Email, Password = password };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("fake-jwt-token");

        _jwtTokenGeneratorMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("fake-refresh-token");

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("fake-jwt-token");
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var sut = CreateSut();
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123")
        };

        var request = new LoginRequest { Email = user.Email, Password = "WrongPassword!" };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act
        var act = async () => await sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndSendEmail_WhenInputIsValid()
    {
        // Arrange
        var sut = CreateSut();
        var request = new RegisterRequest
        {
            Email = "newuser@test.com",
            Password = "Password123!"
        };

        // Simulate that the email is not taken
        _userRepositoryMock
            .Setup(x => x.IsRegisteredAsync(request.Email))
            .ReturnsAsync(false);

        // Act
        await sut.RegisterAsync(request);

        // Assert
        // Verify that we checked if the user exists
        _userRepositoryMock.Verify(x => x.IsRegisteredAsync(request.Email), Times.Once);

        // Verify that the user was added to the DB
        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == request.Email)), Times.Once);

        // Verify that the welcome email was sent
        _emailServiceMock.Verify(x => x.SendEmailAsync(
            request.Email,
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
    {
        // Arrange
        var sut = CreateSut();
        var request = new RegisterRequest { Email = "duplicate@test.com", Password = "Password123!" };

        // Simulamos que el repositorio encuentra al usuario
        _userRepositoryMock
            .Setup(x => x.IsRegisteredAsync(request.Email))
            .ReturnsAsync(true);

        // Act
        var act = async () => await sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Email is already registered.");

        // Verificamos que NUNCA se intentó guardar ni mandar mail
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}