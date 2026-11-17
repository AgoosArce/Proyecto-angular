using GestionAcademica.Application.DTOs.Materia;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAcademica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public class MateriaController : ControllerBase
{
    private readonly IMateriaService _service;

    public MateriaController(IMateriaService service) => _service = service;

    /// <summary>Lista completa (sin paginar): la usa también el combo de materias de Angular.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MateriaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MateriaDto>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<MateriaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MateriaDto>> GetById(int id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<MateriaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MateriaDto>> Create(CrearMateriaDto dto, CancellationToken ct)
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
    public async Task<IActionResult> Update(int id, CrearMateriaDto dto, CancellationToken ct)
    {
        await _service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
