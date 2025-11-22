using Audio3A.Core;
using Audio3A.RoomManagement;
using Audio3A.RoomManagement.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Audio3A.RoomManagement.Tests;

/// <summary>
/// RoomManager 单元测试
/// </summary>
public class RoomManagerTests
{
    private readonly RoomManager _roomManager;
    private readonly IServiceProvider _serviceProvider;

    public RoomManagerTests()
    {
        _serviceProvider = new ServiceCollection()
            .BuildServiceProvider();
        _roomManager = new RoomManager(NullLogger<RoomManager>.Instance, _serviceProvider);
    }

    [Fact]
    public void CreateRoom_ShouldSucceed()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };

        // Act
        var room = _roomManager.CreateRoom("test-room", "Test Room", config);

        // Assert
        Assert.NotNull(room);
        Assert.Equal("test-room", room.Id);
        Assert.Equal("Test Room", room.Name);
        Assert.Equal(RoomState.Active, room.State);
    }

    [Fact]
    public void CreateRoom_WithDuplicateId_ShouldThrow()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };
        _roomManager.CreateRoom("dup-room", "Room 1", config);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _roomManager.CreateRoom("dup-room", "Room 2", config));
    }

    [Fact]
    public void GetRoom_ExistingRoom_ShouldReturnRoom()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };
        var created = _roomManager.CreateRoom("room-1", "Room 1", config);

        // Act
        var retrieved = _roomManager.GetRoom("room-1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public void GetRoom_NonExistingRoom_ShouldReturnNull()
    {
        // Act
        var room = _roomManager.GetRoom("non-existing");

        // Assert
        Assert.Null(room);
    }

    [Fact]
    public void RemoveRoom_ExistingRoom_ShouldSucceed()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };
        _roomManager.CreateRoom("room-1", "Room 1", config);

        // Act
        var result = _roomManager.RemoveRoom("room-1");

        // Assert
        Assert.True(result);
        Assert.Null(_roomManager.GetRoom("room-1"));
    }

    [Fact]
    public void JoinRoom_ValidParticipant_ShouldSucceed()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };
        var room = _roomManager.CreateRoom("room-1", "Room 1", config);
        var participant = new Participant("user-1", "User 1", room.Id, TransportProtocol.WebSocket);

        // Act
        var result = _roomManager.JoinRoom(room.Id, participant);

        // Assert
        Assert.True(result);
        Assert.Equal(1, room.ParticipantCount);
        Assert.Equal(ParticipantState.Connected, participant.State);
    }

    [Fact]
    public void LeaveRoom_ExistingParticipant_ShouldSucceed()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };
        var room = _roomManager.CreateRoom("room-1", "Room 1", config);
        var participant = new Participant("user-1", "User 1", room.Id, TransportProtocol.WebSocket);
        _roomManager.JoinRoom(room.Id, participant);

        // Act
        var result = _roomManager.LeaveRoom(room.Id, participant.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(0, room.ParticipantCount);
    }

    [Fact]
    public void GetAllRooms_ShouldReturnAllRooms()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };
        _roomManager.CreateRoom("room-1", "Room 1", config);
        _roomManager.CreateRoom("room-2", "Room 2", config);

        // Act
        var rooms = _roomManager.GetAllRooms();

        // Assert
        Assert.Equal(2, rooms.Count);
    }

    [Fact]
    public void CleanupEmptyRooms_ShouldRemoveEmptyRooms()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };
        var room1 = _roomManager.CreateRoom("room-1", "Room 1", config);
        var room2 = _roomManager.CreateRoom("room-2", "Room 2", config);
        
        var participant = new Participant("user-1", "User 1", room2.Id, TransportProtocol.WebSocket);
        _roomManager.JoinRoom(room2.Id, participant);

        // Act
        var cleaned = _roomManager.CleanupEmptyRooms();

        // Assert
        Assert.Equal(1, cleaned); // Only room-1 should be cleaned
        Assert.Null(_roomManager.GetRoom("room-1"));
        Assert.NotNull(_roomManager.GetRoom("room-2"));
    }

    [Fact]
    public void TotalParticipantCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var config = new Audio3AConfig { SampleRate = 16000 };
        var room1 = _roomManager.CreateRoom("room-1", "Room 1", config);
        var room2 = _roomManager.CreateRoom("room-2", "Room 2", config);

        var p1 = new Participant("user-1", "User 1", room1.Id, TransportProtocol.WebSocket);
        var p2 = new Participant("user-2", "User 2", room1.Id, TransportProtocol.WebSocket);
        var p3 = new Participant("user-3", "User 3", room2.Id, TransportProtocol.WebSocket);

        _roomManager.JoinRoom(room1.Id, p1);
        _roomManager.JoinRoom(room1.Id, p2);
        _roomManager.JoinRoom(room2.Id, p3);

        // Act
        var total = _roomManager.TotalParticipantCount;

        // Assert
        Assert.Equal(3, total);
    }
}
