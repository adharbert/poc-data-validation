using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Data;

namespace POC.CustomerValidation.API.Startup;

public static class SerilogSetup
{

    public static IHostBuilder AddCustomSerilogLogging(this IHostBuilder hostBuilder)
    {
        // ---- Custom SQL Columns ----
        var columnOptions = new ColumnOptions
        {
            AdditionalColumns =
            [
                new SqlColumn {
                    ColumnName = "Application",
                    DataType   = SqlDbType.NVarChar,
                    DataLength = 100
                },
                new SqlColumn
                {
                    ColumnName = "RequestPath",
                    DataType = SqlDbType.NVarChar,
                    DataLength = 200
                },
                new SqlColumn
                {
                    ColumnName = "RequestBody",
                    DataType = SqlDbType.NVarChar,
                    DataLength = -1
                },
                new SqlColumn
                {
                    ColumnName = "CorrelationId",
                    DataType = SqlDbType.NVarChar,
                    DataLength = 50
                }
            ]
        };



        hostBuilder.UseSerilog((context, services, config) =>
        {
            var loggingConnection = context.Configuration.GetConnectionString("LoggingConnection");
            var appName = context.Configuration["Serilog:Application"] ?? "Unknown";
            var enableInfoDb  = context.Configuration.GetValue<bool>("Serilog:Sinks:InformationDb:Enabled", true);
            var enableErrorDb = context.Configuration.GetValue<bool>("Serilog:Sinks:ErrorDb:Enabled", true);

            var logConfig = config
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", appName);

            if (enableInfoDb)
                logConfig.WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
                    .WriteTo.MSSqlServer(
                        connectionString: loggingConnection,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "InformationLogs",
                            AutoCreateSqlTable = true,
                            BatchPostingLimit = 50
                        },
                        columnOptions: columnOptions
                    )
                );

            if (enableErrorDb)
                logConfig.WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                    .WriteTo.MSSqlServer(
                        connectionString: loggingConnection,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "ErrorLogs",
                            AutoCreateSqlTable = true,
                            BatchPostingLimit = 50
                        },
                        columnOptions: columnOptions
                    )
                );
        });



        return hostBuilder;
    }
}
