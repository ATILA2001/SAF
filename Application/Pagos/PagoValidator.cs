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
        else if (tipoDev.Length > 20)
            errores.Add("El tipo de devengado no puede superar los 20 caracteres (los reales tienen 3 o 4).");

        if (vm.NroDev <= 0)
            errores.Add("El número de devengado es obligatorio y debe ser mayor a cero.");

        if (vm.FechaDevengado is null)
            errores.Add("La fecha de devengado es obligatoria.");

        if (vm.Importe is not > 0)
            errores.Add("El importe es obligatorio y debe ser mayor a cero (mismo filtro que la sincronización).");

        Validaciones.Expediente(errores, vm.Expediente);
        Validaciones.Largo(errores, vm.Empresa, 255, "Empresa");

        errores.AddRange(ValidarEdicion(vm));

        return errores;
    }

    /// <summary>
    /// Campos propios de SAF, editables tanto en el alta como sobre una fila del ledger.
    /// Los que vienen de IVC son de solo lectura en la edición.
    /// </summary>
    public static IReadOnlyList<string> ValidarEdicion(PagoViewModel vm)
    {
        var errores = new List<string>();

        Validaciones.Largo(errores, vm.Observaciones, 500, "Observaciones");
        Validaciones.Largo(errores, vm.Ccoo, 200, "CCOO");

        return errores;
    }
}