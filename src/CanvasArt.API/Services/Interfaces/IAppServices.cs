using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Auth;
using CanvasArt.API.Models.DTOs.Cart;
using CanvasArt.API.Models.DTOs.Categories;
using CanvasArt.API.Models.DTOs.Cms;
using CanvasArt.API.Models.DTOs.Contact;
using CanvasArt.API.Models.DTOs.Frames;
using CanvasArt.API.Models.DTOs.Orders;
using CanvasArt.API.Models.DTOs.Paintings;
using CanvasArt.API.Models.DTOs.Promotions;
using CanvasArt.API.Models.DTOs.Tags;
using CanvasArt.API.Repository;

namespace CanvasArt.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ip, CancellationToken cancellationToken = default);
    Task RevokeAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<UserDto> GetCurrentAsync(int userId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<UserDto>> QueryUsersAsync(PagedQuery query, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> QueryAsync(CategoryQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TagDto> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken = default);
    Task<TagDto> UpdateAsync(int id, UpdateTagRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IPaintingService
{
    Task<PagedResult<PaintingListItemDto>> QueryAsync(PaintingQuery query, bool publishedOnly, CancellationToken cancellationToken = default);
    Task<PaintingDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaintingDetailDto> GetBySlugAsync(string slug, bool incrementView, CancellationToken cancellationToken = default);
    Task<PaintingDetailDto> CreateAsync(CreatePaintingRequest request, CancellationToken cancellationToken = default);
    Task<PaintingDetailDto> UpdateAsync(int id, UpdatePaintingRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<PaintingImageDto> UploadImageAsync(int paintingId, Stream content, string fileName, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(int paintingId, int imageId, CancellationToken cancellationToken = default);
    Task SetPrimaryImageAsync(int paintingId, int imageId, CancellationToken cancellationToken = default);
}

public interface IFrameService
{
    Task<PagedResult<FrameListItemDto>> QueryAsync(FrameQuery query, bool activeOnly, CancellationToken cancellationToken = default);
    Task<FrameDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FrameDetailDto> CreateAsync(CreateFrameRequest request, CancellationToken cancellationToken = default);
    Task<FrameDetailDto> UpdateAsync(int id, UpdateFrameRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<FrameDetailDto> UploadImageAsync(int frameId, Stream content, string fileName, CancellationToken cancellationToken = default);
}

public interface IPromotionService
{
    Task<PagedResult<PromotionDto>> QueryAsync(PromotionQuery query, CancellationToken cancellationToken = default);
    Task<PromotionDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionDto> UpdateAsync(int id, UpdatePromotionRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<CombinationPromotionDto>> QueryCombinationsAsync(PromotionQuery query, CancellationToken cancellationToken = default);
    Task<CombinationPromotionDto> GetCombinationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CombinationPromotionDto> CreateCombinationAsync(CreateCombinationPromotionRequest request, CancellationToken cancellationToken = default);
    Task<CombinationPromotionDto> UpdateCombinationAsync(int id, UpdateCombinationPromotionRequest request, CancellationToken cancellationToken = default);
    Task DeleteCombinationAsync(int id, CancellationToken cancellationToken = default);
}

public interface ICartService
{
    Task<CartResponse> CalculateAsync(CartRequest request, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderListItemDto>> QueryAsync(OrderQuery query, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    Task<OrderStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}

public interface IContactService
{
    Task<ContactMessageDto> SubmitAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<ContactMessageDto>> QueryAsync(ContactMessageQuery query, CancellationToken cancellationToken = default);
    Task<ContactMessageDto> MarkReadAsync(int id, CancellationToken cancellationToken = default);
}

public interface ICmsService
{
    Task<IReadOnlyList<SlideDto>> GetSlidesAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<SlideDto> GetSlideAsync(int id, CancellationToken cancellationToken = default);
    Task<SlideDto> CreateSlideAsync(CreateSlideRequest request, Stream imageContent, string fileName, CancellationToken cancellationToken = default);
    Task<SlideDto> CreateSlideFromPaintingImageAsync(CreateSlideFromPaintingImageRequest request, CancellationToken cancellationToken = default);
    Task<SlideDto> UpdateSlideAsync(int id, UpdateSlideRequest request, Stream? imageContent, string? fileName, CancellationToken cancellationToken = default);
    Task<SlideDto> UpdateSlideFromPaintingImageAsync(int id, CreateSlideFromPaintingImageRequest request, CancellationToken cancellationToken = default);
    Task DeleteSlideAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SettingDto>> GetSettingsAsync(string? group, CancellationToken cancellationToken = default);
    Task UpsertSettingsAsync(UpsertSettingsRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestimonialDto>> GetTestimonialsAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<TestimonialDto> GetTestimonialAsync(int id, CancellationToken cancellationToken = default);
    Task<TestimonialDto> CreateTestimonialAsync(CreateTestimonialRequest request, Stream? imageContent, string? fileName, CancellationToken cancellationToken = default);
    Task<TestimonialDto> UpdateTestimonialAsync(int id, UpdateTestimonialRequest request, Stream? imageContent, string? fileName, CancellationToken cancellationToken = default);
    Task DeleteTestimonialAsync(int id, CancellationToken cancellationToken = default);
}
