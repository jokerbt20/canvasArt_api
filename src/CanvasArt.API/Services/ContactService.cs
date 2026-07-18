using AutoMapper;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Contact;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Settings;
using Microsoft.Extensions.Options;

namespace CanvasArt.API.Services;

public sealed class ContactService : IContactService
{
    private readonly IContactMessageRepository _messages;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _clock;
    private readonly IEmailService _email;
    private readonly EmailSettings _emailSettings;

    public ContactService(
        IContactMessageRepository messages,
        IMapper mapper,
        IDateTimeProvider clock,
        IEmailService email,
        IOptions<EmailSettings> emailSettings)
    {
        _messages = messages;
        _mapper = mapper;
        _clock = clock;
        _email = email;
        _emailSettings = emailSettings.Value;
    }

    public async Task<ContactMessageDto> SubmitAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default)
    {
        var message = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim(),
            Message = request.Message.Trim(),
            IsRead = false,
            CreatedAt = _clock.UtcNow
        };
        message.Id = await _messages.CreateAsync(message, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_emailSettings.NotifyToAddress))
        {
            var subject = string.IsNullOrWhiteSpace(message.Subject)
                ? $"New contact form message from {message.Name}"
                : $"New contact form message: {message.Subject}";
            var body = $"From: {message.Name} <{message.Email}>\n\n{message.Message}";
            await _email.SendAsync(_emailSettings.NotifyToAddress, subject, body, cancellationToken);
        }

        return _mapper.Map<ContactMessageDto>(message);
    }

    public async Task<PagedResult<ContactMessageDto>> QueryAsync(ContactMessageQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _messages.QueryAsync(query, cancellationToken);
        var items = _mapper.Map<List<ContactMessageDto>>(result.Items);
        return new PagedResult<ContactMessageDto>(items, result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<ContactMessageDto> MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await _messages.GetByIdAsync(id, cancellationToken)
                      ?? throw new NotFoundException("ContactMessage", id);
        await _messages.MarkReadAsync(id, cancellationToken);
        message.IsRead = true;
        return _mapper.Map<ContactMessageDto>(message);
    }
}
