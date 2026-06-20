using BadmintonKiosken.Api.Services.Customer;
using BadmintonKiosken.Shared;
using BadmintonKiosken.Shared.DTOs.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadmintonKiosken.Api.Controllers.Customer;

[ApiController]
[Route("api/v1/[controller]")]
public class CustomersController(ICustomerService customerService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<CustomerProfileResponse>> RegisterCustomer(
        CustomerRequest request)
    {
        var result = await customerService.RegisterCustomer(request);

        return CreatedAtAction(nameof(GetCustomerById), new { id = result.Id },
            result);
    }

    [Authorize]
    [HttpGet("{id:Guid}")]
    public async Task<ActionResult<CustomerProfileResponse>>
        GetCustomerById(Guid id)
    {
        var result = await customerService.GetCustomerById(id);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("search")]
    public async Task<ActionResult<CustomerProfileResponse>> GetSingleCustomer(
        [FromQuery] SingleCustomerSearchRequest searchRequest)
    {
        if (string.IsNullOrWhiteSpace(searchRequest.Username) &&
            string.IsNullOrWhiteSpace(searchRequest.Email) &&
            searchRequest.PhoneNumber == 0)
        {
            return BadRequest(
                "Du skal angive enten 'brugernavn', 'e-mail' eller 'telefonnummer', når du søger efter enkel kunde");
        }

        var user =
            await customerService.GetSingleCustomerBySearch(searchRequest);
        return Ok(user);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<CustomerOverviewResponse>>>
        GetCustomers(
            [FromQuery] Guid? id,
            [FromQuery] string? username,
            [FromQuery] string? fullName,
            [FromQuery] string? email,
            [FromQuery] long? phoneNumber,
            [FromQuery] string? nationality,
            [FromQuery] decimal? balance,
            [FromQuery] Loyalty? loyaltyGroup,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? lastUpdated)
    {
        var users = await customerService.GetCustomers(id, username, fullName,
            email, phoneNumber, nationality, balance, loyaltyGroup, createdAt,
            lastUpdated);
        return Ok(users);
    }
}