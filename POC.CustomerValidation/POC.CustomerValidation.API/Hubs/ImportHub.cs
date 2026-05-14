using Microsoft.AspNetCore.SignalR;
using POC.CustomerValidation.API.Models.DTOs;

namespace POC.CustomerValidation.API.Hubs;

public interface IImportClient
{
    Task ImportStatusChanged(ImportBatchDto batch);
}

public class ImportHub : Hub<IImportClient>
{
    public async Task JoinBatch(string batchId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"import:{batchId}");

    public async Task LeaveBatch(string batchId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"import:{batchId}");
}
