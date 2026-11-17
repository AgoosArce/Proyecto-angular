using GestionAcademica.Application.Common;
using GestionAcademica.Application.DTOs.Alumno;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // cualquier usuario autenticado puede consultar
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class AlumnoController : ControllerBase
{
    private readonly IAlumnoService _service;

    public AlumnoController(IAlumnoService service) => _service = service;

    /// <summary>Listado paginado y filtrado: ?pageNumber=1&amp;pageSize=10&amp;nombre=ana&amp;materiaId=2</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AlumnoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AlumnoDto>>> GetPaged([FromQuery] AlumnoFiltroDto filtro, CancellationToken ct) =>
        Ok(await _service.GetPagedAsync(filtro, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<AlumnoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlumnoDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<AlumnoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlumnoDto>> Create(CrearAlumnoDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, ActualizarAlumnoDto dto, CancellationToken ct)
    {
        await _service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
