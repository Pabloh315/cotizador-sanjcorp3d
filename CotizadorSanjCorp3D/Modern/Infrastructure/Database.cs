using System.Globalization;
using CotizadorSanjCorp3D.Domain;
using Microsoft.Data.Sqlite;

namespace CotizadorSanjCorp3D.Infrastructure;

public sealed class Database
{
    private readonly string _path;
    private readonly string _connectionString;

    public Database(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("La ruta de la base de datos es obligatoria.", nameof(path));
        _path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = false
        }.ToString();
    }

    public string PathOnDisk => _path;

    public void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY NOT NULL,
                value TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS printers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                build_x REAL NOT NULL CHECK(build_x > 0),
                build_y REAL NOT NULL CHECK(build_y > 0),
                build_z REAL NOT NULL CHECK(build_z > 0),
                nozzle REAL NOT NULL CHECK(nozzle > 0),
                speed REAL NOT NULL CHECK(speed > 0),
                power_watts REAL NOT NULL CHECK(power_watts >= 0),
                hourly_cost REAL NOT NULL CHECK(hourly_cost >= 0),
                is_default INTEGER NOT NULL DEFAULT 0,
                active INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE IF NOT EXISTS filaments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                category TEXT NOT NULL DEFAULT 'Consumible',
                material TEXT NOT NULL,
                color TEXT NOT NULL,
                price_per_kg REAL NOT NULL CHECK(price_per_kg >= 0),
                density REAL NOT NULL CHECK(density > 0),
                is_default INTEGER NOT NULL DEFAULT 0,
                active INTEGER NOT NULL DEFAULT 1,
                stock_quantity INTEGER NOT NULL DEFAULT 0 CHECK(stock_quantity >= 0),
                UNIQUE(name, material, color)
            );
            CREATE TABLE IF NOT EXISTS materials (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                category TEXT NOT NULL,
                unit TEXT NOT NULL,
                unit_price REAL NOT NULL CHECK(unit_price >= 0),
                active INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE IF NOT EXISTS quotes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_code TEXT NOT NULL UNIQUE,
                created_at TEXT NOT NULL,
                customer TEXT NOT NULL,
                project_name TEXT NOT NULL,
                printer_name TEXT NOT NULL,
                print_hours REAL NOT NULL,
                quantity INTEGER NOT NULL,
                labor_hours REAL NOT NULL,
                manual_additional REAL NOT NULL,
                functional_part INTEGER NOT NULL,
                margin_percent REAL NOT NULL,
                profit_multiplier REAL NOT NULL DEFAULT 1.30,
                notes TEXT NOT NULL,
                total_weight REAL NOT NULL,
                material_cost REAL NOT NULL,
                electricity_cost REAL NOT NULL,
                machine_cost REAL NOT NULL,
                maintenance_cost REAL NOT NULL,
                labor_cost REAL NOT NULL,
                additional_cost REAL NOT NULL,
                functional_surcharge REAL NOT NULL,
                subtotal REAL NOT NULL,
                profit_amount REAL NOT NULL,
                tax_amount REAL NOT NULL,
                recommended_price REAL NOT NULL
            );
            CREATE TABLE IF NOT EXISTS quote_filaments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                quote_id INTEGER NOT NULL REFERENCES quotes(id) ON DELETE CASCADE,
                filament_id INTEGER NOT NULL,
                filament_name TEXT NOT NULL,
                category TEXT NOT NULL DEFAULT 'Consumible',
                material TEXT NOT NULL DEFAULT '',
                color TEXT NOT NULL DEFAULT '',
                grams REAL NOT NULL,
                price_per_kg REAL NOT NULL,
                density REAL NOT NULL DEFAULT 1,
                line_cost REAL NOT NULL
            );
            CREATE TABLE IF NOT EXISTS quote_materials (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                quote_id INTEGER NOT NULL REFERENCES quotes(id) ON DELETE CASCADE,
                material_id INTEGER NOT NULL,
                material_name TEXT NOT NULL,
                quantity REAL NOT NULL,
                unit_price REAL NOT NULL,
                line_cost REAL NOT NULL
            );
            CREATE TABLE IF NOT EXISTS sales (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                quote_id INTEGER NOT NULL UNIQUE REFERENCES quotes(id) ON DELETE CASCADE,
                sold_at TEXT NOT NULL,
                sale_amount REAL NOT NULL CHECK(sale_amount >= 0)
            );
            CREATE INDEX IF NOT EXISTS ix_quotes_created ON quotes(created_at);
            CREATE INDEX IF NOT EXISTS ix_quotes_customer ON quotes(customer);
            CREATE INDEX IF NOT EXISTS ix_sales_sold_at ON sales(sold_at);
            """;
        command.ExecuteNonQuery();
        EnsureColumn(connection, "filaments", "category", "TEXT NOT NULL DEFAULT 'Consumible'");
        EnsureColumn(connection, "filaments", "stock_quantity", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "quotes", "profit_multiplier", "REAL NOT NULL DEFAULT 1.30");
        EnsureColumn(connection, "quote_filaments", "category", "TEXT NOT NULL DEFAULT 'Consumible'");
        EnsureColumn(connection, "quote_filaments", "material", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "quote_filaments", "color", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "quote_filaments", "density", "REAL NOT NULL DEFAULT 1");
        using (var migrate = connection.CreateCommand())
        {
            migrate.CommandText = """
                UPDATE quote_filaments
                SET category=COALESCE((SELECT category FROM filaments WHERE filaments.id=quote_filaments.filament_id),category),
                    material=COALESCE((SELECT material FROM filaments WHERE filaments.id=quote_filaments.filament_id),material),
                    color=COALESCE((SELECT color FROM filaments WHERE filaments.id=quote_filaments.filament_id),color),
                    density=COALESCE((SELECT density FROM filaments WHERE filaments.id=quote_filaments.filament_id),density);
                """;
            migrate.ExecuteNonQuery();
        }
        Seed(connection);
    }

    public AppSettings GetSettings()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT key, value FROM settings";
        using var reader = command.ExecuteReader();
        while (reader.Read()) values[reader.GetString(0)] = reader.GetString(1);
        var defaults = AppSettings.Defaults;
        decimal legacyMargin = Decimal(values, "default_margin", 30m);
        return new(
            Get(values, "business_name", defaults.BusinessName),
            Get(values, "currency_name", defaults.CurrencyName),
            Get(values, "currency_symbol", defaults.CurrencySymbol),
            Decimal(values, "electricity_per_kwh", defaults.ElectricityPerKwh),
            Decimal(values, "maintenance_per_print", defaults.MaintenancePerPrint),
            Decimal(values, "profit_multiplier", 1m + legacyMargin / 100m),
            Decimal(values, "tax_percent", defaults.TaxPercent),
            Decimal(values, "round_to", defaults.RoundTo),
            (int)Decimal(values, "decimal_places", defaults.DecimalPlaces));
    }

    public void SaveSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.BusinessName) || string.IsNullOrWhiteSpace(settings.CurrencySymbol))
            throw new ArgumentException("El negocio y el símbolo monetario son obligatorios.", nameof(settings));
        if (new[] { settings.ElectricityPerKwh, settings.MaintenancePerPrint,
                settings.TaxPercent, settings.RoundTo }
            .Any(value => value < 0)) throw new ArgumentOutOfRangeException(nameof(settings), "Los costos no pueden ser negativos.");
        if (settings.DefaultProfitMultiplier < 1m)
            throw new ArgumentOutOfRangeException(nameof(settings), "El multiplicador debe ser igual o mayor que 1.");

        var values = new Dictionary<string, string>
        {
            ["business_name"] = settings.BusinessName.Trim(),
            ["currency_name"] = settings.CurrencyName.Trim(),
            ["currency_symbol"] = settings.CurrencySymbol.Trim(),
            ["electricity_per_kwh"] = Inv(settings.ElectricityPerKwh),
            ["maintenance_per_print"] = Inv(settings.MaintenancePerPrint),
            ["profit_multiplier"] = Inv(settings.DefaultProfitMultiplier),
            ["tax_percent"] = Inv(settings.TaxPercent),
            ["round_to"] = Inv(settings.RoundTo),
            ["decimal_places"] = settings.DecimalPlaces.ToString(CultureInfo.InvariantCulture)
        };
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        foreach (var item in values) UpsertSetting(connection, transaction, item.Key, item.Value);
        transaction.Commit();
    }

    public IReadOnlyList<Printer> GetPrinters() => QueryList("""
        SELECT id,name,build_x,build_y,build_z,nozzle,speed,power_watts,hourly_cost,is_default,active
        FROM printers WHERE active=1 ORDER BY is_default DESC,name
        """, reader => new Printer(reader.GetInt64(0), reader.GetString(1), Dec(reader, 2), Dec(reader, 3),
        Dec(reader, 4), Dec(reader, 5), Dec(reader, 6), Dec(reader, 7), Dec(reader, 8), reader.GetBoolean(9), reader.GetBoolean(10)));

    public IReadOnlyList<Filament> GetFilaments() => QueryList("""
        SELECT id,name,category,material,color,price_per_kg,density,is_default,active,stock_quantity
        FROM filaments WHERE active=1 ORDER BY category,is_default DESC,material,name,color
        """, reader => new Filament(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
        Dec(reader, 5), Dec(reader, 6), reader.GetBoolean(7), reader.GetBoolean(8), reader.GetInt32(9)));

    public IReadOnlyList<ExtraMaterial> GetMaterials() => QueryList("""
        SELECT id,name,category,unit,unit_price,active FROM materials WHERE active=1 ORDER BY category,name
        """, reader => new ExtraMaterial(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), Dec(reader, 4), reader.GetBoolean(5)));

    public long SavePrinter(Printer item)
    {
        ValidateName(item.Name);
        if (item.BuildX <= 0 || item.BuildY <= 0 || item.BuildZ <= 0 || item.Nozzle <= 0 || item.Speed <= 0 || item.PowerWatts < 0 || item.HourlyCost < 0)
            throw new ArgumentOutOfRangeException(nameof(item), "Revise dimensiones, velocidad, potencia y costo.");
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        if (item.IsDefault) ClearDefault(connection, transaction, "printers");
        long id = UpsertCatalog(connection, transaction, "printers", item.Id,
            "name,build_x,build_y,build_z,nozzle,speed,power_watts,hourly_cost,is_default,active",
            "$name,$x,$y,$z,$nozzle,$speed,$power,$cost,$default,1",
            command => { Add(command, "$name", item.Name.Trim()); Add(command, "$x", item.BuildX); Add(command, "$y", item.BuildY); Add(command, "$z", item.BuildZ); Add(command, "$nozzle", item.Nozzle); Add(command, "$speed", item.Speed); Add(command, "$power", item.PowerWatts); Add(command, "$cost", item.HourlyCost); Add(command, "$default", item.IsDefault); });
        transaction.Commit(); return id;
    }

    public long SaveFilament(Filament item)
    {
        ValidateName(item.Name); ValidateName(item.Category); ValidateName(item.Material); ValidateName(item.Color);
        if (item.Category is not ("Consumible" or "Resina")) throw new ArgumentException("La categoría debe ser Consumible o Resina.", nameof(item));
        if (item.PricePerUnit < 0 || item.Density <= 0 || item.StockQuantity < 0) throw new ArgumentOutOfRangeException(nameof(item), "El precio, densidad o existencia no son válidos.");
        using var connection = Open(); using var transaction = connection.BeginTransaction();
        if (item.IsDefault) ClearDefault(connection, transaction, "filaments");
        long id = UpsertCatalog(connection, transaction, "filaments", item.Id,
            "name,category,material,color,price_per_kg,density,is_default,stock_quantity,active", "$name,$category,$material,$color,$price,$density,$default,$stock,1",
            command => { Add(command, "$name", item.Name.Trim()); Add(command, "$category", item.Category); Add(command, "$material", item.Material.Trim()); Add(command, "$color", item.Color.Trim()); Add(command, "$price", item.PricePerUnit); Add(command, "$density", item.Density); Add(command, "$default", item.IsDefault); Add(command, "$stock", item.StockQuantity); });
        transaction.Commit(); return id;
    }

    public void UpdateFilamentStock(long id, int quantity)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity), "La existencia no puede ser negativa.");
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "UPDATE filaments SET stock_quantity=$quantity WHERE id=$id AND active=1; SELECT changes();";
        Add(command, "$id", id); Add(command, "$quantity", quantity);
        if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 1)
            throw new InvalidOperationException("No se encontró el consumible para actualizar su inventario.");
    }

    public long SaveMaterial(ExtraMaterial item)
    {
        ValidateName(item.Name); ValidateName(item.Category); ValidateName(item.Unit);
        if (item.UnitPrice < 0) throw new ArgumentOutOfRangeException(nameof(item), "El precio no puede ser negativo.");
        using var connection = Open(); using var transaction = connection.BeginTransaction();
        long id = UpsertCatalog(connection, transaction, "materials", item.Id,
            "name,category,unit,unit_price,active", "$name,$category,$unit,$price,1",
            command => { Add(command, "$name", item.Name.Trim()); Add(command, "$category", item.Category.Trim()); Add(command, "$unit", item.Unit.Trim()); Add(command, "$price", item.UnitPrice); });
        transaction.Commit(); return id;
    }

    public void ArchiveCatalogItem(string table, long id)
    {
        if (id <= 0) return;
        if (table is not ("printers" or "filaments" or "materials")) throw new ArgumentException("Catálogo no permitido.", nameof(table));
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = table == "materials"
            ? "UPDATE materials SET active=0 WHERE id=$id"
            : $"UPDATE {table} SET active=0,is_default=0 WHERE id=$id";
        Add(command, "$id", id); command.ExecuteNonQuery();
    }

    public SavedQuote SaveQuote(QuoteInput input, QuoteCalculation calculation)
    {
        if (string.IsNullOrWhiteSpace(input.Customer) || string.IsNullOrWhiteSpace(input.ProjectName))
            throw new ArgumentException("Cliente y nombre de la pieza/proyecto son obligatorios.", nameof(input));
        using var connection = Open(); using var transaction = connection.BeginTransaction();
        string code = CreateOrderCode(connection, transaction, input.Customer, input.Printer.Name);
        DateTime created = DateTime.Now;
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO quotes(order_code,created_at,customer,project_name,printer_name,print_hours,quantity,labor_hours,
                manual_additional,functional_part,margin_percent,profit_multiplier,notes,total_weight,material_cost,electricity_cost,machine_cost,
                maintenance_cost,labor_cost,additional_cost,functional_surcharge,subtotal,profit_amount,tax_amount,recommended_price)
            VALUES($code,$created,$customer,$project,$printer,$hours,$quantity,0,$manual,0,0,$multiplier,$notes,
                $weight,$material,$electricity,$machine,$maintenance,$labor,$additional,$surcharge,$subtotal,$profit,$tax,$recommended);
            SELECT last_insert_rowid();
            """;
        Add(command, "$code", code); Add(command, "$created", created.ToString("O", CultureInfo.InvariantCulture)); Add(command, "$customer", input.Customer.Trim());
        Add(command, "$project", input.ProjectName.Trim()); Add(command, "$printer", input.Printer.Name); Add(command, "$hours", input.PrintHours);
        Add(command, "$quantity", input.Quantity); Add(command, "$manual", input.AdditionalManualCost);
        Add(command, "$multiplier", input.ProfitMultiplier); Add(command, "$notes", input.Notes.Trim());
        Add(command, "$weight", calculation.TotalWeight); Add(command, "$material", calculation.MaterialCost); Add(command, "$electricity", calculation.ElectricityCost);
        Add(command, "$machine", calculation.MachineCost); Add(command, "$maintenance", calculation.MaintenanceCost); Add(command, "$labor", calculation.LaborCost);
        Add(command, "$additional", calculation.AdditionalCost); Add(command, "$surcharge", calculation.FunctionalSurcharge); Add(command, "$subtotal", calculation.Subtotal);
        Add(command, "$profit", calculation.ProfitAmount); Add(command, "$tax", calculation.TaxAmount); Add(command, "$recommended", calculation.RecommendedPrice);
        long id = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        foreach (var item in input.Filaments) InsertFilamentLine(connection, transaction, id, item, input.Quantity);
        foreach (var item in input.Materials) InsertMaterialLine(connection, transaction, id, item);
        transaction.Commit(); return new SavedQuote(id, code, created, input, calculation);
    }

    public IReadOnlyList<QuoteSummary> GetQuotes(string search = "", DateTime? from = null, DateTime? to = null)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT q.id,q.order_code,q.created_at,q.customer,q.project_name,q.printer_name,q.total_weight,q.subtotal,q.recommended_price,s.sold_at
            FROM quotes q LEFT JOIN sales s ON s.quote_id=q.id
            WHERE ($search='' OR q.order_code LIKE $like OR q.customer LIKE $like OR q.project_name LIKE $like)
                AND ($from='' OR created_at >= $from) AND ($to='' OR created_at < $to)
            ORDER BY q.created_at DESC
            """;
        search = search.Trim(); Add(command, "$search", search); Add(command, "$like", $"%{search.Replace("%", "[%]")}%");
        Add(command, "$from", from?.Date.ToString("O", CultureInfo.InvariantCulture) ?? "");
        Add(command, "$to", to?.Date.AddDays(1).ToString("O", CultureInfo.InvariantCulture) ?? "");
        var result = new List<QuoteSummary>(); using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(new(reader.GetInt64(0), reader.GetString(1), DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(3), reader.GetString(4), reader.GetString(5), Dec(reader, 6), Dec(reader, 7), Dec(reader, 8),
            reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        return result;
    }

    public QuoteDetails? GetQuote(long id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT q.id,q.order_code,q.created_at,q.customer,q.project_name,q.printer_name,q.print_hours,q.quantity,
                q.manual_additional,q.profit_multiplier,q.notes,q.total_weight,q.material_cost,q.electricity_cost,q.machine_cost,
                q.maintenance_cost,q.labor_cost,q.additional_cost,q.functional_surcharge,q.subtotal,q.profit_amount,q.tax_amount,
                q.recommended_price,s.sold_at
            FROM quotes q LEFT JOIN sales s ON s.quote_id=q.id WHERE q.id=$id
            """;
        Add(command, "$id", id);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        long quoteId = reader.GetInt64(0);
        string code = reader.GetString(1);
        DateTime created = DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        string customer = reader.GetString(3); string project = reader.GetString(4); string printer = reader.GetString(5);
        decimal hours = Dec(reader, 6); int quantity = reader.GetInt32(7); decimal manual = Dec(reader, 8); decimal multiplier = Dec(reader, 9); string notes = reader.GetString(10);
        var calculation = new QuoteCalculation(Dec(reader, 11), Dec(reader, 12), Dec(reader, 13), Dec(reader, 14),
            Dec(reader, 15), Dec(reader, 16), Dec(reader, 17), Dec(reader, 18), Dec(reader, 19), Dec(reader, 20), Dec(reader, 21), Dec(reader, 22));
        DateTime? soldAt = reader.IsDBNull(23) ? null : DateTime.Parse(reader.GetString(23), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        reader.Close();

        var filaments = new List<FilamentUsage>();
        using (var lines = connection.CreateCommand())
        {
            lines.CommandText = "SELECT filament_id,filament_name,category,material,color,grams,price_per_kg,density FROM quote_filaments WHERE quote_id=$id ORDER BY id";
            Add(lines, "$id", id);
            using var lineReader = lines.ExecuteReader();
            while (lineReader.Read()) filaments.Add(new(lineReader.GetInt64(0), lineReader.GetString(1), lineReader.GetString(2), lineReader.GetString(3), lineReader.GetString(4), Dec(lineReader, 5), Dec(lineReader, 6), Dec(lineReader, 7)));
        }
        var materials = new List<MaterialUsage>();
        using (var lines = connection.CreateCommand())
        {
            lines.CommandText = "SELECT material_id,material_name,quantity,unit_price FROM quote_materials WHERE quote_id=$id ORDER BY id";
            Add(lines, "$id", id);
            using var lineReader = lines.ExecuteReader();
            while (lineReader.Read()) materials.Add(new(lineReader.GetInt64(0), lineReader.GetString(1), Dec(lineReader, 2), Dec(lineReader, 3)));
        }
        return new(quoteId, code, created, customer, project, printer, hours, quantity, manual, multiplier, notes,
            filaments, materials, calculation, soldAt);
    }

    public bool ConfirmSale(long quoteId)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO sales(quote_id,sold_at,sale_amount)
            SELECT id,$sold,recommended_price FROM quotes WHERE id=$id;
            SELECT changes();
            """;
        Add(command, "$id", quoteId); Add(command, "$sold", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
    }

    public IReadOnlyList<SaleSummary> GetSales(DateTime from, DateTime to)
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.id,q.id,q.order_code,s.sold_at,q.customer,q.project_name,q.subtotal,s.sale_amount
            FROM sales s JOIN quotes q ON q.id=s.quote_id
            WHERE s.sold_at >= $from AND s.sold_at < $to ORDER BY s.sold_at DESC
            """;
        Add(command, "$from", from.Date.ToString("O", CultureInfo.InvariantCulture)); Add(command, "$to", to.Date.AddDays(1).ToString("O", CultureInfo.InvariantCulture));
        var result = new List<SaleSummary>(); using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2),
            DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), reader.GetString(4), reader.GetString(5), Dec(reader, 6), Dec(reader, 7)));
        return result;
    }

    public ReportSummary GetReport(DateTime from, DateTime to)
    {
        var quotes = GetQuotes("", from, to);
        var filaments = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT qf.filament_name,SUM(qf.grams*q.quantity) FROM quote_filaments qf
            JOIN quotes q ON q.id=qf.quote_id WHERE q.created_at >= $from AND q.created_at < $to GROUP BY qf.filament_name
            """;
        Add(command, "$from", from.Date.ToString("O", CultureInfo.InvariantCulture)); Add(command, "$to", to.Date.AddDays(1).ToString("O", CultureInfo.InvariantCulture));
        using (var reader = command.ExecuteReader()) while (reader.Read()) filaments[reader.GetString(0)] = Dec(reader, 1);
        var costs = new Dictionary<string, decimal>
        {
            ["Filamento"] = ScalarSum(connection, "material_cost", from, to),
            ["Electricidad"] = ScalarSum(connection, "electricity_cost", from, to),
            ["Mantenimiento"] = ScalarSum(connection, "maintenance_cost", from, to),
            ["Adicionales"] = ScalarSum(connection, "additional_cost", from, to)
        };
        IReadOnlyList<SaleSummary> sales = GetSales(from, to);
        return new(quotes.Count, quotes.Sum(q => q.CostTotal), quotes.Sum(q => q.RecommendedPrice),
            ScalarSum(connection, "profit_amount", from, to), sales.Count, sales.Sum(sale => sale.SaleAmount),
            sales.Sum(sale => sale.Profit), sales, filaments, costs);
    }

    public void DeleteQuote(long id)
    {
        using var connection = Open(); using var command = connection.CreateCommand(); command.CommandText = "DELETE FROM quotes WHERE id=$id";
        Add(command, "$id", id); command.ExecuteNonQuery();
    }

    public void BackupTo(string destination)
    {
        string full = Path.GetFullPath(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using (var connection = Open()) { using var command = connection.CreateCommand(); command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);"; command.ExecuteNonQuery(); }
        File.Copy(_path, full, true);
    }

    public void RestoreFrom(string source)
    {
        string full = Path.GetFullPath(source);
        if (!File.Exists(full)) throw new FileNotFoundException("No se encontró el respaldo.", full);
        using (var test = new SqliteConnection($"Data Source={full};Mode=ReadOnly")) { test.Open(); using var cmd = test.CreateCommand(); cmd.CommandText = "PRAGMA integrity_check;"; if (!string.Equals(Convert.ToString(cmd.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("El respaldo no es una base SQLite válida."); }
        File.Copy(full, _path, true); Initialize();
    }

    private SqliteConnection Open() { var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection; }

    private void Seed(SqliteConnection connection)
    {
        var defaults = AppSettings.Defaults; using var transaction = connection.BeginTransaction();
        var settings = new Dictionary<string, string>
        {
            { "business_name", defaults.BusinessName }, { "currency_name", defaults.CurrencyName },
            { "currency_symbol", defaults.CurrencySymbol }, { "electricity_per_kwh", Inv(defaults.ElectricityPerKwh) },
            { "maintenance_per_print", Inv(defaults.MaintenancePerPrint) },
            { "profit_multiplier", Inv(defaults.DefaultProfitMultiplier) }, { "tax_percent", Inv(defaults.TaxPercent) },
            { "round_to", Inv(defaults.RoundTo) }, { "decimal_places", defaults.DecimalPlaces.ToString(CultureInfo.InvariantCulture) }
        };
        foreach (var item in settings) UpsertSetting(connection, transaction, item.Key, item.Value, true);
        if (Count(connection, transaction, "printers") == 0) Execute(connection, transaction, "INSERT INTO printers(name,build_x,build_y,build_z,nozzle,speed,power_watts,hourly_cost,is_default) VALUES('Elegoo Neptune 4',225,225,265,0.4,250,310,4.10,1),('Ender 3 V3 SE',220,220,250,0.4,180,350,3.50,0)");
        if (Count(connection, transaction, "filaments") == 0) Execute(connection, transaction, "INSERT INTO filaments(name,category,material,color,price_per_kg,density,is_default,stock_quantity) VALUES('PLA estándar','Consumible','PLA','Azul',145,1.24,1,2),('PETG estándar','Consumible','PETG','Verde',155,1.27,0,1),('ABS estándar','Consumible','ABS','Negro',150,1.04,0,1),('TPU flexible','Consumible','TPU','Morado',180,1.21,0,1)");
        Execute(connection, transaction, "INSERT OR IGNORE INTO filaments(name,category,material,color,price_per_kg,density,is_default,stock_quantity) VALUES('Resina SLA','Resina','SLA','Estándar',220,1.10,0,1)");
        if (Count(connection, transaction, "materials") == 0) Execute(connection, transaction, "INSERT INTO materials(name,category,unit,unit_price) VALUES('Soportes solubles','Consumible','kg',200),('Adhesivo en barra','Consumible','unidad',15),('Cinta Kapton','Consumible','unidad',12),('Boquilla 0.4 mm','Repuesto','unidad',25)");
        Execute(connection, transaction, "UPDATE materials SET active=0 WHERE name='Resina SLA'");
        transaction.Commit();
    }

    private IReadOnlyList<T> QueryList<T>(string sql, Func<SqliteDataReader, T> map) { using var connection = Open(); using var command = connection.CreateCommand(); command.CommandText = sql; using var reader = command.ExecuteReader(); var result = new List<T>(); while (reader.Read()) result.Add(map(reader)); return result; }
    private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition) { using var check = connection.CreateCommand(); check.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name=$column"; Add(check, "$column", column); if (Convert.ToInt32(check.ExecuteScalar(), CultureInfo.InvariantCulture) > 0) return; using var alter = connection.CreateCommand(); alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}"; alter.ExecuteNonQuery(); }
    private static long UpsertCatalog(SqliteConnection c, SqliteTransaction t, string table, long id, string columns, string values, Action<SqliteCommand> bind) { using var cmd = c.CreateCommand(); cmd.Transaction = t; if (id == 0) cmd.CommandText = $"INSERT INTO {table}({columns}) VALUES({values}); SELECT last_insert_rowid();"; else cmd.CommandText = $"UPDATE {table} SET {string.Join(',', columns.Split(',').Where(x => x != "active").Select(x => $"{x}=${x switch { "price_per_kg" => "price", "unit_price" => "price", "is_default" => "default", "stock_quantity" => "stock", "build_x" => "x", "build_y" => "y", "build_z" => "z", "power_watts" => "power", "hourly_cost" => "cost", _ => x }}"))},active=1 WHERE id=$id; SELECT $id;"; bind(cmd); Add(cmd, "$id", id); return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture); }
    private static void ClearDefault(SqliteConnection c, SqliteTransaction t, string table) { using var cmd = c.CreateCommand(); cmd.Transaction = t; cmd.CommandText = $"UPDATE {table} SET is_default=0"; cmd.ExecuteNonQuery(); }
    private static void InsertFilamentLine(SqliteConnection c, SqliteTransaction t, long quoteId, FilamentUsage item, int quantity)
    {
        using var cmd = c.CreateCommand(); cmd.Transaction = t;
        cmd.CommandText = """
            INSERT INTO quote_filaments(quote_id,filament_id,filament_name,category,material,color,grams,price_per_kg,density,line_cost)
            VALUES($quote,$item,$name,$category,$material,$color,$quantity,$price,$density,$cost)
            """;
        Add(cmd, "$quote", quoteId); Add(cmd, "$item", item.FilamentId); Add(cmd, "$name", item.FilamentName);
        Add(cmd, "$category", item.Category); Add(cmd, "$material", item.Material); Add(cmd, "$color", item.Color); Add(cmd, "$quantity", item.Grams); Add(cmd, "$price", item.PricePerUnit);
        Add(cmd, "$density", item.Density); Add(cmd, "$cost", item.Cost * quantity); cmd.ExecuteNonQuery();
    }

    private static void InsertMaterialLine(SqliteConnection c, SqliteTransaction t, long quoteId, MaterialUsage item)
    {
        using var cmd = c.CreateCommand(); cmd.Transaction = t;
        cmd.CommandText = """
            INSERT INTO quote_materials(quote_id,material_id,material_name,quantity,unit_price,line_cost)
            VALUES($quote,$item,$name,$quantity,$price,$cost)
            """;
        Add(cmd, "$quote", quoteId); Add(cmd, "$item", item.MaterialId); Add(cmd, "$name", item.MaterialName);
        Add(cmd, "$quantity", item.Quantity); Add(cmd, "$price", item.UnitPrice); Add(cmd, "$cost", item.Cost); cmd.ExecuteNonQuery();
    }

    private static string CreateOrderCode(SqliteConnection c, SqliteTransaction t, string customer, string printer)
    {
        using var sequence = c.CreateCommand(); sequence.Transaction = t;
        sequence.CommandText = "SELECT COALESCE((SELECT seq FROM sqlite_sequence WHERE name='quotes'),0)+1";
        long next = Convert.ToInt64(sequence.ExecuteScalar(), CultureInfo.InvariantCulture);
        string customerInitial = Initial(customer); string printerInitial = Initial(printer);
        string code = $"{customerInitial}{printerInitial}{DateTime.Now:yyyyMMdd}{next:0000}";
        using var check = c.CreateCommand(); check.Transaction = t; check.CommandText = "SELECT COUNT(*) FROM quotes WHERE order_code=$code"; Add(check, "$code", code);
        if (Convert.ToInt32(check.ExecuteScalar(), CultureInfo.InvariantCulture) != 0)
            throw new InvalidOperationException("No se pudo generar el número correlativo de la cotización.");
        return code;
    }

    private static string Initial(string value)
    {
        foreach (char character in value.Trim().Normalize(System.Text.NormalizationForm.FormD))
        {
            if (char.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark || !char.IsLetterOrDigit(character)) continue;
            return char.ToUpperInvariant(character).ToString();
        }
        return "X";
    }
    private static decimal ScalarSum(SqliteConnection c, string column, DateTime from, DateTime to) { using var cmd = c.CreateCommand(); cmd.CommandText = $"SELECT COALESCE(SUM({column}),0) FROM quotes WHERE created_at >= $from AND created_at < $to"; Add(cmd, "$from", from.Date.ToString("O", CultureInfo.InvariantCulture)); Add(cmd, "$to", to.Date.AddDays(1).ToString("O", CultureInfo.InvariantCulture)); return Convert.ToDecimal(cmd.ExecuteScalar(), CultureInfo.InvariantCulture); }
    private static void UpsertSetting(SqliteConnection c, SqliteTransaction t, string key, string value, bool onlyIfMissing = false) { using var cmd = c.CreateCommand(); cmd.Transaction = t; cmd.CommandText = onlyIfMissing ? "INSERT OR IGNORE INTO settings(key,value) VALUES($key,$value)" : "INSERT INTO settings(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value"; Add(cmd, "$key", key); Add(cmd, "$value", value); cmd.ExecuteNonQuery(); }
    private static int Count(SqliteConnection c, SqliteTransaction t, string table) { using var cmd = c.CreateCommand(); cmd.Transaction = t; cmd.CommandText = $"SELECT COUNT(*) FROM {table}"; return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture); }
    private static void Execute(SqliteConnection c, SqliteTransaction t, string sql) { using var cmd = c.CreateCommand(); cmd.Transaction = t; cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
    private static void Add(SqliteCommand command, string name, object value) => command.Parameters.AddWithValue(name, value is decimal d ? (double)d : value);
    private static decimal Dec(SqliteDataReader reader, int ordinal) => Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    private static string Inv(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback) => values.TryGetValue(key, out var value) ? value : fallback;
    private static decimal Decimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback) => values.TryGetValue(key, out var value) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    private static void ValidateName(string value) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100) throw new ArgumentException("El nombre es obligatorio y no puede superar 100 caracteres."); }
}
