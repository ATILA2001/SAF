#nullable enable
using SAF.Application.Common;
using SAF.Application.Pagos.Dtos;

namespace SAF.Application.Pagos;

/// <summary>
/// Reglas del alta manual de devengados. Fuente única: la usa el servicio (integridad)
/// y la grilla (aviso antes de perder lo cargado).
/// Solo aplica al alta: en la edición los campos que vienen de IVC son de solo lectura.
/// La unicidad de la fila se valida en el servicio (necesita consultar la base).
/// </summary>
public static class PagoValidator
{
    public static IReadOnlyList<string> ValidarAlta(PagoViewModel vm)
    {
        var errores = new List<string>();

        var tipoDev = (vm.TipoDev ?? string.Empty).Trim().ToUpperInvariant();
        if (tipoDev.Length == 0)
            errores.Add("El tipo de devengado es obligatorio (ej.: PRD, DGG, DRG, DGT).");
        else if (ReglasDevengado.TiposExcluidos.Contains(tipoDev))
            errores.Add($"El tipo {tipoDev} está excluido de la vista Pagos (mismo filtro que la sincronización).");

        if (vm.NroDev <= 0)
            errores.Add("El número de devengado es obligatorio y debe ser mayor a cero.");

        if (vm.FechaDevengado is null)
            errores.Add("La fecha de devengado es obligatoria.");

        if (vm.Importe is not > 0)
            errores.Add("El importe es obligatorio y debe ser mayor a cero (mismo filtro que la sincronización).");

        Validaciones.Expediente(errores, vm.Expediente);

        return errores;
    }
}