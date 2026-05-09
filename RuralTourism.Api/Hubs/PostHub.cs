using Microsoft.AspNetCore.SignalR;

namespace RuralTourism.Api.Hubs;

public class PostHub : Hub
{
    public async Task JoinPostGroup(string postId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, postId);
    }

    public async Task LeavePostGroup(string postId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, postId);
    }

    public async Task JoinListGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "PostList");
    }

    public async Task LeaveListGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "PostList");
    }
}

