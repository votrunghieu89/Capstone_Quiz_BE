using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

[Route("api/[controller]")]
[ApiController]
public class DebugController : ControllerBase
{
    private readonly EndpointDataSource _endpointDataSource;

    public DebugController(EndpointDataSource endpointDataSource)
    {
        _endpointDataSource = endpointDataSource;
    }

    // ===== GET METHODS =====
    [HttpGet("all")]
    public IActionResult GetAllEndpoints()
    {
        var endpoints = _endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => new
            {
                Route = e.RoutePattern.RawText,
                HttpMethods = e.Metadata
                    .OfType<HttpMethodMetadata>()
                    .FirstOrDefault()?.HttpMethods
            })
            .ToList();

        return Ok(new
        {
            Total = endpoints.Count,
            Endpoints = endpoints
        });
    }
}
