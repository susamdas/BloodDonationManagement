using Microsoft.AspNetCore.SignalR;

namespace BloodDonationManagement.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinDistrictGroup(string districtId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"district_{districtId}");
    }

    public async Task LeaveDistrictGroup(string districtId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"district_{districtId}");
    }
}
