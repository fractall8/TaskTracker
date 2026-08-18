using Application.Interfaces.Services;
using Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("config")]
public class AppConfigController(IBusinessCalendar calendar) : ControllerBase
{
    // The offset rides along so the browser can do date maths without needing the tz database.
    [HttpGet]
    public ActionResult<AppConfigDto> Get() =>
        Ok(new AppConfigDto(
            calendar.TimeZoneId,
            (int)calendar.StartOfDayLocal(calendar.Today).Offset.TotalMinutes));
}
