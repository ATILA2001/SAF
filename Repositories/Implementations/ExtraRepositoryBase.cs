#nullable enable
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SAF.Application.Common;
using SAF.Data;
using SAF.Data.Entities;

namespace SAF.Repositories.Implementations;

/// <summary>
/// Upsert de las tablas "extra" (datos manuales por fila): concentra la política de
/// concurrencia, idéntica para todas. Cada repo concreto solo aporta su clave de
/// negocio y qué campos copia el update.
/// </summary>
public abstract class ExtraRepositoryBase<TEntity>(IDbContextFactory<AppDbContext> dbFactory)
    where TEntity : class, IExtraEditable
{
    protected IDbContextFactory<AppDbContext> DbFactory => dbFactory;

    /// <summary>Predicado que encuentra la fila existente con la clave de negocio de la entidad.</summary>
    protected abstract Expression<Func<TEntity, bool>> MismaClave(TEntity entity);

    /// <summary>Copia los campos editables de la entidad recibida sobre la fila existente.</summary>
    protected abstract void CopiarCampos(TEntity origen, TEntity destino);

    public async Task UpsertAsync(TEntity entity, CancellationToken ct = default)
    {
        try
        {
            await GuardarAsync(entity, ct);
        }
        catch (DbUpdateException ex) when (ErroresSql.EsClaveDuplicada(ex))
        {
            // Otro usuario insertó la fila entre el chequeo y el guardado: el usuario
            // editó sobre datos que ya no existen, así que actualizar acá pisaría lo
            // recién guardado sin aviso. Es un conflicto, no un caso de reintento.
            throw new ConflictoDeConcurrenciaException();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoDeConcurrenciaException();
        }
    }

    private async Task GuardarAsync(TEntity entity, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.Set<TEntity>().FirstOrDefaultAsync(MismaClave(entity), ct);

        if (existing is null)
        {
            entity.FechaCreacion = DateTime.UtcNow;
            entity.FechaModificacion = DateTime.UtcNow;
            db.Set<TEntity>().Add(entity);
        }
        else
        {
            // Sin versión significa que al cargar la grilla el extra no existía y otro
            // usuario lo creó en el medio: actualizar pisaría sus datos sin aviso.
            if (entity.RowVersion is null)
                throw new ConflictoDeConcurrenciaException();

            // Se compara contra la versión que el usuario tenía cargada, no contra la
            // recién leída: si otro guardó en el medio, el UPDATE no afecta filas.
            db.Entry(existing).Property(nameof(IExtraEditable.RowVersion)).OriginalValue = entity.RowVersion;

            CopiarCampos(entity, existing);
            existing.FechaModificacion = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
