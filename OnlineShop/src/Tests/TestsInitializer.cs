﻿using Microsoft.EntityFrameworkCore;
using OnlineShop.Server.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

namespace OnlineShop.Tests;

[TestClass]
public partial class TestsInitializer
{
    [AssemblyInitialize]
    public static async Task Initialize(TestContext testContext)
    {
        await using var testServer = new AppTestServer();

        await testServer.Build().Start();

        await InitializeDatabase(testServer);
    }

    //SQLite database in in-memory mode only lives as long as at least one connection to it is open
    //This connection is required to keep the database alive during the test run.
    private static SqliteConnection connection = null!;
    private static async Task InitializeDatabase(AppTestServer testServer)
    {
        if (testServer.WebApp.Environment.IsDevelopment())
        {
            await using var scope = testServer.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                connection = new SqliteConnection(dbContext.Database.GetConnectionString());
                await connection.OpenAsync();
            if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
            {
                await dbContext.Database.MigrateAsync();
            }
            else if ((await dbContext.Database.GetAppliedMigrationsAsync()).Any() is false)
            {
                throw new InvalidOperationException("No migrations have been added. Please ensure that migrations are added before running tests.");
            }
        }
    }
}
