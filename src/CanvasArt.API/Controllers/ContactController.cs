using CanvasArt.API.Authorization;
using CanvasArt.API.Models.DTOs.Contact;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/contact")]
public sealed class ContactController : ApiControllerBase
{
    private readonly IContactService _contact;

    public ContactController(IContactService contact) => _contact = contact;

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Submit(CreateContactMessageRequest request, CancellationToken ct)
        => Created(await _contact.SubmitAsync(request, ct), "Your message has been sent.");

    [HttpGet]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Query([FromQuery] ContactMessageQuery query, CancellationToken ct)
        => Success(await _contact.QueryAsync(query, ct));

    [HttpPut("{id:int}/read")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
        => Success(await _contact.MarkReadAsync(id, ct), "Message marked as read.");
}
