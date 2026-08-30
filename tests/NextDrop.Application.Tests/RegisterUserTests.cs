using FluentAssertions;
using Moq;
using NextDrop.Modules.Identity.Application.Abstractions;
using NextDrop.Modules.Identity.Application.Commands.RegisterUser;
using NextDrop.Modules.Identity.Domain.Aggregates.User;
using NextDrop.SharedKernel.Abstractions;
using Xunit;

namespace NextDrop.Application.Tests;

public class RegisterUserTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly RegisterUserCommandHandler _handler;
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    public RegisterUserTests()
    {
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(FixedNow);
        _tokenServiceMock.Setup(x => x.GenerateSecureToken()).Returns("raw-token");
        _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashed-token");
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>())).Returns("hashed-password");

        _handler = new RegisterUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _dateTimeProviderMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithUniqueEmail_ShouldCreateUser_AndSendEmail()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegisterUserCommand("newuser@nextdrop.com", "Password123!", "John", "Doe", "+1234567890");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("newuser@nextdrop.com");
        result.Value.Status.Should().Be("Unverified");

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendVerificationEmailAsync("newuser@nextdrop.com", "raw-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReturnConflictError()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new RegisterUserCommand("existing@nextdrop.com", "Password123!", "John", "Doe", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailNotUnique");
    }
}
