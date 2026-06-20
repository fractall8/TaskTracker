using Contracts.DTOs;
using Contracts.Requests;
using Services.Abstractions.Columns;
using Services.Api;
using Services.Extensions;

namespace Services.Columns;

public class ColumnApiService(IColumnsApi columnsApi) : IColumnApiService
{
    public async Task<ColumnDto> CreateColumnAsync(Guid boardId, string name, CancellationToken ct = default)
    {
        var request = new CreateColumnRequest(name);
        var response = await columnsApi.CreateAsync(boardId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task<ColumnDto> UpdateColumnAsync(Guid boardId, Guid columnId, string name, CancellationToken ct = default)
    {
        var request = new UpdateColumnRequest(name);
        var response = await columnsApi.UpdateAsync(boardId, columnId, request, ct);
        return await response.HandleResponseAsync();
    }

    public async Task DeleteColumnAsync(Guid boardId, Guid columnId, CancellationToken ct = default)
    {
        var response = await columnsApi.DeleteAsync(boardId, columnId, ct);
        await response.HandleResponseAsync();
    }

    public async Task MoveColumnAsync(Guid boardId, Guid columnId, int newPosition, CancellationToken ct = default)
    {
        var response = await columnsApi.MoveAsync(boardId, columnId, newPosition, ct);
        await response.HandleResponseAsync();
    }
}
