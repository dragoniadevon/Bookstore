using Bookstore.Api.Models;
using Bookstore.Api.Models.DTO;

namespace Bookstore.Api.Services.Interface
{
    // Регистрация и вход
    public interface IAuthService
    {
        Task<ServiceResult<AuthResponseDTO>> RegisterAsync(RegisterDTO dto);
        Task<ServiceResult<AuthResponseDTO>> LoginAsync(LoginDTO dto);
        Task<UserDTO?> GetUserAsync(int userId);
    }

    // Корзина пользователя. userId берётся из токена в контроллере —
    // пользователь может работать только со своей корзиной.
    public interface ICartService
    {
        Task<CartDTO> GetAsync(int userId);
        Task<ServiceResult<CartDTO>> AddItemAsync(int userId, AddCartItemDTO dto);
        Task<ServiceResult<CartDTO>> SetQuantityAsync(int userId, int bookId, int quantity);
        Task<ServiceResult<CartDTO>> RemoveItemAsync(int userId, int bookId);
        Task ClearAsync(int userId);
    }

    // Заказы пользователя
    public interface IOrderService
    {
        Task<ServiceResult<OrderDTO>> PlaceOrderAsync(int userId, CreateOrderDTO dto);
        Task<List<OrderDTO>> GetMyOrdersAsync(int userId);
        Task<OrderDTO?> GetByNumberAsync(int userId, string number);
    }
}
