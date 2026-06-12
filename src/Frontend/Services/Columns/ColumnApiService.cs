using Contracts.DTOs;
using Contracts.Requests;
using Services.Abstractions.Columns;
using Services.Api;

namespace Services.Columns;

public class ColumnApiService(IColumnsApi columnsApi) : IColumnApiService
{
    public async Task<ColumnDto> CreateColumnAsync(Guid boardId, string name, CancellationToken ct = default)
    {
        var request = new CreateColumnRequest(name);
        var response = await columnsApi.CreateAsync(boardId, request, ct);

        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            throw new Exception($"Failed to create column: {response.Error?.Message}");
        }

        return response.Content;
    }

    public async Task<ColumnDto> UpdateColumnAsync(Guid boardId, Guid columnId, string name, CancellationToken ct = default)
    {
        var request = new UpdateColumnRequest(name);
        var response = await columnsApi.UpdateAsync(boardId, columnId, request, ct);

        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            throw new Exception($"Failed to update column: {response.Error?.Message}");
        }

        return response.Content;
    }

    public async Task DeleteColumnAsync(Guid boardId, Guid columnId, CancellationToken ct = default)
    {
        var response = await columnsApi.DeleteAsync(boardId, columnId, ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to delete column: {response.Error?.Message}");
        }
    }

    public async Task MoveColumnAsync(Guid boardId, Guid columnId, int newPosition, CancellationToken ct = default)
    {
        var response = await columnsApi.MoveAsync(boardId, columnId, newPosition, ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to move column: {response.Error?.Message}");
        }
    }
}