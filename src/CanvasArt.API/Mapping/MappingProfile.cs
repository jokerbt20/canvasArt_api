using AutoMapper;
using CanvasArt.API.Models.DTOs.Cms;
using CanvasArt.API.Models.DTOs.Contact;
using CanvasArt.API.Models.DTOs.Orders;
using CanvasArt.API.Models.DTOs.Tags;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Mapping;

/// <summary>
/// AutoMapper configuration for the straightforward 1:1 projections. DTOs that carry
/// promotion-derived prices are mapped by hand in their services so pricing stays in one place.
/// </summary>
public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Tag, TagDto>();
        CreateMap<Setting, SettingDto>();
        CreateMap<Slide, SlideDto>();
        CreateMap<Testimonial, TestimonialDto>();
        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<OrderStatusHistory, OrderStatusHistoryDto>();
        CreateMap<ContactMessage, ContactMessageDto>();
    }
}
