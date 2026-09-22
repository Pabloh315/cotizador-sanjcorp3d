using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Data;

namespace SanjCorp3D.Api.Services;

public static class DatabaseSequenceService
{
    public static Task AlignBusinessSequencesAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT setval(pg_get_serial_sequence('sanjcorp."Printers"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM sanjcorp."Printers"), 0) + 1, 1), false);
            SELECT setval(pg_get_serial_sequence('sanjcorp."Consumables"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM sanjcorp."Consumables"), 0) + 1, 1), false);
            SELECT setval(pg_get_serial_sequence('sanjcorp."Materials"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM sanjcorp."Materials"), 0) + 1, 1), false);
            SELECT setval(pg_get_serial_sequence('sanjcorp."Quotes"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM sanjcorp."Quotes"), 0) + 1, 1), false);
            SELECT setval(pg_get_serial_sequence('sanjcorp."QuoteConsumables"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM sanjcorp."QuoteConsumables"), 0) + 1, 1), false);
            SELECT setval(pg_get_serial_sequence('sanjcorp."QuoteMaterials"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM sanjcorp."QuoteMaterials"), 0) + 1, 1), false);
            SELECT setval(pg_get_serial_sequence('sanjcorp."Sales"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM sanjcorp."Sales"), 0) + 1, 1), false);
            SELECT setval(pg_get_serial_sequence('sanjcorp."SaleConsumables"', 'Id'), GREATEST(COALESCE((SELECT MAX("Id") FROM sanjcorp."SaleConsumables"), 0) + 1, 1), false);
            """;
        return db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
