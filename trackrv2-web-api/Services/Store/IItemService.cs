using BadmintonKiosken.Shared.DTOs.Item;

namespace BadmintonKiosken.Api.Services;

public interface IItemService
{
    Task<ItemResponse> AddItemAsync(ItemRequest itemRequest);
}